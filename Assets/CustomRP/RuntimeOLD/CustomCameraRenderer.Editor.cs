using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.ExceptionServices;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VersionControl;
#endif
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;


//this is the partial class of SRP where all what is rendered in in editor only
partial class CustomCameraRenderer {

    partial void DrawGizmos();

    partial void DrawUnsupportedShaders();

    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();


#if UNITY_EDITOR

    string SampleName { get; set; }


    //static shaders for editor only
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    static Material errorMaterial;

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            //context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }
        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

    }

    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

#else

    string SampleName => bufferName;

#endif

}