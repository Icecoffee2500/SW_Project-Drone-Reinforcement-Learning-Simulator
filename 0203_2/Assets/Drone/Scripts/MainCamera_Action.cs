using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera_Action : MonoBehaviour
{
    public Transform drone;
    public Transform goal;
    public GameObject mainC;
    public GameObject subC;

    private Camera mainCamera;
    private Camera subCamera;
    private Transform mainCameraTrans;
    private Transform subCameraTrans;
    private Vector3 headToGoal;
    private Vector3 goalY;
    private Vector3 droneY;

    public float dist = 5.0f;
    public float height = 2.0f;
    public float dampTrace = 20.0f;

    private void Start()
    {
        mainCamera = mainC.GetComponent<Camera>();
        subCamera = subC.GetComponent<Camera>();
        
        mainCameraTrans = mainC.GetComponent<Transform>();
        subCameraTrans = subC.GetComponent <Transform>();

        goalY = new Vector3(0, goal.position.y, 0);
        droneY = new Vector3(0, drone.position.y, 0);

        headToGoal = (goal.position - drone.position + droneY - goalY).normalized;

        mainCameraTrans.position = (drone.position - (headToGoal * dist) - (Vector3.up * height));
        subCameraTrans.position = (drone.position - (headToGoal * dist) + (Vector3.up * height));

        mainCameraTrans.LookAt(drone);
        subCameraTrans.LookAt(drone);
        subCameraOn();

    }

    private void mainCameraOn()
    {
        mainCamera.enabled = true;
        subCamera.enabled = false;
    }

    private void subCameraOn()
    {
        mainCamera.enabled = false;
        subCamera.enabled = true;
    }

    private void LateUpdate()
    {
        mainCameraTrans.position = Vector3.Lerp(mainCameraTrans.position, drone.position - (headToGoal * dist), Time.deltaTime * dampTrace);
        subCameraTrans.position = Vector3.Lerp(subCameraTrans.position, drone.position - (headToGoal * dist) + (Vector3.up * 5f), Time.deltaTime * dampTrace);

        if (Input.GetKey("1"))
            mainCameraOn();

        if (Input.GetKey("2"))
            subCameraOn();
    }
}