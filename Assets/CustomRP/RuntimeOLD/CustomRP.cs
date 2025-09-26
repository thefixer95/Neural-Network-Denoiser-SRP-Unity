using UnityEngine;
using UnityEngine.Rendering;


public class CustomRP : RenderPipeline
{
    CustomCameraRenderer renderer = new CustomCameraRenderer();


    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }
}
