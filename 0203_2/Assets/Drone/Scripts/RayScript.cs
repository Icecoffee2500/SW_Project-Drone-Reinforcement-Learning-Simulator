using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayScript : MonoBehaviour
{

    private RaycastHit hit;
    public float distance = 10;


    void Update()
    {
        if (Physics.Raycast(transform.position, -transform.up, out hit))
        {
            //Debug.Log("hit point : " + hit.point + ", distance : " + hit.distance + ", name : " + hit.collider.name);
            //Debug.DrawRay(transform.position, -transform.up * hit.distance, Color.red);
            distance = hit.distance;
        }
        else
        {
            //Debug.DrawRay(transform.position, -transform.up * 1000f, Color.red);
        }
    }
}