using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
//using UnityEditor.VersionControl;
//using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using static AABBox;
using static BVH_Creation;
using static BVHconstruction;

partial class CameraRendererCS
{
    //parameters for path tracing

    bool isMultipleAA;
    int numberRays;

    private struct BVHnodes4CS
    {
        public int leaf;
        public int root;
        public int empty;

        public Vector3 _mibBB;
        public Vector3 _maxBB;
        public Vector3 _centerBB;
        public Vector3 _sizeBB;

        //public int[] childrenIndex;
        public int childrenIndexOffset;
        public int childrenIndexFirstID;

        public int fatherIndex;

        public int meshIndex;

        public int visited;
    }

    List<BVHnodes4CS> BVHcs;
    List<int> childrenList;
    ComputeBuffer BVHBuffer;
    ComputeBuffer childrenListBuffer;
    List<BoundingVolumeHierarchy> BVHcreatedInScene;

    private void GetBVHinScene()
    {
        BVHcs = new List<BVHnodes4CS>();
        childrenList = new List<int>();
        BVHcreatedInScene = GameObject.FindObjectOfType<BVH_Creation>().BVHInScene;

        childrenList.Clear();

        foreach (BoundingVolumeHierarchy b in BVHcreatedInScene)
        {
            int firstIndex = -1;    //if <0 non ha figli
            if (b.childrenIndex != null)
            {
                firstIndex = childrenList.Count;
                childrenList.AddRange(b.childrenIndex);
            }

            BVHnodes4CS el = new BVHnodes4CS()
            {
                leaf = b.leaf ? 1 : 0,  //true = 1, false = 0
                root = b.root ? 1 : 0,  //true = 1, false = 0
                empty = b.empty ? 1 : 0,    //true = 1, false = 0
                fatherIndex = b.fatherIndex,
                meshIndex = b.meshIndex,

                childrenIndexFirstID = firstIndex,
                childrenIndexOffset = -1,

                _maxBB = b.boundingBox.max,
                _mibBB = b.boundingBox.min,
                _centerBB = b.boundingBox.center,
                _sizeBB = b.boundingBox.size,
                visited = 0,            //true = 1, false = 0
            };

            if (b.childrenIndex != null)
                el.childrenIndexOffset = b.childrenIndex.Count;
            BVHcs.Add(el);
        }


        CreateComputeBuffer(ref BVHBuffer, BVHcs, System.Runtime.InteropServices.Marshal.SizeOf(typeof(BVHnodes4CS)));
        CreateComputeBuffer(ref childrenListBuffer, childrenList, System.Runtime.InteropServices.Marshal.SizeOf(typeof(int)));

    }


    //texture render target
    private RenderTexture _target;
    private RenderTexture _uvTarget;
    private RenderTexture _shadowTarget;
    private RenderTexture _normalTarget;



    //texture where to store the parameters of the rays     (!!! BETA !!!)
    private ComputeBuffer _rayOr;
    List<Vector4> _rayOriginsVect = new List<Vector4>();
    private ComputeBuffer _rayDir;
    List<Vector4> _rayDirVect = new List<Vector4>();
    private RenderTexture _rayEnergys;



    //// skybox texture
    //public Texture SkyboxTxt;
    //public float illuminationSkyboxRatio;

    //light parameters
    public Vector3 l;   //light position
    public Light directionalLight;  //light

    //---SPHERES---
    //spheres Parameters
    private ComputeBuffer sphereBuffer;
    //sphere structure
    private struct Sphere
    {
        public Vector3 center;
        public float radius;
        public Vector3 specular;
        public Vector3 albedo;
        public float smoothness;
        public Vector3 emission;
        public float materialType;
    }
    //sphere setup
    public static List<SphereParameters> SpheresInScene = new List<SphereParameters>();     //list of spheres
    public static bool spheresNeedRebuild = false;  //if the speres are setted dont do again

    public static void RegisterSphereForRT(SphereParameters obj)    //take spheres from The CS of sphere Object
    {
        SpheresInScene.Add(obj);
        spheresNeedRebuild = true;
    }

    public static void UnregisterSphereForRT(SphereParameters obj)      //delate spheres from the array of spheres in the scene
    {
        SpheresInScene.Remove(obj);
        spheresNeedRebuild = true;
    }

    private List<Sphere> Spheres = new List<Sphere>();      //list of spheres for the Compute Shader

    private void CreateSphereBuffer()       //create the sphere buffer
    {

        if (!spheresNeedRebuild)    //if already done dont do again
        {
            return;
        }

        spheresNeedRebuild = false; // i'm rebuilding so no need to do it twice or more
        Spheres.Clear();        //clear the List for compute shaders

        //loop all objects in game to take data
        foreach (SphereParameters go in SpheresInScene)
        {
            Spheres.Add(new Sphere()
            {
                center = go.transform.position,
                radius = go.transform.localScale.x / 2,
                albedo = go.albedo,
                specular = go.specular,
                smoothness = go.smoothness,
                materialType = go.matType,
                emission = go.emissionOut,
            });
        }
        CreateComputeBuffer(ref sphereBuffer, Spheres, 60);
    }

    //---MESHES---
    //Meshes Parameters
    //statics for mesh renders
    public static List<RTObjects> rTObjects = new List<RTObjects>();   //list of meshes in scene
    public static bool meshNeedRebuild = false;    //if the Compute shader list needs to be rebuit

    //function for taking meshes in scene
    public static void RegisterObjectForRT(RTObjects obj)       //take meshes from The CS of mesh Object
    {
        rTObjects.Add(obj);
        meshNeedRebuild = true;
    }
    public static void UnregisterObjectForRT(RTObjects obj)      //delate spheres from the array of spheres in the scene 
    {
        rTObjects.Remove(obj);
        meshNeedRebuild = true;
    }
    public static void UpdateOBJs()      //delate spheres from the array of spheres in the scene 
    {
        meshNeedRebuild = true;
    }

    //mesh struct              
    private struct MeshObj
    {
        public Matrix4x4 localToWorldMat;
        public int indices_offset;
        public int indices_count;
        public Vector3 specular;
        public Vector3 albedo;
        public Vector3 emission;
        public float smoothness;
        public float materialType;
        //public Texture2D texture;

        //BB
        public Vector3 centerbb;
        public Vector3 _minbb;
        public Vector3 _maxbb;
    }

    //mesh buffers
    private static List<MeshObj> meshes = new List<MeshObj>();
    private static List<Vector3> vertexs = new List<Vector3>();
    private static List<int> indexs = new List<int>();
    private static List<Vector3> vertexNormals = new List<Vector3>();
    private static List<Vector2> vertexUVs = new List<Vector2>();

    private ComputeBuffer meshObjBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;
    private ComputeBuffer VetexNormalsBuffer;

    private ComputeBuffer VertexUVBuffer;

    public static int SortByName(RTObjects o1, RTObjects o2)
    {
        return o1.gameObject.name.CompareTo(o2.gameObject.name);
    }

    //function to create buffers of mesh
    private void RebuildMeshObjBuffers()
    {
        //Debug.Log(rTObjects.Count);
        if (!meshNeedRebuild)
        {
            return;
        }
        rTObjects.Sort(SortByName);

        meshNeedRebuild = false;
        meshes.Clear();
        vertexs.Clear();
        indexs.Clear();
        vertexNormals.Clear();
        vertexUVs.Clear();

        //loop all objects in game to take data
        foreach (RTObjects obj in rTObjects)
        {
            //OGGETTI ORDINATI PER NOME ALLO STESSO MODO CHE DALL'ALTRO LATO, quindi l'iesimo numero è l'i dell'oggetto (BVH possibile da fare)
            //Debug.Log(obj.gameObject.name);

            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            //vertex data
            int firstVertex = vertexs.Count;
            vertexs.AddRange(mesh.vertices);

            int firstIndex = indexs.Count;
            var inds = mesh.GetIndices(0);

            //int firstVertexNorm = verticesNorm.Count;
            vertexNormals.AddRange(mesh.normals);
            vertexUVs.AddRange(mesh.uv);

            //foreach(Vector3 n in mesh.normals)
            //{
            //    Debug.Log(n);
            //}

            //print(mesh.normals.Length);
            //print(mesh.vertices.Length);
            //Debug.Log(obj.name + "  :  " + obj.transform.localToWorldMatrix);

            //mesh.normals

            indexs.AddRange(inds.Select(index => index + firstVertex));

            meshes.Add(new MeshObj()
            {
                localToWorldMat = obj.transform.localToWorldMatrix,
                indices_offset = firstIndex,
                indices_count = inds.Length,
                specular = obj.specular,
                albedo = obj.albedo,
                emission = obj.emissionOut,
                smoothness = obj.smoothness,
                materialType = obj.matType,
                centerbb = obj.gameObject.GetComponent<MeshRenderer>().bounds.center,
                _minbb = obj.gameObject.GetComponent<MeshRenderer>().bounds.min,
                _maxbb = obj.gameObject.GetComponent<MeshRenderer>().bounds.max,
                //centerbb = obj.transform.TransformPoint(obj.gameObject.GetComponent<MeshRenderer>().bounds.center),
                //_minbb = obj.transform.TransformPoint(obj.gameObject.GetComponent<MeshRenderer>().bounds.min),
                //_maxbb = obj.transform.TransformPoint(obj.gameObject.GetComponent<MeshRenderer>().bounds.max),
            });
        }
        //works
        //Debug.Log("bit root MeshOBJ (ref 116): " + System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshObj)));

        //CreateComputeBuffer(ref meshObjBuffer, meshes, 116);
        CreateComputeBuffer(ref meshObjBuffer, meshes, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshObj)));
        CreateComputeBuffer(ref VetexNormalsBuffer, vertexNormals, 12);
        CreateComputeBuffer(ref vertexBuffer, vertexs, 12);
        CreateComputeBuffer(ref indexBuffer, indexs, 4);

        CreateComputeBuffer(ref VertexUVBuffer, vertexUVs, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector2)));

    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
    where T : struct
    {

        //check if compute buffer exist
        if (buffer != null)
        {
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }
        //Debug.Log(data.Count + "conteggio");
        if (data.Count != 0)
        {
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            buffer.SetData(data);
        }
    }
    partial void ReleaseBuffers();

    partial void ReleaseBuffers()
    {
        if (sphereBuffer != null)
        {
            sphereBuffer.Dispose();
            sphereBuffer = null;
        }
        if (indexBuffer != null)
        {
            indexBuffer.Dispose();
            indexBuffer = null;
        }
        if (meshObjBuffer != null)
        {
            meshObjBuffer.Dispose();
            meshObjBuffer = null;
        }
        if (vertexBuffer != null)
        {
            vertexBuffer.Dispose();
            vertexBuffer = null;
        }
        if (VetexNormalsBuffer != null)
        {
            VetexNormalsBuffer.Dispose();
            VetexNormalsBuffer = null;
        }
        if (VertexUVBuffer != null)
        {
            VertexUVBuffer.Dispose();
            VertexUVBuffer = null;
        }
        //bvhbuffer dispose
        if (BVHBuffer != null)
        {
            BVHBuffer.Dispose();
            BVHBuffer = null;
        }
        if (childrenListBuffer != null)
        {
            childrenListBuffer.Dispose();
            childrenListBuffer = null;
        }
    }

    partial void SetAllComputeShader();
    partial void SetAllComputeShader()
    {
        if (!setBuffers)
        {
            setBuffers = true;
            //ReleaseBuffers();

            // if già cericate le variabili non serve farlo tutti i frame
            InitializeValues();

            //Debug.Log("sphereBuffer count: "+sphereBuffer.count);
        }
        else
        {
            l = directionalLight.transform.forward;
            RebuildMeshObjBuffers();
        }
        SetShaderParameters();
    }

    private void InitializeValues()
    {
        //initialize light
        directionalLight = Light.FindObjectOfType<Light>();
        l = directionalLight.transform.forward;
        //initialize sphere buffer
        CreateSphereBuffer();
        //initialize mesh triangle
        RebuildMeshObjBuffers();

        //initialize BVH
        GetBVHinScene();
    }

    private void SetComputeBuffer(string bufferName, ComputeBuffer buffer)
    {
        //Debug.Log(bufferName);

        if (buffer != null)
        {
            RTcs.SetBuffer(0, bufferName, buffer);
        }
    }


    private void SetShaderParameters()
    {
        RTcs.SetMatrix("CamToWorld", camera.cameraToWorldMatrix);
        RTcs.SetMatrix("CamInverseProjection", camera.projectionMatrix.inverse);

        RTcs.SetBool("isMultipleRay", isMultipleAA);
        RTcs.SetInt("numberRaysPP", numberRays);


        //set new Skybox
        RTcs.SetTexture(0, "skybox", sBox);
        RTcs.SetFloat("skyboxIllumination", iRateo);
        //RTcs.SetFloat("sBoxIll", iRateo);


        //set random seed
        //RTcs.SetFloat("randomSeed", UnityEngine.Random.Range(0f,100f));
        RTcs.SetFloat("randomSeed", 10);

        //setup lights
        RTcs.SetVector("directionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));

        //set array of spheres
        if (sphereBuffer != null)
            SetComputeBuffer("Spheres", sphereBuffer);
        //RTshader.SetBuffer(0, "Spheres", sphereBuffer);

        //Debug.Log("sphereBuffer count: " + sphereBuffer.count);

        ////objMeshes
        SetComputeBuffer("meshObjects", meshObjBuffer);
        SetComputeBuffer("VetexNormals", VetexNormalsBuffer);
        SetComputeBuffer("vertices", vertexBuffer);
        SetComputeBuffer("indices", indexBuffer);

        SetComputeBuffer("uvs", VertexUVBuffer);


        SetComputeBuffer("BVH", BVHBuffer);
        SetComputeBuffer("BVHidChildren", childrenListBuffer);
    }

    public void RenderCS()
    {
        InitRenderTexture();


        //InitRenderParams();


        RTcs.SetTexture(0, "Result", _target);

        //Setting the texture that stores the ray parameters
        //CreateComputeBuffer(ref _rayOr, _rayOriginsVect, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)));
        //SetComputeBuffer("RayOrNEW", _rayOr);

        //CreateComputeBuffer(ref _rayDir, _rayDirVect, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)));
        //SetComputeBuffer("RayDirNEW", _rayDir);

        //RTcs.SetTexture(0, "RayEnergyMatrix", _rayEnergys);



        RTcs.SetTexture(0, "UVResult", _uvTarget);      //uv da fare ancora (forse no fare, per ora = a normal)
        RTcs.SetTexture(0, "ShadowResult", _shadowTarget);
        RTcs.SetTexture(0, "NormalResult", _normalTarget);

        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        //int threadGroupsX = Screen.width;
        //int threadGroupsY = Screen.height;

        //Vector4[] temp = new Vector4[Screen.width * Screen.height];
        RTcs.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        //PROGRRESSIVE CS
        //for (int iterations = 0; iterations < 8; iterations++)
        //{
        //    RTcs.SetInt("numberIteration", iterations);
        //    RTcs.SetFloat("maxReflections", 8);
        //    RTcs.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        //    //_rayOr.GetData(temp);
        //    //Debug.Log(temp[1051]);
        //}

    }

    private void InitRenderParams()
    {
        //reset params
        _rayEnergys = null;
        _rayOriginsVect = null;
        _rayDirVect = null;

        if (_rayEnergys == null || _rayEnergys.width != Screen.width || _rayEnergys.height != Screen.height)
        {
            if (_rayEnergys != null)
                _rayEnergys.Release();

            _rayEnergys = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            //_rayEnergys = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _rayEnergys.enableRandomWrite = true;
            _rayEnergys.Create();
        }

        if (_rayOriginsVect == null)
        {
            _rayOriginsVect = new List<Vector4>();
            for (int i = 0; i < Screen.width * Screen.height; i++)
                _rayOriginsVect.Add(new Vector4(0, 0, 0, 0));
        }

        if (_rayDirVect == null)
        {
            _rayDirVect = new List<Vector4>();
            for (int i = 0; i < Screen.width * Screen.height; i++)
                _rayDirVect.Add(new Vector4(0, 0, 0, 0));
        }


    }

    private void InitRenderTexture()
    {
        _target = null;
        _uvTarget = null;
        _shadowTarget = null;
        _normalTarget = null;

        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            if (_target != null)
                _target.Release();

            _target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            //_target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.enableRandomWrite = true;
            _target.Create();
        }
        if (_uvTarget == null || _uvTarget.width != Screen.width || _uvTarget.height != Screen.height)
        {
            if (_uvTarget != null)
                _uvTarget.Release();

            _uvTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            //_uvTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _uvTarget.enableRandomWrite = true;
            _uvTarget.Create();
        }
        if (_shadowTarget == null || _shadowTarget.width != Screen.width || _shadowTarget.height != Screen.height)
        {
            if (_shadowTarget != null)
                _shadowTarget.Release();

            _shadowTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            //_shadowTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _shadowTarget.enableRandomWrite = true;
            _shadowTarget.Create();
        }
        if (_normalTarget == null || _normalTarget.width != Screen.width || _normalTarget.height != Screen.height)
        {
            if (_normalTarget != null)
                _normalTarget.Release();

            _normalTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            //_normalTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _normalTarget.enableRandomWrite = true;
            _normalTarget.Create();
        }
    }
}