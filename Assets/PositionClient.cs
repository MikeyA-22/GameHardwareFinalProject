using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using TMPro;

public class PositionClient : MonoBehaviour
{
    [SerializeField] private GameObject MyCube;
    [SerializeField] private GameObject RemoteCube;
    public TMP_InputField ipInput;

    private Socket clientSocket;
    private IPEndPoint remoteEP;
    private IPEndPoint localEP;

    private bool isRunning = false;
    private Thread receiveThread;

    private Vector3 newPosition;
    private bool positionUpdated = false;
    private readonly object positionLock = new object();

    public void StartConnection()
    {
        string otherClientIP = ipInput.text.Trim();
        if (string.IsNullOrEmpty(otherClientIP))
        {
            Debug.LogError("Other client IP is empty.");
            return;
        }

        try
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientSocket.Blocking = false;

            // Bind to a local port
            localEP = new IPEndPoint(IPAddress.Any, 0);
            clientSocket.Bind(localEP);

            // Set the remote endpoint (server address)
            remoteEP = new IPEndPoint(IPAddress.Parse(otherClientIP), 8889);

            isRunning = true;

            Debug.Log("Client started, sending first message...");
            SendTestMessage(); // Send a test message to verify the connection
            
            InvokeRepeating(nameof(SendPosition), 0.5f, 0.1f);

            // Start a receiving thread
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection error: {e}");
        }
    }

    private void SendTestMessage()
    {
        try
        {
            byte[] testData = System.Text.Encoding.ASCII.GetBytes("hello");
            clientSocket.SendTo(testData, remoteEP);
            Debug.Log("Sent test message.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Send error: {e}");
        }
    }

   private void SendPosition()
{
    try
    {
        if (clientSocket == null) return;

        Vector3 pos = MyCube.transform.position;
        float[] posArray = { pos.x, pos.y, pos.z };
        byte[] data = new byte[12];
        Buffer.BlockCopy(posArray, 0, data, 0, data.Length);

        clientSocket.SendTo(data, remoteEP);
        Debug.Log($"Sent Position: {pos}");
    }
    catch (Exception e)
    {
        Debug.LogError($"Send error: {e}");
    }
}

private void ReceiveLoop()
{
    EndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);
    byte[] receivedData = new byte[4096]; // Larger buffer for larger datagrams

    while (isRunning)
    {
        try
        {
            int receivedBytes = clientSocket.ReceiveFrom(receivedData, ref senderEP);

            if (receivedBytes > 0)
            {
                // Process the received data
                int totalFloats = receivedBytes / sizeof(float); // Number of floats in the received data
                if (totalFloats == 3) // We expect 3 floats (x, y, z)
                {
                    float[] posArray = new float[3];
                    Buffer.BlockCopy(receivedData, 0, posArray, 0, receivedBytes);

                    Vector3 receivedPosition = new Vector3(posArray[0], posArray[1], posArray[2]);

                    lock (positionLock) // Ensure thread safety when updating the position
                    {
                        newPosition = receivedPosition;
                        positionUpdated = true;
                    }

                    Debug.Log($"Received Position: {receivedPosition}");
                }
                else
                {
                    Debug.LogWarning($"Unexpected data size: {receivedBytes} bytes (Expected 12 bytes for 3 floats)");
                }
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
        {
            // No data received, continue loop
        }
        catch (Exception e)
        {
            Debug.LogError($"Receive error: {e}");
        }

        Thread.Sleep(10); // Prevents CPU overload
    }
}




    private void Update()
    {
        if (positionUpdated)
        {
            lock (positionLock)
            {
                RemoteCube.transform.position = newPosition;
                positionUpdated = false;
            }
        }
    }

    public void StopConnection()
    {
        if (clientSocket != null)
        {
            CancelInvoke(nameof(SendPosition));
            isRunning = false;
            receiveThread?.Abort();
            clientSocket.Close();
            clientSocket = null;
            Debug.Log("Disconnected from server.");
        }
    }

    void OnApplicationQuit()
    {
        StopConnection();
    }
}
