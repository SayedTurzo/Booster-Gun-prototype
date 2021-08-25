using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    Vector3 target;

    public ParticleSystem explodeParticle;

     Rigidbody rb;
    GameObject enemy;
    bool flag = false;

    public void SetDestination(Vector3 target)
    {
        this.target = target;
    }
    void Start()
    {
        //StartCoroutine(MakeExplodeWithoutReason());
        rb = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if(flag==false)
        {
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 5);
        }
        

        if (Input.GetKeyDown("g"))
        {
            StartCoroutine(MakeExplode());
        }
        if(flag==true)
        {
            rb.AddForce(5, 10, 0);
            enemy.GetComponent<Rigidbody>().transform.position = Vector3.Lerp(transform.position, gameObject.transform.position, Time.deltaTime * 5);

        }
    }
    IEnumerator MakeExplode()
    {    
        yield return new WaitForSeconds(.1f);
        explodeParticle.Play();
        flag = true;
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Enemy")
        {
            Debug.Log("Enemy colided");
            StartCoroutine(MakeExplode());
            enemy = col.gameObject;
            
        }
    }
}
