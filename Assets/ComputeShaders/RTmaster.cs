//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.Assertions.Must;
//using UnityEngine.Experimental.GlobalIllumination;
//using UnityEngine.UI;

//public class RTmaster : MonoBehaviour
//{
//    public ComputeShader RTshader;

//    // skybox texture
//    public Texture SkyboxTxt;
//    [Range(0,1)]
//    public float illuminationSkyboxRatio;
    
//    // Scene Light
//    public Light directionalLight;
//    Vector3 l;

//    //texture render target
//    private RenderTexture _target;
//    private RenderTexture converged;

//    //cam
//    private Camera cam;

//    //public var for the ground
//    public GameObject groundInfPlane;
//    private Ground GroundData;
//    private ComputeBuffer groundBuffer;
//    private struct Ground
//    {
//        public float positionY;
//        public Vector3 specular;
//        public Vector3 albedo;
//    }

//    //statics for mesh renders
//    private static bool meshObjNeedRebuild = false;
//    private static List<RTObjects> RTobjects = new List<RTObjects>();

//    //function for taking meshes in scene
//    public static void RegisterObject(RTObjects obj)
//    {
//        RTobjects.Add(obj);
//        meshObjNeedRebuild = true;
//    }
//    public static void UnregisterObject(RTObjects obj)
//    {
//        RTobjects.Remove(obj);
//        meshObjNeedRebuild = true;
//    }

//    //mesh struct
//    private struct MeshObj
//    {
//        public Matrix4x4 localToWorldMat;
//        public int indices_offset;
//        public int indices_count;
//        public Vector3 specular;
//        public Vector3 albedo;
//        public Vector3 emission;
//        public float smoothness;
//        public float materialType;
//        //public Texture2D texture;
//    }

//    //mesh buffers
//    private static List<MeshObj> meshObjs = new List<MeshObj>();
//    private static List<Vector3> vertices = new List<Vector3>();
//    private static List<int> indices = new List<int>();
//    private static List<Vector3> verticesNorm = new List<Vector3>();

//    private ComputeBuffer meshObjBuffer;
//    private ComputeBuffer vertexBuffer;
//    private ComputeBuffer indexBuffer;
//    private ComputeBuffer VetexNormalsBuffer;


//    //function to create buffers of mesh
//    private void RebuildMeshObjBuffers()
//    {
//        if (!meshObjNeedRebuild)
//        {
//            return;
//        }

//        meshObjNeedRebuild = false;
//        meshObjs.Clear();
//        vertices.Clear();
//        indices.Clear();

//        //loop all objects in game to take data
//        foreach (RTObjects obj in RTobjects)
//        {
//            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
//            //vertex data
//            int firstVertex = vertices.Count;
//            vertices.AddRange(mesh.vertices);

//            int firstIndex = indices.Count;
//            var inds = mesh.GetIndices(0);

//            //int firstVertexNorm = verticesNorm.Count;
//            verticesNorm.AddRange(mesh.normals);

//            //print(mesh.normals.Length);
//            //print(mesh.vertices.Length);

//            //mesh.normals

//            indices.AddRange(inds.Select(index => index + firstVertex));

//            meshObjs.Add(new MeshObj()
//            {
//                localToWorldMat = obj.transform.localToWorldMatrix,
//                indices_offset = firstIndex,
//                indices_count = inds.Length,
//                specular = obj.specular,
//                albedo = obj.albedo,
//                emission = obj.emissionOut,
//                smoothness = obj.smoothness,
//                materialType = obj.matType,
//            });
//        }

//        CreateComputeBuffer(ref meshObjBuffer, meshObjs, 116);
//        CreateComputeBuffer(ref VetexNormalsBuffer, verticesNorm, 12);
//        CreateComputeBuffer(ref vertexBuffer, vertices, 12);
//        CreateComputeBuffer(ref indexBuffer, indices, 4);

//    }

//    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
//        where T : struct
//    {
//        //check if compute buffer exist
//        if (buffer != null)
//        {
//            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
//            {
//                buffer.Release();
//                buffer = null;
//            }
//        }

//        if (data.Count != 0)
//        {
//            if (buffer == null)
//            {
//                buffer = new ComputeBuffer(data.Count, stride);
//            }
//            buffer.SetData(data);
//        }
//    }

//    private void SetComputeBuffer(string name, ComputeBuffer buffer)
//    {
//        if (buffer != null)
//        {
//            RTshader.SetBuffer(0, name, buffer);
//        }
//    }

//    // sphere buffer
//    private ComputeBuffer sphereBuffer;
//    // sphere structure
//    private struct Sphere
//    {
//        public Vector3 center;
//        public float radius;
//        public Vector3 specular;
//        public Vector3 albedo;
//        public float smoothness;
//        public Vector3 emission;
//        public float materialType;
//    }

//    private void CreateSphereBuffer()
//    {
//        //sphere setup
//        List<Sphere> spheres = new List<Sphere>();

//        foreach (GameObject go in GameObject.FindGameObjectsWithTag("SphereInScene"))
//        {
//            Sphere s = new Sphere();
//            s.center = go.transform.position;
//            s.radius = go.transform.localScale.x/2;
//            s.albedo = go.GetComponent<SphereParameters>().albedo;
//            s.specular = go.GetComponent<SphereParameters>().specular;
//            s.smoothness = go.GetComponent<SphereParameters>().smoothness;
//            s.materialType = go.GetComponent<SphereParameters>().matType;
//            s.emission = go.GetComponent<SphereParameters>().emissionOut;

//            spheres.Add(s);
//        }
//        CreateComputeBuffer(ref sphereBuffer, spheres, 60);
//        //sphereBuffer = new ComputeBuffer(spheres.Count, 60);
//        //sphereBuffer.SetData(spheres);
//    }


//    //all is set from the camera on awake
//    private void Awake()
//    {
//        cam = GetComponent<Camera>();
//    }

//    //when you enable an object set the scene
//    private void OnEnable()
//    {
//        SetupScene();
//    }

//    //at the start setup the scene
//    private void Start()
//    {
//        SetupScene();
//    }

//    private void ReleaseBuffers()
//    {
//        if (sphereBuffer != null)
//            sphereBuffer.Release();
//        if (groundBuffer != null)
//            groundBuffer.Release();
//        if (indexBuffer != null)
//            indexBuffer.Release();
//        if (meshObjBuffer != null)
//            meshObjBuffer.Release();
//        if (vertexBuffer != null)
//            vertexBuffer.Release();
//        if (VetexNormalsBuffer != null)
//            VetexNormalsBuffer.Release();
//    }

//    //when disable something release the buffers
//    private void OnDisable()
//    {
//        ReleaseBuffers();
//    }


//    private void SetupScene()
//    {
//        //light setup
//        l = directionalLight.transform.forward; //Returns a normalized vector representing the blue axis of the transform in world space

//        //plane setup
//        GroundData = new Ground();
//        GroundData.positionY = groundInfPlane.transform.position.y;
//        GroundData.albedo = new Vector3(0.1f, 0.1f, 0.1f);
//        GroundData.specular = new Vector3(0.04f, 0.04f, 0.04f);

//        //sphere buffer
//        CreateSphereBuffer();

//        //mesh buffer
//        RebuildMeshObjBuffers();
//    }


//    private void SetShaderParameters()
//    {
//        //set camera world
//        RTshader.SetMatrix("CamToWorld", cam.cameraToWorldMatrix);
//        RTshader.SetMatrix("CamInverseProjection", cam.projectionMatrix.inverse);

//        //set random seed
//        RTshader.SetFloat("randomSeed", 10);

//        //set new Skybox
//        RTshader.SetTexture(0, "skybox", SkyboxTxt);
//        RTshader.SetFloat("sBoxIll", illuminationSkyboxRatio);

//        // setup the infinite ground of the scene
//        RTshader.SetVector("groundSpecular", GroundData.specular);
//        RTshader.SetVector("groundAlbedo", GroundData.albedo);
//        RTshader.SetFloat("groundY", GroundData.positionY);

//        //setup lights
//        RTshader.SetVector("directionalLight", new Vector4(l.x,l.y,l.z, directionalLight.intensity));

//        //set array of spheres
//        SetComputeBuffer("Spheres", sphereBuffer);
//        //RTshader.SetBuffer(0, "Spheres", sphereBuffer);

//        //set objs mesh
//        SetComputeBuffer("meshObjects", meshObjBuffer);
//        SetComputeBuffer("VetexNormals", VetexNormalsBuffer);
//        SetComputeBuffer("vertices", vertexBuffer);
//        SetComputeBuffer("indices", indexBuffer);

//    }

//    private void OnRenderImage(RenderTexture source, RenderTexture destination)
//    {
//        //RebuildMeshObjBuffers();
//        SetShaderParameters();
//        Render(destination);
//    }

//    private void Render(RenderTexture destination)
//    {
//        InitRenderTexture();

//        RTshader.SetTexture(0, "Result", _target);
//        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
//        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
//        RTshader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

//        Graphics.Blit(_target, converged);
//        Graphics.Blit(converged, destination);
//    }

//    private void InitRenderTexture()
//    {
//        print(Screen.width+"   "+ Screen.height);

//        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
//        {
//            if (_target != null)
//                _target.Release();

//            _target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
//            _target.enableRandomWrite = true;
//            _target.Create();
//        }
//    }

//    void Update()
//    {
//        l = directionalLight.transform.forward; //Returns a normalized vector representing the blue axis of the transform in world space
//        //here if i want to animate something I need to set the scene each frame
//        SetupScene();
//    }

//}
