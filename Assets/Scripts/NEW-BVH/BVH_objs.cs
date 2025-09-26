using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVH_objs : MonoBehaviour
{
    struct BoundingVH
    {
        int indexObject;
        Vector3 centerBBobj;
        Vector3 _minBBobj;
        Vector3 _maxBBobj;

        int indexStartTriangle;
        int numberTriangles;
    }
    
    
    List<BoundingVH> boundings = new List<BoundingVH>();

    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
