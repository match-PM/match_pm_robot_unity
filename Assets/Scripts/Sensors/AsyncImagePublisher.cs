using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace ROS2
{
    /// <summary>
    /// Captures a camera into a sensor_msgs/Image with as little main-thread work
    /// as possible, shared by CamPublisher and CamObserverPublisher.
    ///
    /// Only issuing the render has to happen on the main thread. The vertical
    /// flip is a GPU blit, the GPU-to-CPU copy is an AsyncGPUReadback (no
    /// pipeline stall like ReadPixels), and the ROS message is filled and
    /// published on a worker thread. One capture is in flight at a time: a
    /// capture requested while the previous one is still running is skipped,
    /// so slow consumers lower the frame rate of the topic, not of Unity.
    /// </summary>
    public class AsyncImagePublisher : IDisposable
    {
        // A readback that never completes (e.g. device reset) must not silence
        // the publisher for the rest of the session.
        private const float ReadbackTimeout = 2f;

        private readonly Camera camera;
        private readonly IPublisher<sensor_msgs.msg.Image> publisher;
        private readonly ROS2UnityComponent ros2Unity;
        private readonly bool flipVertically;
        private readonly bool useAsyncReadback;

        private readonly RenderTexture renderTexture;
        private readonly RenderTexture flippedTexture;
        private readonly Texture2D syncTexture;

        private readonly sensor_msgs.msg.Image msg = new sensor_msgs.msg.Image();
        private readonly byte[] buffer;

        private readonly Thread worker;
        private readonly AutoResetEvent frameReady = new AutoResetEvent(false);
        private volatile bool disposed;

        // Main thread only.
        private bool readbackPending;
        private float readbackStarted;
        // Set by the main thread when a frame is handed over, cleared by the worker.
        private volatile bool publishing;

        public AsyncImagePublisher(Camera camera, int width, int height, bool flipVertically,
                                   IPublisher<sensor_msgs.msg.Image> publisher, ROS2UnityComponent ros2Unity)
        {
            this.camera = camera;
            this.publisher = publisher;
            this.ros2Unity = ros2Unity;
            this.flipVertically = flipVertically;
            useAsyncReadback = SystemInfo.supportsAsyncGPUReadback;

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            if (flipVertically)
            {
                flippedTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                flippedTexture.Create();
            }
            if (!useAsyncReadback)
            {
                syncTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Debug.LogWarning($"AsyncImagePublisher: AsyncGPUReadback is not supported, " +
                                 $"'{camera.name}' falls back to a blocking ReadPixels.");
            }

            buffer = new byte[width * height * 4];
            msg.Width = (uint)width;
            msg.Height = (uint)height;
            msg.Step = 4 * (uint)width;
            msg.Encoding = "rgba8";
            msg.Data = buffer;

            worker = new Thread(PublishLoop) { IsBackground = true, Name = $"ImagePublisher {camera.name}" };
            worker.Start();
        }

        /// <summary>Starts a capture unless one is still in flight. Main thread only.</summary>
        public bool TryCapture()
        {
            if (disposed || publishing)
                return false;

            if (readbackPending)
            {
                if (Time.realtimeSinceStartup - readbackStarted < ReadbackTimeout)
                    return false;
                Debug.LogWarning($"AsyncImagePublisher: readback for '{camera.name}' timed out, retrying.");
            }

            Texture source = Render();

            if (!useAsyncReadback)
            {
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = (RenderTexture)source;
                syncTexture.ReadPixels(new Rect(0, 0, syncTexture.width, syncTexture.height), 0, 0);
                RenderTexture.active = previousActive;
                syncTexture.GetRawTextureData<byte>().CopyTo(buffer);
                HandOver();
                return true;
            }

            readbackPending = true;
            readbackStarted = Time.realtimeSinceStartup;
            AsyncGPUReadback.Request(source, 0, TextureFormat.RGBA32, OnReadback);
            return true;
        }

        private Texture Render()
        {
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
            }
            finally
            {
                camera.targetTexture = previousTarget;
            }

            if (!flipVertically)
                return renderTexture;

            Graphics.Blit(renderTexture, flippedTexture, new Vector2(1f, -1f), new Vector2(0f, 1f));
            return flippedTexture;
        }

        // Called on the main thread a few frames after the request.
        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            readbackPending = false;
            if (disposed || request.hasError || publishing)
                return;

            // The NativeArray is only valid during this callback; a memcpy.
            request.GetData<byte>().CopyTo(buffer);
            HandOver();
        }

        private void HandOver()
        {
            publishing = true;
            frameReady.Set();
        }

        private void PublishLoop()
        {
            while (true)
            {
                frameReady.WaitOne();
                if (disposed)
                    return;

                try
                {
                    // Skip once the ROS context is gone (e.g. while leaving Play mode),
                    // otherwise ros2cs logs "Cannot publish as the class is already disposed".
                    if (ros2Unity.Ok())
                        publisher.Publish(msg);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    publishing = false;
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            frameReady.Set();
            worker.Join(1000);

            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
            if (flippedTexture != null)
            {
                flippedTexture.Release();
                UnityEngine.Object.Destroy(flippedTexture);
            }
            if (syncTexture != null)
                UnityEngine.Object.Destroy(syncTexture);
        }
    }
}
