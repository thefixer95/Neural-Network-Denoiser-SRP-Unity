using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class CPU_Program_Behave : MonoBehaviour
{
 
    public RenderPipelineAsset exampleAssetA;

    // Start is called before the first frame update
    void Start()
    {
        GraphicsSettings.defaultRenderPipeline = null;
    }
    void Update()
    {
    }

    public void ChangeSceneTo1()
    {
        //GraphicsSettings.renderPipelineAsset = exampleAssetA;
        SceneManager.LoadScene("SingleObjects", LoadSceneMode.Single);

    }
    public void ChangeSceneTo2()
    {
        //GraphicsSettings.renderPipelineAsset = exampleAssetA;
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);

    }
    public void ChangeSceneTo3()
    {
        //GraphicsSettings.renderPipelineAsset = exampleAssetA;
        SceneManager.LoadScene("BunnyScene", LoadSceneMode.Single);

    }

}
