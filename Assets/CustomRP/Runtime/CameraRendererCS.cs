using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Unity.Barracuda;
#if UNITY_EDITOR
using UnityEditor;
#endif
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRendererCS
{

    ComputeShader RTcs;

    //rasterized Texture
    //private readonly string renderTexturePropertyName = "_Test";
    private int renderTexturePropertyID;
    private RenderTargetIdentifier renderTextureID;
    RenderTexture _raster;

    //Skybox features
    Texture sBox;
    float iRateo;

    bool setBuffers = false;

    //Buffer name and shaders using
    const string bufferName = "Render Camera";
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");


    //Output Render Texture
    RenderTexture _outputRender;


    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };


    ScriptableRenderContext context;

    Camera camera;

    Camera sceneviewCam;

    CullingResults cullingResults;

    public void Render(ScriptableRenderContext context, Camera camera, ComputeShader cs, Texture txt, float f, Model mod, bool isDenoising, bool mRay, int nR, CustomRPAsset.DenoiserModelAttributes nnAttributes)
    {
        //if (rTObjects.Count < 1)
        //{
        //    return;
        //}

        //check the renderTexture for rasterization

        //Debug.Log("rendering");

        this.context = context;
        this.camera = camera;

        RTcs = cs;
        sBox = txt;
        iRateo = f;

        isMultipleAA = mRay;
        numberRays = nR;

        autoencoderModel = mod;
        Denoiser = isDenoising;
        modelAttributes = nnAttributes;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull())
        {
            return;
        }

        Setup();

        if (Application.isPlaying)
        {

            //compute Shader for ray tracing
            SetAllComputeShader();
            RenderCS();


            ////RASTERIZATION TO RENDER TEXTURE

            //renderTexturePropertyID = Shader.PropertyToID(renderTexturePropertyName);

            //renderTextureID = new RenderTargetIdentifier(renderTexturePropertyID);

            //if (_raster != null)
            //    _raster.Release();

            if (_raster == null || _raster.width != Screen.width || _raster.height != Screen.height)
            {
                if (_raster != null)
                    _raster.Release();

                _raster = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                //_target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);                
                _raster.enableRandomWrite = true;
                _raster.Create();

                //Shader.SetGlobalTexture(renderTexturePropertyID, _raster);
            }


            //DrawVisibleGeometryRaster();

            //buffer.GetTemporaryRT(renderTexturePropertyID, camera.pixelWidth, camera.pixelHeight, 24);
            //buffer.SetRenderTarget(renderTextureID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);



            if (Denoiser)
            {
                //Denoising image out of Compute shader
                SetupDenoiser();
                DenoisingModel();
                _outputRender = _denoisedRenderTexture;
            }
            else
            {
                _outputRender = _target;
            }

            //_outputRender = _raster;

        }
        else
        {
#if UNITY_EDITOR
            EditorUtility.UnloadUnusedAssetsImmediate();
#endif
            GC.Collect();
            setBuffers = false;
            ReleaseBuffers();
            DestroyWorkersAndModels();
            DrawVisibleGeometry();
            DrawUnsupportedShaders();
            DrawGizmos();
        }
        Submit();

    }



    bool Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        //buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();

        GC.Collect();
    }

    private void ExecuteBuffer()
    {

        buffer.Blit(_outputRender, BuiltinRenderTextureType.CameraTarget);
        //if (!Application.isPlaying)
        //{
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        //buffer.ReleaseTemporaryRT(renderTexturePropertyID);
        //}
    }


    private void DrawVisibleGeometryRaster()
    {

        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);


    }




    private void DrawVisibleGeometry()
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);


        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

}