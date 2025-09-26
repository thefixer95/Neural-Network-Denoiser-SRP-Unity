using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarracudaRenderDenoise : MonoBehaviour
{

    public RenderTexture txt;


    // Start is called before the first frame update
    void Start()
    {
        
    }
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Debug.Log("wwwwwww");
        Graphics.Blit(txt, dest);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
