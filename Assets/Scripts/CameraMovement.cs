using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    private Camera cam;
    public float camSpeed = 100.0f;
    float camSens = 0.25f;
    private Vector3 lastMouse = new Vector3(255, 255, 255);
    private float totalRun = 1.0f;


    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        lastMouse = Input.mousePosition - lastMouse;
        lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
        lastMouse = new Vector3(cam.transform.eulerAngles.x + lastMouse.x, cam.transform.eulerAngles.y + lastMouse.y, 0);
        cam.transform.eulerAngles = lastMouse;
        lastMouse = Input.mousePosition;


        Vector3 p = GetBaseInput();
        totalRun = Mathf.Clamp(totalRun * 0.5f, 1.0f, 1000.0f);
        p = p * camSpeed;
        p = p * Time.deltaTime;
        Vector3 newPosition = cam.transform.position;
        cam.transform.Translate(p);
    }

    private Vector3 GetBaseInput()
    {
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }
}
