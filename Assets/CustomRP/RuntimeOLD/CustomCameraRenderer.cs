using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomCameraRenderer {

    ComputeShader RTcs = (ComputeShader)Resources.Load("RTshader");


    const string bufferName = "Render Camera";
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");


    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };


    ScriptableRenderContext context;
    
    Camera camera;

    CullingResults cullingResults;

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;


        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull())
        {
            return;
        }

        Setup();
        DrawVisibleGeometry();

        DrawUnsupportedShaders();

        DrawGizmos();
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
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void DrawVisibleGeometry()
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId,sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);


        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        //int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        //int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        //shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

}