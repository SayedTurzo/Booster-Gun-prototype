using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public GameObject bombObject;
    public GameObject bombSpawnPoint;
    //GameObject enemy;
    //public GameObject cameraObj;

    //public Transform [] target;

    //int count=1;

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            RaycastHit hit;
            if (Physics.Raycast(camRay, out hit, 1000) && (Input.mousePosition.y > 232))
            {
                GameObject temp = Instantiate(bombObject, bombSpawnPoint.transform.position, Quaternion.identity);
                temp.GetComponent<Bomb>().SetDestination(hit.point);
                temp.GetComponent<Rigidbody>().AddForce(10,10,0);
                //count++;
            }
        }
        //transform.LookAt(target[count]);

    }
}
