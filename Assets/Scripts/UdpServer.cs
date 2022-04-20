using UnityEngine;
using System;
using System.Text;
using TMPro;
using System.Collections.Generic;
using System.Collections.Concurrent;

#if UNITY_EDITOR
using System.Net;
using System.Net.Sockets;
using System.Threading;
#else
using Windows.Networking;
using Windows.Networking.Connectivity;
using System.Windows;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

/// <summary>
/// Simple demo code to recieve and display text data from a UDP port
/// </summary>
public class UdpServer : MonoBehaviour
{
    [SerializeField]
    private int serverPortNumber;
    public int ServerPortNumber => this.serverPortNumber;

    [SerializeField]
    private TextMeshPro logDisplay;
    public TextMeshPro LogDisplay => this.logDisplay;

    // Start is called before the first frame update
    void Start()
    {
        this.StartServer();
    }

    private void Update()
    {
        while(this.mainThreadActions.Count > 0)
        {
            Action action;
            if(this.mainThreadActions.TryDequeue(out action))
            {
                action();       
            }
        }
    }

    void OnDestroy()
    {
        this.StopServer();
    }

#if UNITY_EDITOR
    private UdpClient udpClient;

    private void StartServer()
    {
        if(!Application.isPlaying)
        {
            this.Log("Application is not playing.  Not creating server");
            return;
        }

        if(this.udpClient != null)
        {
            this.Log("server already started");
            return;
        }

        Thread serverThread = new Thread(new ThreadStart(() =>
        {
        try
        {
            this.udpClient = new UdpClient(this.ServerPortNumber);
            this.Log($"my IPv4: {this.GetIP4Address()}");
            this.Log($"receiving on port {this.ServerPortNumber}");
        }
        catch (Exception ex)
        {
            this.Log($"exception opening UDP port {this.ServerPortNumber} : {ex}");
            return;
        }

        while (true)
        {
            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref ipEndPoint);
                this.RunOnMainThread(() => { this.OnDataReceived(data); });
                }
                catch (Exception ex)
                {
                    this.Log($"exception receiving data: {ex}");
                    return;
                }
            }
        }));
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    private void StopServer()
    {
        if (this.udpClient != null)
        {
            this.udpClient.Dispose(); // this will cause the udpClient.Receive to throw.  That will exit the thread proc
            this.udpClient = null;
        }
    }

    private string GetIP4Address()
    {
        foreach (IPAddress ipAddress in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                return(ipAddress.ToString());
            }
        }
        return String.Empty;
    }

#else

    private DatagramSocket serverSocket = null;

    private async void StartServer()
    {
        if(this.serverSocket != null)
        {
            return;
        }

        try
        {
            this.serverSocket = new DatagramSocket();
            this.serverSocket.MessageReceived += UdpMessageReceived;
            await this.serverSocket.BindServiceNameAsync(ServerPortNumber.ToString());
            this.Log($"my IPv4: {this.GetIP4Address()}");
            this.Log($"receiving on port {this.ServerPortNumber}");
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            this.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
    }

    private void UdpMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        try
        {
            using (DataReader dataReader = args.GetDataReader())
            {
                uint byteCount = dataReader.UnconsumedBufferLength;
                byte[] buffer = new byte[byteCount];
                dataReader.ReadBytes(buffer);
                this.RunOnMainThread(() => { this.OnDataReceived(buffer); });
            }

            // Send a response back
            //using (Stream outputStream = (await sender.GetOutputStreamAsync(args.RemoteAddress, args.RemotePort)).AsStreamForWrite())
            //{
            //    using (var streamWriter = new StreamWriter(outputStream))
            //    {
            //        await streamWriter.WriteLineAsync("Thanks!");
            //        await streamWriter.FlushAsync();
            //    }
            //}
        }
        catch (Exception ex)
        {
            // You need to do more work here - like re-start the server
            this.Log($"    ServerDatagramSocket_MessageReceived: Exception {ex}");
        }
    }

    private void StopServer()
    {
        if(this.serverSocket != null)
        {
            this.serverSocket.Dispose();
            this.serverSocket = null;
        }
    }

    private string GetIP4Address()
    {
        foreach (HostName localHostName in NetworkInformation.GetHostNames())
        {
            if (localHostName.IPInformation != null)
            {
                if (localHostName.Type == HostNameType.Ipv4)
                {
                    return localHostName.ToString();
                }
            }
        }
        return string.Empty;
    }

#endif

    private void OnDataReceived(byte[] data)
    {
        string text = Encoding.UTF8.GetString(data).Trim();
        this.Log($"received text: \"{text}\"");
    }

    private void Log(string message, [System.Runtime.CompilerServices.CallerMemberName] string callerMemberName = "")
    {
        string fullMessage = $"UdpServer::{callerMemberName} - {message}";
        this.RunOnMainThread(() => { this.MainThreadLog(fullMessage); });
    }

    // Helper to get received stuff back on to the main thread.  Not really needed for UDP version of the code
    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private void RunOnMainThread(Action action)
    {
        mainThreadActions.Enqueue(action);
    }

    // Helpers to log stuff to the Unity player log as well as the display
    private void MainThreadLog(string message)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, message);

        Log2TextMesh(message);
    }

    Queue<string> logQueue = new Queue<string>();
    private void Log2TextMesh(string message)
    {
        while (logQueue.Count > 6) logQueue.Dequeue();

        logQueue.Enqueue(message);

        StringBuilder sb = new StringBuilder();
        foreach (var item in logQueue)
        {
            sb.Append(item.ToString());
            sb.Append("\r\n");
        }

        if(this.LogDisplay != null)
        {
            this.LogDisplay.text = sb.ToString();
        }
    }
}
