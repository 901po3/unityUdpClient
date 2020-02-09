using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCS : MonoBehaviour
{
    public NetworkMan netMan;
    public string id;

    private void Start()
    {
        //netMan = gameObject.GetComponent<NetworkMan>();
        InvokeRepeating("NetworkTransform", 1, 0.03f);
    }

    private void Update()
    {
        if (id != netMan.ClientID) return;

        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(-Vector3.forward * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * 180);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(-Vector3.up * Time.deltaTime * 180);
        }
    }

    public void NetworkTransform()
    {
        if (netMan != null)
        {
            netMan.SendPlayerInfo(transform);
        }
    }
}
