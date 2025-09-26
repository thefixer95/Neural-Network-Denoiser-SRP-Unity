using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.Profiling;

partial class CameraRendererCS
{
    //Public variables moddable in CustomRPAssetCS.cs
    bool Denoiser;
    Model autoencoderModel;
    CustomRPAsset.DenoiserModelAttributes modelAttributes;

    //Privates
    private IWorker worker;
    private Dictionary<string, Tensor> inputs;
    private Tensor input;
    private Tensor output;
    //private List<string> layerNameToPatch;


    ////NOT USED
    //private bool firstWorker = true;
    //private IEnumerator inferenceCoroutine;
    //private int inferenceCurrentLayer;
    //private float[] modelLayerTimingsPercent = new float[]
    //{
    //    0.0f,
    //    0.0f,
    //    0.0f,
    //    0.0f,
    //    0.0f,
    //    0.021941f,
    //    0.017204f,
    //    0.036529f,
    //    0.009227f,
    //    0.042545f,
    //    0.009238f,
    //    0.042679f,
    //    0.009394f,
    //    0.011978f,
    //    0.042437f,
    //    0.009405f,
    //    0.042346f,
    //    0.009383f,
    //    0.011913f,
    //    0.042405f,
    //    0.009378f,
    //    0.042512f,
    //    0.009330f,
    //    0.011924f,
    //    0.042443f,
    //    0.009388f,
    //    0.042427f,
    //    0.009319f,
    //    0.011907f,
    //    0.042427f,
    //    0.009383f,
    //    0.042448f,
    //    0.009405f,
    //    0.011994f,
    //    0.011854f,
    //    0.099505f,
    //    0.017209f,
    //    0.024267f,
    //    0.171338f,
    //    0.012918f,
    //};




    //output TXT
    RenderTexture _denoisedRenderTexture;


    public void DenoisingModel()
    {
        //inputs
        OnRenderImageNormal();
        //output the new image
    }

    public void SetupDenoiser()
    {
        if (_denoisedRenderTexture == null || _denoisedRenderTexture.width != Screen.width || _denoisedRenderTexture.height != Screen.height)
        {
            if (_denoisedRenderTexture != null)
                _denoisedRenderTexture.Release();
            _denoisedRenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            _denoisedRenderTexture.enableRandomWrite = true;
            _denoisedRenderTexture.Create();
        }
        if (worker != null)
        {
            return;
        }
        PrecompileDenoiserModelAndWorker();
        Debug.Log("worker created");
    }


    private void OnRenderImageNormal()
    {
        inputs = new Dictionary<string, Tensor>();
        input = new Tensor(_target, 3);
        //CustomPinTensorFromTexture(input);
        inputs.Add("ENCODER_INPUT_1spp", input);
        input = new Tensor(_normalTarget, 3);
        //CustomPinTensorFromTexture(input);
        inputs.Add("ENCODER_INPUT_normal", input);
        input = new Tensor(_shadowTarget, 3);
        //CustomPinTensorFromTexture(input);
        inputs.Add("ENCODER_INPUT_shadow", input);
        input = new Tensor(_uvTarget, 3);
        //CustomPinTensorFromTexture(input);
        inputs.Add("ENCODER_INPUT_uv", input);
        input = new Tensor(_raster, 3);
        //CustomPinTensorFromTexture(input);
        inputs.Add("ENCODER_INPUT_raster", input);
        input.Dispose();


        worker.Execute(inputs);
        //output = new Tensor();
        output = worker.PeekOutput();
        //_denoisedRenderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        //_outputRender = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

        output.ToRenderTexture(_denoisedRenderTexture);

        //CustomTensorToRenderTexture(output, _denoisedRenderTexture, 0, 0, Vector4.one, modelAttributes.postNetworkColorBias);


        DisposeInputTensor(inputs);
        output.Dispose();
    }

    //DA CONTROLLARE
    private void CustomTensorToRenderTexture(Tensor X, RenderTexture target, int batch, int fromChannel, Vector4 scale, Vector4 bias, Texture3D lut = null)
    {
        if (!modelAttributes.shouldUseSRGBTensor)
        {
            X.ToRenderTexture(target, batch, fromChannel, scale, bias, lut);
            return;
        }

        //By default Barracuda work on Tensor containing value in linear color space.
        //Here we handle custom convertion from tensor to texture when tensor is in sRGB color space.
        //This is important for this demo as network was trained with data is sRGB color space.
        //Direct support for this will be added in a latter revision of Barracuda.
        if (!target.enableRandomWrite || !target.IsCreated())
        {
            target.Release();
            target.enableRandomWrite = true;
            target.Create();
        }

        var gpuBackend = new ReferenceComputeOps(ComputeShaderSingleton.Instance.referenceKernels);
        var fn = new CustomComputeKernel(modelAttributes.tensorToTextureSRGB, "TensorToTexture" + (lut == null ? "NoLUT" : "3DLUT"));
        var XonDevice = gpuBackend.Pin(X);
        fn.SetTensor("X", X.shape, XonDevice.buffer, XonDevice.offset);
        fn.shader.SetTexture(fn.kernelIndex, "Otex2D", target);
        fn.shader.SetVector("_Scale", scale);
        fn.shader.SetVector("_Bias", bias);
        fn.shader.SetInts("_Pad", new int[] { batch, 0, 0, fromChannel });
        fn.shader.SetBool("_FlipY", true);
        if (lut != null)
        {
            fn.shader.SetTexture(fn.kernelIndex, "Otex3D", lut);
            fn.shader.SetVector("_LutParams", new Vector2(1f / lut.width, lut.width - 1f));
        }

        fn.Dispatch(target.width, target.height, 1);

    }
    //DA CONTROLLARE
    private void CustomPinTensorFromTexture(Tensor X)
    {
        if (!modelAttributes.shouldUseSRGBTensor)
            return;

        //By default Barracuda work on Tensor containing value in linear color space.
        //Here we handle custom tensor Pin from texture when tensor is to contain data in sRGB color space.
        //This is important for this demo as network was trained with data is sRGB color space.
        //Direct support for this will be added in a latter revision of Barracuda.  
        var onDevice = X.tensorOnDevice as ComputeTensorData;
        Debug.Assert(onDevice == null);

        var asTexture = X.tensorOnDevice as TextureAsTensorData;
        Debug.Assert(asTexture != null);

        X.AttachToDevice(CustomTextureToTensorData(asTexture, X.name));


    }

    //DA CONTROLLARE
    private class CustomComputeKernel
    {
        public readonly int kernelIndex;
        public readonly ComputeShader shader;

        private readonly string kernelName;
        private readonly uint threadGroupSizeX;
        private readonly uint threadGroupSizeY;
        private readonly uint threadGroupSizeZ;

        public CustomComputeKernel(ComputeShader cs, string kn)
        {
            string kernelNameWithChannelsOrder = kn + (ComputeInfo.channelsOrder == ComputeInfo.ChannelsOrder.NHWC ? "_NHWC" : "_NCHW");
            if (!cs.HasKernel(kernelNameWithChannelsOrder) && !cs.HasKernel(kn))
                throw new ArgumentException($"Kernel {kn} and {kernelNameWithChannelsOrder} are both missing");

            shader = cs;
            kernelName = cs.HasKernel(kernelNameWithChannelsOrder) ? kernelNameWithChannelsOrder : kn;
            kernelIndex = shader.FindKernel(kernelName);
            shader.GetKernelThreadGroupSizes(kernelIndex, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);
        }

        private static int IntDivCeil(int v, int div)
        {
            return (v + div - 1) / div;
        }

        public void Dispatch(int workItemsX, int workItemsY, int workItemsZ)
        {
            Profiler.BeginSample(kernelName);
            var x = IntDivCeil(workItemsX, (int)threadGroupSizeX);
            var y = IntDivCeil(workItemsY, (int)threadGroupSizeY);
            var z = IntDivCeil(workItemsZ, (int)threadGroupSizeZ);
            shader.Dispatch(kernelIndex, x, y, z);
            Profiler.EndSample();
        }

        public void SetTensor(string name, TensorShape shape, ComputeBuffer buffer, Int64 dataOffset = 0)
        {
            var shapeId = Shader.PropertyToID(name + "declShape");
            var infoId = Shader.PropertyToID(name + "declInfo");
            var dataId = Shader.PropertyToID(name + "data");
            int[] tensorShape = { shape.batch, shape.height, shape.width, shape.channels };
            int[] tensorInfo = { (int)dataOffset, shape.length };
            shader.SetInts(shapeId, tensorShape);
            shader.SetInts(infoId, tensorInfo);
            shader.SetBuffer(kernelIndex, dataId, buffer);
        }
    }
    ITensorData CustomTextureToTensorData(TextureAsTensorData texData, string name)
    {
        //By default Barracuda work on Tensor containing value in linear color space.
        //Here we handle custom tensor Pin from texture when tensor is to contain data in sRGB color space.
        //This is important for this demo as network was trained with data is sRGB color space.
        //Direct support for this will be added in a latter revision of Barracuda. 
        var fn = new CustomComputeKernel(modelAttributes.tensorToTextureSRGB, "TextureToTensor");
        var tensorData = new ComputeTensorData(texData.shape, name, ComputeInfo.channelsOrder, false);

        fn.SetTensor("O", texData.shape, tensorData.buffer);
        fn.shader.SetBool("_FlipY", texData.flip == TextureAsTensorData.Flip.Y);

        var offsets = new int[] { 0, 0, 0, 0 };
        foreach (var tex in texData.textures)
        {
            var texArr = tex as Texture2DArray;
            var tex3D = tex as Texture3D;
            var rt = tex as RenderTexture;

            var texDepth = 1;
            if (texArr)
                texDepth = texArr.depth;
            else if (tex3D)
                texDepth = tex3D.depth;
            else if (rt)
                texDepth = rt.volumeDepth;

            //var srcChannelMask = TextureFormatUtils.FormatToChannelMask(tex, texData.interpretPixelAsChannels);
            Color srcChannelMask = Color.white;

            fn.shader.SetTexture(fn.kernelIndex, "Xtex2D", tex);
            fn.shader.SetInts("_Pool", new int[] { tex.width, tex.height });
            fn.shader.SetInts("_Pad", offsets);
            fn.shader.SetInts("_ChannelWriteMask", new[] { (int)srcChannelMask[0], (int)srcChannelMask[1], (int)srcChannelMask[2], (int)srcChannelMask[3] });

            fn.Dispatch(texData.shape.width, texData.shape.height, texDepth);

            if (texData.interpretDepthAs == TextureAsTensorData.InterpretDepthAs.Batch)
                offsets[0] += texDepth;
            else if (texData.interpretDepthAs == TextureAsTensorData.InterpretDepthAs.Channels)
                offsets[3] += texDepth * texData.interpretPixelAsChannels;
        }

        return tensorData;
    }



    //private int FindLayerIndexByName(List<Layer> list, string name)
    //{
    //    int res = 0;
    //    while (res < list.Count && list[res].name != name)
    //        res++;
    //    return res;
    //}

    private void PrecompileDenoiserModelAndWorker()
    {
        modelAttributes.postNetworkColorBias = new Vector4(0.4850196f, 0.4579569f, 0.4076039f, 0.0f);
        ComputeInfo.channelsOrder = modelAttributes.channelsOrder;

        ////model already loaded in CustoRPCS.cs
        //List<Layer> layerList = new List<Layer>(autoencoderModel.layers);

        ////Debug.Log(autoencoderModel.layers.Count);
        ////PreProcessing Network for run-time use
        //Layer lastConv = null;
        ////for instead of foreach so the array can be resized
        //for (int i = 0; i < layerList.Count; i++)
        //{
        //    Layer layer = layerList[i];

        //    //Fix Upsample2D size parameters
        //    if (layer.type == Layer.Type.Upsample2D || layer.type == Layer.Type.MaxPool2D)
        //    {
        //        layer.pool = new[] { 2, 2 };

        //        //WHAT??? (not changing anything)
        //        //ref model is supposed to be nearest sampling but bilinear scale better when network is applied at lower resoltions
        //        bool useBilinearUpsample = modelAttributes.forceBilinearUpsample2DInModel;
        //        layer.axis = useBilinearUpsample ? 1 : -1;
        //    }


        //    //if the layer is a convolution (need to find the last conv layer
        //    //else if (layer.type == Layer.Type.Conv2D || layer.type == Layer.Type.Conv2DTrans)
        //    //{
        //    //    //layer.pad = new[] { 1, 1 };
        //    //    lastConv = layer;
        //    //    //Debug.Log(layer.datasets.Length);
        //    //}
        //    //else if ((layer.type == Layer.Type.Normalization || layer.type == Layer.Type.ScaleBias))
        //    //{
        //    //    //Debug.Log(layer.name + " layer of type: " + layer.type);
        //    //    //Debug.Log(layerList[i - 1].name + " layer (-1) of type: " + layerList[i - 1].type);

        //    //    if (layerList[i - 1].type == Layer.Type.StridedSlice)
        //    //    {
        //    //        //qualcosa di mancante qui?
        //    //    }
        //    //    else
        //    //    {
        //    //        //Debug.Log(lastConv.datasets.Length);
        //    //        int channels = lastConv.datasets[1].shape.channels;
        //    //        layer.datasets = new Layer.DataSet[2];

        //    //        layer.datasets[0].shape = new TensorShape(1, 1, 1, channels);
        //    //        layer.datasets[0].offset = 0;
        //    //        layer.datasets[0].length = channels;

        //    //        layer.datasets[1].shape = new TensorShape(1, 1, 1, channels);
        //    //        layer.datasets[1].offset = channels;
        //    //        layer.datasets[1].length = channels;

        //    //        float[] data = new float[channels * 2];
        //    //        for (int j = 0; j < data.Length / 2; j++)
        //    //            data[j] = 1.0f;
        //    //        for (int j = data.Length / 2; j < data.Length; j++)
        //    //            data[j] = 0.0f;
        //    //        layer.weights = data;
        //    //    }
        //    //}
        //}

        //// Fold Relu into instance normalisation
        //Dictionary<string, string> reluToInstNorm = new Dictionary<string, string>();
        //for (int i = 0; i < layerList.Count; i++)
        //{
        //    Layer layer = layerList[i];
        //    if (layer.type == Layer.Type.Activation && layer.activation == Layer.Activation.Relu)
        //    {
        //        if (layerList[i - 1].type == Layer.Type.Normalization || layerList[i - 1].type == Layer.Type.ScaleBias)
        //        {
        //            layerList[i - 1].activation = layer.activation;
        //            reluToInstNorm[layer.name] = layerList[i - 1].name;
        //            layerList.RemoveAt(i);
        //            i--;
        //        }
        //    }
        //}
        //for (int i = 0; i < layerList.Count; i++)
        //{
        //    Layer layer = layerList[i];
        //    for (int j = 0; j < layer.inputs.Length; j++)
        //    {
        //        if (reluToInstNorm.ContainsKey(layer.inputs[j]))
        //        {
        //            layer.inputs[j] = reluToInstNorm[layer.inputs[j]];
        //        }
        //    }
        //}


        //////?????   QUI POTREI TOGLIERE GLI INPUT E RIASSEGNARLI SPECIFICI
        ////// Feed first convolution directly with input (no need for normalisation from the model)
        ////string firstConvName = "StyleNetwork/conv1/convolution_conv1/convolution";
        ////int firstConv = FindLayerIndexByName(layerList, firstConvName);
        ////layerList[firstConv].inputs = new[] { model.inputs[1].name };

        ////if (modelToUse == UsedModel.Reference)
        ////{
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalisation/add"));
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalisation/add/y"));
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalisation/normalized_contentFrames"));
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalisation/normalized_contentFrames/y"));
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalisation/sub"));
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalisation/sub/y"));
        ////}
        ////if (modelToUse == UsedModel.RefBut32Channels)
        ////{
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalized_contentFrames"));
        ////    layerList.RemoveAt(FindLayerIndexByName(layerList, "StyleNetwork/normalized_contentFrames/y"));
        ////}

        ////// Remove final model post processing, post process happen in tensor to texture instead
        ////int postAdd = FindLayerIndexByName(layerList, "StyleNetwork/clamp_0_255/add");
        ////layerList.RemoveRange(postAdd, 5);

        //// Correct wrong output layer list
        //autoencoderModel.outputs = new List<string>() { layerList[layerList.Count - 1].name };
        ////Debug.Log(autoencoderModel.outputs[0]);
        //autoencoderModel.layers = layerList;
        ////Debug.Log(autoencoderModel.layers.Count);


        //PARTE IMPORTANTE
        worker = WorkerFactory.CreateWorker(WorkerFactory.ValidateType(modelAttributes.workerType), autoencoderModel, modelAttributes.verboseMode);
        //worker = WorkerFactory.CreateReferenceComputeWorker(autoencoderModel);


        //layerList.Clear();
        //GC.Collect();
    }

    public void DestroyWorkersAndModels()
    {
        if (worker == null)
        {
            return;
        }
        autoencoderModel = null;
        _denoisedRenderTexture.Release();
        worker.Dispose();
    }




    void DisposeInputTensor(Dictionary<string, Tensor> t)
    {
        foreach (var key in t.Keys)
        {
            t[key].Dispose();
        }
        t.Clear();
    }
}
