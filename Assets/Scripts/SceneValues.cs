using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneValues : MonoBehaviour
{
    public Texture skybox;
    [Range(0, 1)]
    public float illRatio = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
    }
}
