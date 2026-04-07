using UnityEngine;

//Lec04
using System;
using System.Net;
using System.Text;
using System.Net.Sockets;


public class Client : MonoBehaviour
{
    public GameObject myCube;
    private static byte[] buffer = new byte[1024];
    private static IPEndPoint remoteEP;
    private static Socket client;
    
    public static void StartClient()
    {
        try
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            remoteEP = new IPEndPoint(ip, 1111);

            client = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            client.Blocking = false;

        } catch (Exception e)
        {
            Debug.Log("Exception: " + e.ToString());
        }
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myCube = GameObject.Find("Cube");
        StartClient();
    }

    // Update is called once per frame
    void Update()
    {
        buffer = Encoding.ASCII.GetBytes(myCube.transform.position.x.ToString());
        client.SendTo(buffer, remoteEP);
    }
}
