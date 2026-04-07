using UnityEngine;
using System;
using System.Net;
using System.Text;
using System.Net.Sockets;


public class Server : MonoBehaviour
{
    public GameObject serverCube;
    private byte[] buffer = new byte[1024];
    private Socket serverSocket;
    private EndPoint remoteClient;

    private Quaternion receivedRotation;
    private bool isNewDataReceived = false;
    
    private Quaternion calibrationOffset = Quaternion.identity;
    private bool isCalibrated = false;
    
    [SerializeField] private int port = 1234; 


    public void StartServer()
    {
        try
        {
            IPAddress ip = IPAddress.Parse("192.168.137.1");
            IPEndPoint localEP = new IPEndPoint(ip, port);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(localEP);

            Debug.Log("Server started on " + localEP);

            IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
            remoteClient = (EndPoint)client;

            serverSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteClient, new AsyncCallback(ReceiveCallback), null);
        }
        catch (Exception e)
        {
            if (serverSocket == null)
                return;
    
            Debug.LogError("ReceiveCallback Exception: " + e);
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            int received = serverSocket.EndReceiveFrom(ar, ref remoteClient);
            string data = Encoding.UTF8.GetString(buffer, 0, received);
            Debug.Log("Received: " + data); // Debug incoming data

            
            data = data.Trim();

            // Split the received data into x, y, z
            string[] rotationData = data.Split(',');
            if (rotationData.Length == 4)
            {
                if (float.TryParse(rotationData[0], out float w) &&
                    float.TryParse(rotationData[1], out float x) &&
                    float.TryParse(rotationData[2], out float y) &&
                    float.TryParse(rotationData[3], out float z))
                {
                    receivedRotation = new Quaternion(y, -z, -x, w).normalized;  
                    isNewDataReceived = true;
                }
                else
                {
                    Debug.LogError("Error parsing received position data.");
                }
            }
            else
            {
                Debug.LogError("Invalid data format received: " + data);
            }

            // Continue listening for new data
            serverSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteClient, new AsyncCallback(ReceiveCallback), null);
        }
        catch (Exception e)
        {
            Debug.LogError("ReceiveCallback Exception: " + e);
        }
    }

    private void Start()
    {
        StartServer();
        //serverCube = GameObject.Find("ServerCube");
    }

    public void Calibrate()
    {
        calibrationOffset = Quaternion.Inverse(receivedRotation);
        isCalibrated = true;
    }

    
    
    private void Update()
    {
        if (isNewDataReceived && isCalibrated)
        {
            Quaternion fixedOffset = Quaternion.Euler(90, -90, -90);
            Quaternion corrected = calibrationOffset * receivedRotation  * fixedOffset;
            serverCube.transform.rotation = corrected;
            isNewDataReceived = false;
        }
    }
    
    public void StopServer()
    {
        if (serverSocket != null)
        {
            serverSocket.Close();
            serverSocket = null;
            Debug.Log("Server stopped.");
        }
    }

    
    private void OnDestroy()
    {
        StopServer();
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }
}
