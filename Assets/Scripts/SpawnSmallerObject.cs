using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSmallerObject : MonoBehaviour
{
    public string objectName;
    
    public Mesh meshPart;
  
    public MeshFilter mf;
    public MeshRenderer mr;

    // Start is called before the first frame update
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        var newMat = Resources.Load<Material>("Materials/green");
        mr.material = newMat;
        
        mf = GetComponent<MeshFilter>();
        name = objectName;

        Vector3[] v = meshPart.vertices;
        Vector3[] n = meshPart.normals;
        Vector3[] r = Left2Right(v);
        Vector3[] s = Left2Right(v);
        meshPart.vertices = r;
        meshPart.normals = s;
        mf.mesh = meshPart;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private static Vector3[] Left2Right(Vector3[] v)
    {
        Vector3[] r = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            r[i] = Ros2Unity(v[i]);
        }
        return r;
    }

    public static Vector3 Ros2Unity(Vector3 vector3)
    {
        return new Vector3(-vector3.y, vector3.z, vector3.x);
    }

    public static Vector3 Unity2Ros(Vector3 vector3)
    {
        return new Vector3(vector3.z, -vector3.x, vector3.y);
    }

    public static Vector3 Ros2UnityScale(Vector3 vector3)
    {
        return new Vector3(vector3.y, vector3.z, vector3.x);
    }

    public static Vector3 Unity2RosScale(Vector3 vector3)
    {
        return new Vector3(vector3.z, vector3.x, vector3.y);
    }

    public static Quaternion Ros2Unity(Quaternion quaternion)
    {
        return new Quaternion(quaternion.y, -quaternion.z, -quaternion.x, quaternion.w);
    }

    public static Quaternion Unity2Ros(Quaternion quaternion)
    {
        return new Quaternion(-quaternion.z, quaternion.x, -quaternion.y, quaternion.w);
    }

    public static void Unity2Ros(ref Quaternion quaternion)
    {
        var z = quaternion.z;
        var x = quaternion.x;
        var y = quaternion.y;
        quaternion.x = -z;
        quaternion.y = x;
        quaternion.z = -y;
    }

    public static void Unity2Ros(ref Vector3 vector)
    {
        var z = vector.z;
        var x = vector.x;
        var y = vector.y;
        vector.x = z;
        vector.y = -x;
        vector.z = y;
    }

    public static Matrix4x4 Unity2RosMatrix4x4()
    {
        // Note: The matrix here is written as-if on paper,
        // but Unity's Matrix4x4 is constructed from column-vectors, hence the transpose.
        return new Matrix4x4(
            new Vector4( 0.0f, 0.0f, 1.0f, 0.0f),
            new Vector4(-1.0f, 0.0f, 0.0f, 0.0f),
            new Vector4( 0.0f, 1.0f, 0.0f, 0.0f),
            new Vector4( 0.0f, 0.0f, 0.0f, 1.0f)
        ).transpose;
    }

}
