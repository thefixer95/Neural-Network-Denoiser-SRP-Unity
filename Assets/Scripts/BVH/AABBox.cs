using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class AABBox : MonoBehaviour
{

    public Color materialColor = new Color(0, 0, 0);
    [System.NonSerialized]
    public Vector3 specular;
    [System.NonSerialized]
    public Vector3 albedo;
    public float smoothness = 0;
    public Color emission = new Color(0, 0, 0);
    [System.NonSerialized]
    public Vector3 emissionOut;
    [Range(0, 100)]
    public float emissionForce;
    public float materialType = 0;

    public Texture2D textureImage;


    public List<AxisAlignBB> aabbS = new List<AxisAlignBB>();      //list of leafs in this mesh

    Bounds BBofOBJ;

    public struct AxisAlignBB
    {
        public TriangleInMesh triangle;
        public Bounds BB;
    }

    public struct TriangleInMesh
    {
        public Matrix4x4 localToWMat;
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;
        public int idx1;
        public int idx2;
        public int idx3;
        public Vector3 specular;
        public Vector3 albedo;
        public Vector3 emission;
        public float smoothness;
        public float materialType;
    }


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(transform.localToWorldMatrix);
        emissionOut = new Vector3(emission.r, emission.g, emission.b);
        emissionOut *= emissionForce;

        bool metal = materialType > 0.5f;
        albedo = metal ? Vector3.zero : new Vector3(materialColor.r, materialColor.g, materialColor.b);
        specular = metal ? new Vector3(materialColor.r, materialColor.g, materialColor.b) : Vector3.one * 0.04f;


        BBofOBJ = GetComponent<Renderer>().bounds;

        List<TriangleInMesh> Triangles = new List<TriangleInMesh>();


        Triangles = extractTriangles(Triangles);

        //Debug.Log(Triangles[0].p1 + " , "+ Triangles[0].p2 + " , "+ Triangles[0].p3);
        //Debug.Log(Triangles[1].p1 + " , "+ Triangles[1].p2 + " , "+ Triangles[1].p3);

        aabbS = CreateBVHofMesh(aabbS, Triangles);

        //Debug.Log(aabbS[1].BB.center);
        //Debug.Log(aabbS[1].BB.min);
        //Debug.Log(aabbS[1].BB.max);

    }

    void OnDrawGizmosSelected()
    {

        foreach (AxisAlignBB a in aabbS)
        {
            Vector3 c = a.BB.center;
            Vector3 s = a.BB.size;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(c, s);
        }

        Vector3 center = BBofOBJ.center;
        Vector3 size = BBofOBJ.size;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }

    private List<AxisAlignBB> CreateBVHofMesh(List<AxisAlignBB> aabbS, List<TriangleInMesh> triangles)
    {
        Debug.Log(gameObject.transform.localScale);

        foreach (TriangleInMesh t in triangles)
        {
            //Vector3 mn = checkForMinOfBB(t);
            //Vector3 mx = checkForMaxOfBB(t);

            Bounds BBtemp = new Bounds();
            BBtemp.SetMinMax(Vector3.Scale(checkForMinOfBB(t), gameObject.transform.localScale), Vector3.Scale(checkForMaxOfBB(t), gameObject.transform.localScale));
            //BBtemp.SetMinMax(checkForMinOfBB(t), checkForMaxOfBB(t));
            BBtemp.center += gameObject.transform.position;

            aabbS.Add(new AxisAlignBB()
            {
                triangle = t,
                BB = BBtemp
            });
        }

        return aabbS;
    }

    private Vector3 checkForMaxOfBB(TriangleInMesh t)
    {
        Vector3 maxVrt = new Vector3(Mathf.Max(t.p1.x, t.p2.x, t.p3.x), Mathf.Max(t.p1.y, t.p2.y, t.p3.y), Mathf.Max(t.p1.z, t.p2.z, t.p3.z));
        return maxVrt;
    }

    private Vector3 checkForMinOfBB(TriangleInMesh t)
    {
        Vector3 minVrt = new Vector3(Mathf.Min(t.p1.x, t.p2.x, t.p3.x), Mathf.Min(t.p1.y, t.p2.y, t.p3.y), Mathf.Min(t.p1.z, t.p2.z, t.p3.z));
        return minVrt;
    }

    private List<TriangleInMesh> extractTriangles(List<TriangleInMesh> triangles)
    {
        Mesh m = gameObject.GetComponent<MeshFilter>().sharedMesh;
        var indices = m.GetIndices(0);

        for (int i = 0; i < m.triangles.Length; i += 3)
        {
            triangles.Add(new TriangleInMesh()
            {
                localToWMat = transform.localToWorldMatrix,
                p1 = m.vertices[m.triangles[i + 0]],
                p2 = m.vertices[m.triangles[i + 1]],
                p3 = m.vertices[m.triangles[i + 2]],
                idx1 = indices[i],
                idx2 = indices[i + 1],
                idx3 = indices[i + 2],
                specular = gameObject.GetComponent<RTObjects>().specular,
                albedo = gameObject.GetComponent<RTObjects>().albedo,
                emission = gameObject.GetComponent<RTObjects>().emissionOut,
                smoothness = gameObject.GetComponent<RTObjects>().smoothness,
                //materialType = gameObject.GetComponent<RTObjects>().materialType,
            });

        }
        return triangles;
    }


    private List<AxisAlignBB> TransformMeshToAABB()
    {
        List<AxisAlignBB> aABBleafs = new List<AxisAlignBB>();



        return aABBleafs;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
