using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateToFaceCamera : MonoBehaviour
{
    // Reference to the camera
    private Camera mainCamera;

    void Start()
    {
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    void Update()
    {
        if (mainCamera != null)
        {
            // Make the canvas always face the camera
            transform.LookAt(-mainCamera.transform.position);

            // Optionally, fix the rotation on the X and Z axis to prevent tilting
            Vector3 eulerRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, eulerRotation.y, 0f);
        }
    }
}
