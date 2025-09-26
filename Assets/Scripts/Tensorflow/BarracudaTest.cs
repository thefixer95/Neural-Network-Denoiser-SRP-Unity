using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;


public class BarracudaTest : MonoBehaviour
{
    public NNModel modelSource;

    public IWorker worker;

    public Dictionary<string, Tensor> inputs;

    public Camera SceneCam;

    private Model model;

    public GameObject imageOut;

    public Texture2D imageIn1spp;
    public Texture2D imageInnormal;
    public Texture2D imageInshadow;
    public Texture2D imageInUV;


    public Texture2D imageInReference;

    private Texture2D textureOut;

    public RenderTexture txtOut;


    // Start is called before the first frame update
    void Start()
    {
        model = ModelLoader.Load(modelSource);



        inputs = new Dictionary<string, Tensor>();
        inputs["ENCODER_INPUT_1spp"] = new Tensor(imageIn1spp);
        inputs["ENCODER_INPUT_normal"] = new Tensor(imageInnormal);
        inputs["ENCODER_INPUT_shadow"] = new Tensor(imageInshadow);
        inputs["ENCODER_INPUT_uv"] = new Tensor(imageInUV);


        textureOut = new Texture2D(imageIn1spp.width, imageIn1spp.height, TextureFormat.RGB24, false);
        txtOut = new RenderTexture(imageIn1spp.width, imageIn1spp.height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        //for (int y = 0; y < imageIn1spp.height; y++)
        //{
        //    for (int x = 0; x < imageIn1spp.width; x++)
        //    {
        //        Color c1spp = imageIn1spp.GetPixel(x, y);
        //        Color cnorm = imageInnormal.GetPixel(x, y);
        //        Color cshad = imageInshadow.GetPixel(x, y);
        //        Color cUV = imageInUV.GetPixel(x, y);

        //        inputs["ENCODER_INPUT_1spp"][0, y, x, 0] = c1spp.r;
        //        inputs["ENCODER_INPUT_1spp"][0, y, x, 1] = c1spp.g;
        //        inputs["ENCODER_INPUT_1spp"][0, y, x, 2] = c1spp.b;

        //        inputs["ENCODER_INPUT_normal"][0, y, x, 0] = cnorm.r;
        //        inputs["ENCODER_INPUT_normal"][0, y, x, 1] = cnorm.g;
        //        inputs["ENCODER_INPUT_normal"][0, y, x, 2] = cnorm.b;

        //        inputs["ENCODER_INPUT_shadow"][0, y, x, 0] = cshad.r;
        //        inputs["ENCODER_INPUT_shadow"][0, y, x, 1] = cshad.g;
        //        inputs["ENCODER_INPUT_shadow"][0, y, x, 2] = cshad.b;

        //        inputs["ENCODER_INPUT_uv"][0, y, x, 0] = cUV.r;
        //        inputs["ENCODER_INPUT_uv"][0, y, x, 1] = cUV.g;
        //        inputs["ENCODER_INPUT_uv"][0, y, x, 2] = cUV.b;


        //        //inputOLD[0,y,x,0] = c1spp.r;
        //        //inputOLD[0,y,x,1] = c1spp.g;
        //        //inputOLD[0,y,x,2] = c1spp.b;
        //        //inputOLD[0,y,x,3] = cnorm.r;
        //        //inputOLD[0,y,x,4] = cnorm.g;
        //        //inputOLD[0,y,x,5] = cnorm.b;
        //        //inputOLD[0,y,x,6] = cshad.r;
        //        //inputOLD[0,y,x,7] = cUV.r;
        //        //inputOLD[0,y,x,8] = cUV.g;

        //        //Debug.Log(c1spp.r);
        //        //Debug.Log(inputs["ENCODER_INPUT_1spp"][0,y,x,0]);

        //    }
        //}

        //Prediction of Output
        worker = WorkerFactory.CreateReferenceComputeWorker(model,true);

        //Debug.Log(model.inputs[0].name + " , " + model.inputs[0].shape);
        //Debug.Log(model.inputs[1].name + " , " + model.inputs[1].shape.ToString());
        //Debug.Log(model.inputs[2].name + " , " + model.inputs[2].shape.ToString());
        //Debug.Log(model.inputs[3].name + " , " + model.inputs[3].shape.ToString());

        worker.Execute(inputs);
        
        Tensor output = worker.PeekOutput();
        //Tensor output = worker.PeekOutput("block_16_lkyReLU_1");


        //output in Texture out
        //for (int y = 0; y < imageIn1spp.height; y++)
        //{
        //    for (int x = 0; x < imageIn1spp.width; x++)
        //    {
        //        Color outPx = new Color(output[0, y, x, 0], output[0, y, x, 1], output[0, y, x, 2]);
        //        //Debug.Log(outPx);
        //        textureOut.SetPixel(x, y, outPx);
        //    }
        //}
        

        txtOut = output.ToRenderTexture(0,0,1,0,null);
        RenderTexture.active = txtOut;
        textureOut.ReadPixels(new Rect(0, 0, txtOut.width, txtOut.height), 0, 0);
        textureOut.Apply();

        Debug.Log(textureOut.GetPixel(0, 0));

        //SceneCam.targetTexture = txtOut;

        var bytes = textureOut.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/rawim.png", bytes);


        Debug.Log(model.layers.Count);
        Debug.Log(model.inputs.Count);

        RawImage im = imageOut.GetComponent<RawImage>();

        im.texture = textureOut;
    }

    // Update is called once per frame
    void Update()
    {

        //worker.Execute(inputs);

        //Tensor output = worker.PeekOutput();
        ////Tensor output = worker.PeekOutput("block_16_lkyReLU_1");


        ////output in Texture out
        ////for (int y = 0; y < imageIn1spp.height; y++)
        ////{
        ////    for (int x = 0; x < imageIn1spp.width; x++)
        ////    {
        ////        Color outPx = new Color(output[0, y, x, 0], output[0, y, x, 1], output[0, y, x, 2]);
        ////        //Debug.Log(outPx);
        ////        textureOut.SetPixel(x, y, outPx);
        ////    }
        ////}


        //txtOut = output.ToRenderTexture(0, 0, 1, 0, null);
        //RenderTexture.active = txtOut;
        //textureOut.ReadPixels(new Rect(0, 0, txtOut.width, txtOut.height), 0, 0);
        //textureOut.Apply();

        //Debug.Log(textureOut.GetPixel(0, 0));

        ////SceneCam.targetTexture = txtOut;

        //var bytes = textureOut.EncodeToPNG();
        //System.IO.File.WriteAllBytes(Application.dataPath + "/rawim.png", bytes);


        //Debug.Log(model.layers.Count);
        //Debug.Log(model.inputs.Count);

        //RawImage im = imageOut.GetComponent<RawImage>();

        //im.texture = textureOut;
    }

}
