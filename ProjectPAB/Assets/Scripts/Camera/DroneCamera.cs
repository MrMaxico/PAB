using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneCamera : MonoBehaviour
{
    public Transform target;          // The NavMesh agent's transform
    public float smoothSpeed = 0.125f;
    public Vector3 offset;            // Offset from the target
    public float rayDistance = 10f;   // Raycast distance for obstacle detection
    public float heightOffset = 5f;   // How high the camera can go over obstacles
    public float lookDownAngle = 30f; // Angle to tilt the camera to avoid looking straight down

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.rotation.SetLookRotation(target.position);
    }
}