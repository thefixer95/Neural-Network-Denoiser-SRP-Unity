using Unity.Barracuda;
using UnityEngine;
using UnityEngine.Rendering;


public class CustomRPCS : RenderPipeline
{
    
    CameraRendererCS renderer = new CameraRendererCS();

    ComputeShader cs;

    Texture sBoxTxt;

    float illRat;

    Model autoenc;

    bool isDenoising;

    bool FirstTime = false;

    bool multipleRay;

    int numberRay;

    CustomRPAsset.DenoiserModelAttributes NNattributes;

    public CustomRPCS(ComputeShader RTcs, Texture sBox, float iRat, bool pathAA, int nRay, bool denoisingModeOn, CustomRPAsset.DenoiserModelAttributes denoisingAttr)
    {
        cs = RTcs;

        sBoxTxt = sBox;

        illRat = iRat;

        isDenoising = denoisingModeOn;

        multipleRay = pathAA;

        numberRay = nRay;

        NNattributes = denoisingAttr;
    }
    

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        if (!FirstTime)
        {
            autoenc = ModelLoader.Load(NNattributes.nnModelSource);
            //worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, autoenc);

            Debug.Log("Model Loaded");

            FirstTime = true;
        }

        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, cs, sBoxTxt, illRat, autoenc, isDenoising, multipleRay, numberRay, NNattributes);
        }
    }
}
