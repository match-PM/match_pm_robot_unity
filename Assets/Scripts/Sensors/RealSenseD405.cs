using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace ROS2
{
    /// <summary>
    /// Simulated Intel RealSense D405 publishing color, depth and a per-part segmentation.
    ///
    /// All images come from the same camera pose and intrinsics, so depth and
    /// segmentation are pixel-aligned with the color image (like the driver's
    /// aligned_depth_to_color output; on the real D405 color and depth come from the
    /// same imager anyway). Topics, relative to <see cref="topicNamespace"/>:
    ///   color/image_raw                    rgb8
    ///   color/camera_info
    ///   aligned_depth_to_color/image_raw   32FC1 [m] or 16UC1 [mm], 0 = no data
    ///   aligned_depth_to_color/camera_info
    ///   segmentation/image_raw             rgb8, one unique color per part, black = background
    ///   segmentation/ids                   mono16, segment id per pixel, 0 = background
    ///   segmentation/labels                std_msgs/String JSON (id, name, kind, path, color),
    ///                                      reliable + transient local, republished on change
    ///
    /// Depth and segmentation are drawn with a CommandBuffer (Hidden/PM/SegmentationDepth),
    /// independent of the scene's materials. The capture mirrors AsyncImagePublisher: the
    /// main thread only issues the renders and AsyncGPUReadbacks, conversion and
    /// publishing run on a worker thread, and one capture is in flight at a time.
    /// The Camera component stays disabled; it is only rendered explicitly.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RealSenseD405 : MonoBehaviour
    {
        public enum DepthEncoding { Float32Meters, UInt16Millimeters }

        private const float ReadbackTimeout = 2f;

        [Header("ROS")]
        public string nodeName = "unity_realsense_d405";
        [Tooltip("Topic namespace, e.g. 'd405' -> /d405/color/image_raw")]
        public string topicNamespace = "d405";
        public string frameId = "d405_color_optical_frame";

        [Header("Sensor (Intel RealSense D405 defaults)")]
        public int width = 848;
        public int height = 480;
        [Tooltip("Horizontal field of view [deg]. D405: 87 x 58 deg.")]
        public float horizontalFovDeg = 87f;
        [Tooltip("Frames per second published (the D405 runs up to 90 fps at 848x480).")]
        public float publishRate = 15f;
        [Tooltip("Closer than this, depth is reported as 0 (no data). D405 min-Z ~7 cm.")]
        public float minDepth = 0.07f;
        [Tooltip("Farther than this, depth is reported as 0 (no data). D405 ideal range up to 50 cm.")]
        public float maxDepth = 0.5f;
        public DepthEncoding depthEncoding = DepthEncoding.Float32Meters;

        [Header("Depth noise (stereo model: sigma = z^2 * subpixel / (fx * baseline))")]
        public bool simulateDepthNoise = false;
        [Tooltip("Stereo baseline [m]. D405: 18 mm.")]
        public float baseline = 0.018f;
        [Tooltip("Subpixel disparity RMS error [px].")]
        public float subpixel = 0.08f;

        [Header("Outputs")]
        public bool publishColor = true;
        public bool publishDepth = true;
        public bool publishSegmentation = true;
        [Tooltip("Seconds between scans for new/removed objects (spawned components).")]
        public float segmentationRescanInterval = 0.5f;

        [Header("Game view displays (1-8, 0 = off; Display 1, 4, 5 are used by other cameras)")]
        [Tooltip("Shows the published color image.")]
        [Range(0, 8)] public int colorDisplay = 6;
        [Tooltip("Shows the published segmentation color map.")]
        [Range(0, 8)] public int segmentationDisplay = 7;
        [Tooltip("Shows the depth (near = bright, far = dark, no data = black).")]
        [Range(0, 8)] public int depthDisplay = 8;

        // A viewer is a camera that renders nothing itself and only blits one of the
        // captured textures to its display, so the displays show exactly what is
        // published, at the publish rate, without rendering the scene again.
        private class Viewer
        {
            public Camera camera;
            public CommandBuffer blit;
        }
        private Viewer colorViewer, segViewer, depthViewer;
        private Material depthVizMaterial;

        private Camera cam;
        private ROS2UnityComponent ros2Unity;
        private ROS2Node node;
        private IPublisher<sensor_msgs.msg.Image> colorPub, depthPub, segPub, idPub;
        private IPublisher<sensor_msgs.msg.CameraInfo> colorInfoPub, depthInfoPub;
        private IPublisher<std_msgs.msg.String> labelsPub;
        private readonly ROS2TimeSource timeSource = new ROS2TimeSource();

        private RenderTexture colorRT, segRT, dataRT, depthBufferRT;
        private Material segMaterial;
        private CommandBuffer cmd;
        private RenderTargetIdentifier[] mrt;
        private Plane[] frustum = new Plane[6];
        private int capturedWidth, capturedHeight;
        private DepthEncoding capturedEncoding;
        private float capturedFx;

        // Readback staging (bottom-up, as read from the GPU).
        private byte[] colorRaw, segRaw;
        private float[] dataRaw;
        // Outgoing messages (top-down).
        private sensor_msgs.msg.Image colorMsg, depthMsg, segMsg, idMsg;
        private sensor_msgs.msg.CameraInfo infoMsg;
        private float[] depthMeters;
        private System.Random rng = new System.Random();

        // Capture state. Main thread: busy/pending/generation; worker clears busy.
        private volatile bool busy;
        private int pendingReadbacks;
        private bool readbackFailed;
        private int generation;
        private float captureStarted;
        private int stampSec;
        private uint stampNanosec;
        private float fx, fy, cx, cy;

        private Thread worker;
        private readonly AutoResetEvent frameReady = new AutoResetEvent(false);
        private volatile bool disposed;

        private float timeSinceCapture;
        private float timeSinceRescan = float.MaxValue;
        private int publishedLabelsVersion = -1;

        private static readonly int SegViewId = Shader.PropertyToID("_SegView");
        private static readonly int SegViewProjId = Shader.PropertyToID("_SegViewProj");
        private static readonly int SegColorId = Shader.PropertyToID("_SegColor");
        private static readonly int SegIdId = Shader.PropertyToID("_SegId");

        void Awake()
        {
            cam = GetComponent<Camera>();
            ApplyIntrinsics();
        }

        void Start()
        {
            // No '??': Unity's fake-null objects are not C# null.
            ros2Unity = GetComponent<ROS2UnityComponent>();
            if (ros2Unity == null)
                ros2Unity = FindObjectOfType<ROS2UnityComponent>();
            if (ros2Unity == null)
                ros2Unity = gameObject.AddComponent<ROS2UnityComponent>();

            var shader = Resources.Load<Shader>("Shader/SegmentationDepth");
            if (shader == null || !shader.isSupported)
                Debug.LogError("RealSenseD405: shader 'Resources/Shader/SegmentationDepth' missing or unsupported; " +
                               "depth and segmentation are disabled.");
            else
                segMaterial = new Material(shader);

            if (!SystemInfo.supportsAsyncGPUReadback)
                Debug.LogError("RealSenseD405: AsyncGPUReadback is not supported on this graphics API.");

            timeSinceCapture = UnityEngine.Random.Range(0f, 1f / Mathf.Max(publishRate, 0.1f));
            worker = new Thread(PublishLoop) { IsBackground = true, Name = $"RealSenseD405 {name}" };
            worker.Start();
        }

        void OnValidate()
        {
            width = Mathf.Max(16, width);
            height = Mathf.Max(16, height);
            horizontalFovDeg = Mathf.Clamp(horizontalFovDeg, 1f, 170f);
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null) ApplyIntrinsics();
        }

        /// <summary>Ideal pinhole intrinsics from the horizontal FOV, square pixels, centered.</summary>
        private void ApplyIntrinsics()
        {
            fx = (width * 0.5f) / Mathf.Tan(horizontalFovDeg * 0.5f * Mathf.Deg2Rad);
            fy = fx;
            cx = (width - 1) * 0.5f;
            cy = (height - 1) * 0.5f;

            cam.aspect = (float)width / height;
            cam.fieldOfView = 2f * Mathf.Atan((height * 0.5f) / fy) * Mathf.Rad2Deg; // vertical
            cam.nearClipPlane = Mathf.Min(0.01f, minDepth * 0.5f);
            cam.farClipPlane = Mathf.Max(1f, maxDepth * 2f);
            cam.enabled = false; // rendered explicitly; the displays use viewer cameras
        }

        void Update()
        {
            if (node == null && ros2Unity != null && ros2Unity.Ok())
            {
                node = ros2Unity.CreateNode(nodeName);
                string ns = string.IsNullOrEmpty(topicNamespace) ? "" : topicNamespace.TrimEnd('/') + "/";
                colorPub = node.CreateSensorPublisher<sensor_msgs.msg.Image>(ns + "color/image_raw");
                colorInfoPub = node.CreateSensorPublisher<sensor_msgs.msg.CameraInfo>(ns + "color/camera_info");
                depthPub = node.CreateSensorPublisher<sensor_msgs.msg.Image>(ns + "aligned_depth_to_color/image_raw");
                depthInfoPub = node.CreateSensorPublisher<sensor_msgs.msg.CameraInfo>(ns + "aligned_depth_to_color/camera_info");
                segPub = node.CreateSensorPublisher<sensor_msgs.msg.Image>(ns + "segmentation/image_raw");
                idPub = node.CreateSensorPublisher<sensor_msgs.msg.Image>(ns + "segmentation/ids");

                var latched = new QualityOfServiceProfile(QosPresetProfile.DEFAULT);
                latched.SetReliability(ReliabilityPolicy.QOS_POLICY_RELIABILITY_RELIABLE);
                latched.SetDurability(DurabilityPolicy.QOS_POLICY_DURABILITY_TRANSIENT_LOCAL);
                latched.SetHistory(HistoryPolicy.QOS_POLICY_HISTORY_KEEP_LAST, 1);
                labelsPub = node.CreatePublisher<std_msgs.msg.String>(ns + "segmentation/labels", latched);

                Debug.Log($"RealSenseD405: publishing on '/{ns}...' ({width}x{height}, " +
                          $"fx={fx:F1}, {publishRate} Hz).");
            }
        }

        void LateUpdate()
        {
            UpdateViewers();

            if (node == null || !ros2Unity.Ok())
                return;

            if (publishSegmentation || publishDepth)
            {
                timeSinceRescan += Time.unscaledDeltaTime;
                if (timeSinceRescan >= segmentationRescanInterval)
                {
                    timeSinceRescan = 0f;
                    SegmentationRegistry.Rescan();
                }
                PublishLabelsIfChanged();
            }

            timeSinceCapture += Time.deltaTime;
            if (timeSinceCapture < 1f / Mathf.Max(publishRate, 0.1f))
                return;

            if (busy)
            {
                if (pendingReadbacks == 0 || Time.realtimeSinceStartup - captureStarted < ReadbackTimeout)
                    return;
                Debug.LogWarning("RealSenseD405: readback timed out, retrying.");
                generation++; // late callbacks of the stuck capture are ignored
                pendingReadbacks = 0;
                busy = false;
            }

            if (Capture())
                timeSinceCapture = 0f;
        }

        private void PublishLabelsIfChanged()
        {
            if (!publishSegmentation || labelsPub == null)
                return;
            int version = SegmentationRegistry.Version;
            if (version == publishedLabelsVersion)
                return;
            publishedLabelsVersion = version;
            labelsPub.Publish(new std_msgs.msg.String { Data = SegmentationRegistry.LabelsToJson() });
        }

        // =====================================================================
        // Main thread: render + readback requests
        // =====================================================================

        private bool Capture()
        {
            if (disposed || !SystemInfo.supportsAsyncGPUReadback)
                return false;
            if (!publishColor && !publishDepth && !publishSegmentation)
                return false;

            EnsureResources();

            busy = true;
            readbackFailed = false;
            captureStarted = Time.realtimeSinceStartup;
            timeSource.GetTime(out stampSec, out stampNanosec);
            int gen = ++generation;
            pendingReadbacks = 0;

            bool wantData = (publishDepth || publishSegmentation) && segMaterial != null;

            if (publishColor)
            {
                RenderTexture previous = cam.targetTexture;
                try
                {
                    cam.targetTexture = colorRT;
                    cam.Render();
                }
                finally
                {
                    cam.targetTexture = previous;
                }
            }
            if (wantData)
                RenderSegmentationAndDepth();

            // Count first: a readback may complete synchronously.
            pendingReadbacks = (publishColor ? 1 : 0) + (wantData && publishSegmentation ? 1 : 0) + (wantData ? 1 : 0);
            if (pendingReadbacks == 0)
            {
                busy = false;
                return false;
            }

            if (publishColor)
                AsyncGPUReadback.Request(colorRT, 0, TextureFormat.RGBA32,
                    r => OnReadback(r, gen, req => req.GetData<byte>().CopyTo(colorRaw)));
            if (wantData && publishSegmentation)
                AsyncGPUReadback.Request(segRT, 0, TextureFormat.RGBA32,
                    r => OnReadback(r, gen, req => req.GetData<byte>().CopyTo(segRaw)));
            if (wantData)
                AsyncGPUReadback.Request(dataRT, 0, TextureFormat.RGFloat,
                    r => OnReadback(r, gen, req => req.GetData<float>().CopyTo(dataRaw)));
            return true;
        }

        private void RenderSegmentationAndDepth()
        {
            Matrix4x4 view = cam.worldToCameraMatrix;
            Matrix4x4 proj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            GeometryUtility.CalculateFrustumPlanes(cam.projectionMatrix * view, frustum);
            int cullingMask = cam.cullingMask;

            cmd.Clear();
            cmd.SetRenderTarget(mrt, depthBufferRT.depthBuffer);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.SetGlobalMatrix(SegViewId, view);
            cmd.SetGlobalMatrix(SegViewProjId, proj * view);

            foreach (var entry in SegmentationRegistry.Entries)
            {
                Renderer r = entry.renderer;
                if (entry.label == null || r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                if ((cullingMask & (1 << r.gameObject.layer)) == 0)
                    continue;
                if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                    continue;
                Mesh mesh = entry.Mesh;
                if (mesh == null || !GeometryUtility.TestPlanesAABB(frustum, r.bounds))
                    continue;

                cmd.SetGlobalVector(SegColorId, entry.label.shaderColor); // vector: no sRGB conversion
                cmd.SetGlobalFloat(SegIdId, entry.label.id);
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    cmd.DrawRenderer(r, segMaterial, sub, 0);
            }

            Graphics.ExecuteCommandBuffer(cmd);
        }

        // Called on the main thread a few frames after the request.
        private void OnReadback(AsyncGPUReadbackRequest request, int gen, Action<AsyncGPUReadbackRequest> copy)
        {
            if (gen != generation || disposed)
                return;
            if (request.hasError)
                readbackFailed = true;
            else
                copy(request);

            if (--pendingReadbacks > 0)
                return;
            if (readbackFailed)
            {
                busy = false;
                return;
            }
            frameReady.Set(); // worker clears busy when done
        }

        private void EnsureResources()
        {
            if (colorRT != null && capturedWidth == width && capturedHeight == height &&
                capturedEncoding == depthEncoding && capturedFx == fx)
                return;
            ReleaseResources();
            capturedWidth = width;
            capturedHeight = height;
            capturedEncoding = depthEncoding;
            capturedFx = fx;
            int w = width, h = height, n = w * h;

            colorRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "D405 color" };
            segRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                { name = "D405 segmentation", filterMode = FilterMode.Point };
            dataRT = new RenderTexture(w, h, 0, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear)
                { name = "D405 depth+id", filterMode = FilterMode.Point };
            depthBufferRT = new RenderTexture(w, h, 24, RenderTextureFormat.Depth) { name = "D405 zbuffer" };
            colorRT.Create(); segRT.Create(); dataRT.Create(); depthBufferRT.Create();
            mrt = new RenderTargetIdentifier[] { segRT, dataRT };
            cmd ??= new CommandBuffer { name = "RealSenseD405 segmentation+depth" };

            colorRaw = new byte[n * 4];
            segRaw = new byte[n * 4];
            dataRaw = new float[n * 2];
            depthMeters = new float[n];

            colorMsg = NewImage(w, h, "rgb8", 3);
            segMsg = NewImage(w, h, "rgb8", 3);
            idMsg = NewImage(w, h, "mono16", 2);
            depthMsg = depthEncoding == DepthEncoding.Float32Meters
                ? NewImage(w, h, "32FC1", 4)
                : NewImage(w, h, "16UC1", 2);
            infoMsg = NewCameraInfo(w, h);

            // The textures were recreated; point the viewers at the new ones.
            DestroyViewers();
        }

        // =====================================================================
        // Game view displays
        // =====================================================================

        private void UpdateViewers()
        {
            if (colorRT == null)
                return; // nothing captured yet

            colorViewer = SyncViewer(colorViewer, colorDisplay, "color", colorRT, null);
            segViewer = SyncViewer(segViewer, publishSegmentation ? segmentationDisplay : 0, "segmentation", segRT, null);

            if (depthVizMaterial == null)
            {
                var shader = Resources.Load<Shader>("Shader/DepthVisualize");
                if (shader != null && shader.isSupported)
                    depthVizMaterial = new Material(shader);
            }
            if (depthVizMaterial != null)
            {
                depthVizMaterial.SetFloat("_DepthMin", minDepth);
                depthVizMaterial.SetFloat("_DepthMax", maxDepth);
            }
            depthViewer = SyncViewer(depthViewer, publishDepth && depthVizMaterial != null ? depthDisplay : 0,
                                     "depth", dataRT, depthVizMaterial);
        }

        private Viewer SyncViewer(Viewer viewer, int display, string label, RenderTexture source, Material material)
        {
            if (display <= 0 || source == null)
            {
                DestroyViewer(viewer);
                return null;
            }
            if (viewer == null)
            {
                var go = new GameObject($"{name} {label} display") { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(transform, false);
                var viewerCam = go.AddComponent<Camera>();
                viewerCam.cullingMask = 0; // render nothing, only the blit below
                viewerCam.clearFlags = CameraClearFlags.SolidColor;
                viewerCam.backgroundColor = Color.black;
                viewerCam.depth = 10;
                viewerCam.useOcclusionCulling = false;

                var blit = new CommandBuffer { name = $"D405 {label} display" };
                if (material != null)
                    blit.Blit(source, BuiltinRenderTextureType.CameraTarget, material);
                else
                    blit.Blit(source, BuiltinRenderTextureType.CameraTarget);
                viewerCam.AddCommandBuffer(CameraEvent.AfterEverything, blit);
                viewer = new Viewer { camera = viewerCam, blit = blit };
            }
            viewer.camera.targetDisplay = display - 1;
            return viewer;
        }

        private static void DestroyViewer(Viewer viewer)
        {
            if (viewer == null)
                return;
            viewer.blit?.Release();
            if (viewer.camera != null)
                Destroy(viewer.camera.gameObject);
        }

        private void DestroyViewers()
        {
            DestroyViewer(colorViewer);
            DestroyViewer(segViewer);
            DestroyViewer(depthViewer);
            colorViewer = segViewer = depthViewer = null;
        }

        private sensor_msgs.msg.Image NewImage(int w, int h, string encoding, int bytesPerPixel)
        {
            var msg = new sensor_msgs.msg.Image
            {
                Width = (uint)w,
                Height = (uint)h,
                Encoding = encoding,
                Is_bigendian = 0, // little endian (x86)
                Step = (uint)(w * bytesPerPixel),
                Data = new byte[w * h * bytesPerPixel],
            };
            msg.Header.Frame_id = frameId;
            return msg;
        }

        private sensor_msgs.msg.CameraInfo NewCameraInfo(int w, int h)
        {
            var info = new sensor_msgs.msg.CameraInfo
            {
                Width = (uint)w,
                Height = (uint)h,
                Distortion_model = "plumb_bob",
                D = new double[5], // ideal pinhole, no distortion
            };
            info.Header.Frame_id = frameId;
            // Fixed-size message arrays are pre-allocated and read-only in ros2cs.
            Array.Copy(new double[] { fx, 0, cx, 0, fy, cy, 0, 0, 1 }, info.K, 9);
            Array.Copy(new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }, info.R, 9);
            Array.Copy(new double[] { fx, 0, cx, 0, 0, fy, cy, 0, 0, 0, 1, 0 }, info.P, 12);
            return info;
        }

        // =====================================================================
        // Worker thread: convert + publish
        // =====================================================================

        private void PublishLoop()
        {
            while (true)
            {
                frameReady.WaitOne();
                if (disposed)
                    return;
                try
                {
                    if (ros2Unity != null && ros2Unity.Ok())
                        ConvertAndPublish();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    busy = false;
                }
            }
        }

        private void ConvertAndPublish()
        {
            int w = capturedWidth, h = capturedHeight;
            bool haveData = segMaterial != null && (publishDepth || publishSegmentation);

            SetStamp(infoMsg.Header);

            if (publishColor)
            {
                FlipRgbaToRgb(colorRaw, colorMsg.Data, w, h);
                SetStamp(colorMsg.Header);
                colorPub.Publish(colorMsg);
                colorInfoPub.Publish(infoMsg);
            }

            if (haveData && publishSegmentation)
            {
                FlipRgbaToRgb(segRaw, segMsg.Data, w, h);
                byte[] ids = idMsg.Data;
                for (int y = 0; y < h; y++)
                {
                    int src = (h - 1 - y) * w, dst = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int id = (int)(dataRaw[(src + x) * 2 + 1] + 0.5f);
                        ids[(dst + x) * 2] = (byte)id;
                        ids[(dst + x) * 2 + 1] = (byte)(id >> 8);
                    }
                }
                SetStamp(segMsg.Header);
                SetStamp(idMsg.Header);
                segPub.Publish(segMsg);
                idPub.Publish(idMsg);
            }

            if (haveData && publishDepth)
            {
                float sigmaScale = simulateDepthNoise ? subpixel / (fx * Mathf.Max(baseline, 1e-4f)) : 0f;
                for (int y = 0; y < h; y++)
                {
                    int src = (h - 1 - y) * w, dst = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        float d = dataRaw[(src + x) * 2];
                        if (d > 0f && sigmaScale > 0f)
                            d += (float)(Gaussian() * sigmaScale * d * d);
                        depthMeters[dst + x] = d >= minDepth && d <= maxDepth ? d : 0f;
                    }
                }

                byte[] outData = depthMsg.Data;
                if (depthMsg.Encoding == "32FC1")
                {
                    Buffer.BlockCopy(depthMeters, 0, outData, 0, outData.Length);
                }
                else
                {
                    for (int i = 0; i < depthMeters.Length; i++)
                    {
                        int mm = Math.Min(65535, (int)(depthMeters[i] * 1000f + 0.5f));
                        outData[i * 2] = (byte)mm;
                        outData[i * 2 + 1] = (byte)(mm >> 8);
                    }
                }
                SetStamp(depthMsg.Header);
                depthPub.Publish(depthMsg);
                depthInfoPub.Publish(infoMsg);
            }
        }

        private void SetStamp(std_msgs.msg.Header header)
        {
            header.Stamp.Sec = stampSec;
            header.Stamp.Nanosec = stampNanosec;
            header.Frame_id = frameId;
        }

        // GPU rows are bottom-up; ROS images are top-down.
        private static void FlipRgbaToRgb(byte[] src, byte[] dst, int w, int h)
        {
            for (int y = 0; y < h; y++)
            {
                int s = (h - 1 - y) * w * 4, d = y * w * 3;
                for (int x = 0; x < w; x++, s += 4, d += 3)
                {
                    dst[d] = src[s];
                    dst[d + 1] = src[s + 1];
                    dst[d + 2] = src[s + 2];
                }
            }
        }

        private double Gaussian()
        {
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        // =====================================================================
        // Cleanup
        // =====================================================================

        private void ReleaseResources()
        {
            foreach (var rt in new[] { colorRT, segRT, dataRT, depthBufferRT })
            {
                if (rt == null) continue;
                rt.Release();
                Destroy(rt);
            }
            colorRT = segRT = dataRT = depthBufferRT = null;
        }

        void OnDestroy()
        {
            disposed = true;
            frameReady.Set();
            worker?.Join(1000);
            DestroyViewers();
            ReleaseResources();
            cmd?.Release();
            if (segMaterial != null)
                Destroy(segMaterial);
            if (depthVizMaterial != null)
                Destroy(depthVizMaterial);
        }
    }
}
