using Unity.Barracuda;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName ="Rendering/Custom Render Pipeline Compute")]
public class CustomRPAsset : RenderPipelineAsset
{
    [System.Serializable]
    public class DenoiserModelAttributes
    {
        public WorkerFactory.Type workerType = WorkerFactory.Type.ComputePrecompiled;
        public NNModel nnModelSource;
        [HideInInspector] public Vector4 postNetworkColorBias;
        public bool forceBilinearUpsample2DInModel = false;
        public ComputeInfo.ChannelsOrder channelsOrder = ComputeInfo.ChannelsOrder.NHWC;
        public bool shouldSaveStyleTransferDataAsAsset = false;
        public bool shouldUseSRGBTensor = false;
        public bool verboseMode = false;
        public ComputeShader tensorToTextureSRGB;
    }


    public ComputeShader computeShader;

    public Texture skybox;

    [Range(0, 1)]
    public float illuminationRat= 1.0f;



    public bool multipleRayAA = false;

    public int numberOfRays = 1;

    [Header("Denoising Attributes")]
    //public NNModel nnMod;
    //public GameObject CPU;
    public bool isDenoisingModeOn;
    public DenoiserModelAttributes internalSetup;




    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRPCS(computeShader, skybox, illuminationRat, multipleRayAA, numberOfRays, isDenoisingModeOn, internalSetup);
    }
}

