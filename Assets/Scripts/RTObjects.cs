using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


//[RequireComponent(typeof(MeshRenderer))]
//[RequireComponent(typeof(MeshFilter))]

public class RTObjects : MonoBehaviour
{
    public enum BRDFused
    {
        Lambert,
        Phong,
    };

    public BRDFused typeOfBRDF = BRDFused.Phong;

    public enum MaterialType
    {
        Metal,
        Plastic,
        //Glass, //NOT IMPLEMENTED YET
    };

    public MaterialType materialType;

    public Color materialColor = new Color(0, 0, 0,1);
    [System.NonSerialized]
    public Vector3 specular;
    [System.NonSerialized]
    public Vector3 albedo;

    [Range(0, 1)]
    public float smoothness = 0;
    public Color emission = new Color(0, 0, 0,1);
    [System.NonSerialized]
    public Vector3 emissionOut;
    [Range(0, 100)]
    public float emissionForce;

    [System.NonSerialized]
    public float matType = 0;

    public Texture2D textureImage;

    private void OnEnable()
    {
        CameraRendererCS.RegisterObjectForRT(this);
    }

    private void OnDisable()
    {
        CameraRendererCS.UnregisterObjectForRT(this);
    }
    //Start is called before the first frame update

    Scene currentScene;

    void Start()
    {

        currentScene = SceneManager.GetActiveScene();
        //if (gameObject.activeSelf)
        //    CameraRendererCS.RegisterObjectForRT(this);

        //CameraRendererCS.rTObjects.Add(this);
        //CameraRendererCS.meshNeedRebuild = true;


        emissionOut = new Vector3(emission.r, emission.g, emission.b);
        emissionOut *= emissionForce;

        switch (typeOfBRDF)
        {
            case BRDFused.Phong:
                {
                    //usually phong is used for metal
                    matType = 1;        //IS PHONG
                    break;
                }
            case BRDFused.Lambert:
                {
                    //usually lambert is used for not metal
                    matType = 0;        //IS LAMBERT
                    break;
                }
        }

        switch (materialType)
        {
            case MaterialType.Metal:
                {
                    albedo = Vector3.zero;
                    specular = new Vector3(materialColor.r, materialColor.g, materialColor.b);
                    break;
                }
            case MaterialType.Plastic:
                {
                    albedo = new Vector3(materialColor.r, materialColor.g, materialColor.b);
                    specular = Vector3.one * 0.04f;
                    break;
                }
        }

        //CameraRendererCS.RegisterObjectForRT(this);
        //bool metal = matType > 0.5f;
        //albedo = metal ? Vector3.zero : new Vector3(materialColor.r, materialColor.g, materialColor.b);
        //specular = metal ? new Vector3(materialColor.r, materialColor.g, materialColor.b) : Vector3.one * 0.04f;

        //RTmaster.RegisterObject(this);
        //Debug.Log(this.GetComponent<Renderer>().material.shader);
    }

    // Update is called once per frame
    void Update()
    {
        string sceneName = currentScene.name;

        if (sceneName == "BunnyScene")
        {
            //Debug.Log("CULOO");
            CameraRendererCS.UpdateOBJs();
            //CameraRendererCS.UnregisterObjectForRT(this);
            //CameraRendererCS.RegisterObjectForRT(this);
        }
    }
}
