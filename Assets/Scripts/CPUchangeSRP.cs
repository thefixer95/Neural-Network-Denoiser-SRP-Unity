using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CPUchangeSRP : MonoBehaviour
{
    public RenderPipelineAsset exampleAssetA;
    // Start is called before the first frame update
    void Start()
    {
        GraphicsSettings.defaultRenderPipeline = exampleAssetA;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
