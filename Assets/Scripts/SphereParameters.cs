using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereParameters : MonoBehaviour
{
    public enum BRDFused
    {
        Lambert,
        Phong,
    };

    public BRDFused typeOfBRDF;


    public enum MaterialType
    {
        Metal,
        Plastic,
        //Glass, //NOT IMPLEMENTED YET
    };

    public MaterialType materialType;

    public Color materialColor = new Color(0, 0, 0);
    [System.NonSerialized]
    public Vector3 specular;
    [System.NonSerialized]
    public Vector3 albedo;
    [Range(0, 1)]
    public float smoothness = 0;
    public Color emission = new Color(0,0,0);
    [System.NonSerialized]
    public Vector3 emissionOut;
    [Range(0, 100)]
    public float emissionForce;
    [System.NonSerialized]
    public float matType = 0;

    private void OnEnable()
    {
        CameraRendererCS.RegisterSphereForRT(this);
    }

    private void OnDisable()
    {
        CameraRendererCS.UnregisterSphereForRT(this);
    }

    private void Start()
    {
        //if (gameObject.activeSelf)
        //    CameraRendererCS.RegisterSphereForRT(this);

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

        //bool metal = materialType > 0.5f;
        //albedo = metal ? Vector3.zero : new Vector3(materialColor.r, materialColor.g, materialColor.b);
        //specular = metal ? new Vector3(materialColor.r, materialColor.g, materialColor.b) : Vector3.one * 0.04f;
    }

    private void Update()
    {
        emissionOut = new Vector3(emission.r, emission.g, emission.b);
        emissionOut *= emissionForce;
    }

}
