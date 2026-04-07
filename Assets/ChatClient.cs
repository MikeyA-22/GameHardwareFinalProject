using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class ChatClient : MonoBehaviour
{
    public TMP_InputField ipInput;
    public GameObject connectionPanel;
    public GameObject chatPanel;

    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] TMP_Text chatLog;
    [SerializeField] private Button sendButton;

    private Socket client;
    private string otherClientIP;
    private static byte[] buffer = new byte[1024];
    private static byte[] sendBuffer = new byte[1024];
    

    private Queue<string> messageQueue = new Queue<string>();
    private readonly object queueLock = new object();

    void Start()
    {
        chatPanel.SetActive(false);
        
        
        
    }

    public void SendMyMessage()
    {
        if (client == null || !client.Connected)
        {
            Debug.LogWarning("Client not found");
            return;
        };

        string message = chatInput.text.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            
            return;
        }

        sendBuffer = Encoding.UTF8.GetBytes(message);
        client.BeginSend(sendBuffer, 0, sendBuffer.Length, 0, new AsyncCallback(SendCallBack), client);

        chatInput.text = ""; 
    }

    public void ConnectToOtherClient()
    {
        otherClientIP = ipInput.text.Trim();
        if (string.IsNullOrEmpty(otherClientIP))
        {
            Console.Error.WriteLine("otherClient IP is empty.");
            return;
        }

        try
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            client.Connect(IPAddress.Parse(otherClientIP), 8888);
            Debug.Log("Connected!!");
            
            client.BeginReceive(buffer, 0, buffer.Length,0, new AsyncCallback(ReceiveCallBack), client);
            
            
            //client.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallBack), client);
            
            
            

            connectionPanel.SetActive(false);
            chatPanel.SetActive(true); // Show chat UI
        }
        catch (Exception e)
        {
            Debug.LogError("Connection failed: " + e.Message);
        }
    }

    void SendCallBack(IAsyncResult ar)
    {
        try
        {
            Socket otherClient = (Socket)ar.AsyncState;
            
            int bytesSent = otherClient.EndSend(ar);
            Debug.Log($"Sent {bytesSent} bytes successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError("Send failed: " + e.Message);
        }
    }

    void ReceiveCallBack(IAsyncResult ar)
    {
        try
        {
            Socket otherClient = (Socket)ar.AsyncState;
            int msgRec = otherClient.EndReceive(ar);

            if (msgRec <= 0) return;

            string message = Encoding.UTF8.GetString(buffer, 0, msgRec);
            Debug.Log("Received: " + message);

            if (message.ToLower().Contains("Client is quitting now."))
            {
                chatPanel.SetActive(false);
                connectionPanel.SetActive(true);
            }
            
            // Safely add message to queue
            lock (queueLock)
            {
                messageQueue.Enqueue(message);
            }

            

            
            otherClient.BeginReceive(buffer, 0, buffer.Length, 0, new AsyncCallback(ReceiveCallBack), otherClient);
        }
        catch (Exception e)
        {
            Debug.LogError("Receive failed: " + e.Message);
        }
    }

    //Once per frame, the same gist
    void Update()
    {
        
        lock (queueLock)
        {
            while (messageQueue.Count > 0)
            {
                chatLog.text += "\n" + messageQueue.Dequeue();
            }
        }
        
        
    }

    void OnApplicationQuit()
    {
        if (client != null)
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            client = null;
        }

        
        
    }
}
