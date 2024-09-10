using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections;


public class NetworkManager : MonoBehaviour
{
    private TcpListener server;
    private bool isRunning;

    public GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        StartServer();
        Debug.Log("NetworkManager is active and running.");

        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            Debug.Log("UnityMainThreadDispatcher is working!");
        });

        
    }

    void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            isRunning = true;
            Debug.Log("server started on port 5000");

            server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), server);

        }
        catch (Exception e)
        {
            Debug.LogError("Error starting server: " + e.Message);
        }
    }

    void OnClientConnected(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        TcpClient client = listener.EndAcceptTcpClient(ar);

        Debug.Log("Client connected");

        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Debug.Log("Received message: " + message);

        Debug.Log("Client connected"); // 클라이언트가 연결되었는지 확인

        if (UnityMainThreadDispatcher.Instance() == null)
        {
            Debug.LogError("UnityMainThreadDispatcher instance is null.");
        }
        else
        {
            Debug.Log("UnityMainThreadDispatcher instance found. Enqueuing message handling.");
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                Debug.Log("Dispatching message to HandleCommand: " + message);
                HandleCommand(message);
            });
        }


        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            Debug.Log("Dispatching message to HandleCommand: " + message);
            HandleCommand(message);
        });

        Debug.Log("Queued command execution");


        server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), server);

    }

    void HandleCommand(string message)
    {
        Debug.Log("Received command: " + message);  // 메시지 수신 확인
        if (int.TryParse(message, out int speakerIndex))
        {
            Debug.Log("Parsed speaker index: " + speakerIndex);  // 파싱된 인덱스 확인

            // 코루틴을 사용하여 메인 스레드에서 PlaySoundOnSpecificSpeaker 호출
            StartCoroutine(CallPlaySoundOnMainThread(speakerIndex));
        }
        else
        {
            Debug.LogError("Invalid command received: " + message);
        }
    }

    private IEnumerator CallPlaySoundOnMainThread(int speakerIndex)
    {
        Debug.Log("CallPlaySoundOnMainThread started: " + speakerIndex); // 코루틴 시작 확인
        yield return null;  // 다음 프레임으로 넘김, 이로써 메인 스레드에서 실행됨
        Debug.Log("CallPlaySoundOnMainThread executing: " + speakerIndex); // 코루틴이 계속 진행 중인지 확인
        gameManager.PlaySoundOnSpecificSpeaker(speakerIndex);
    }



    void OnApplicationQuit()
    {
        if (server != null)
        {
            server.Stop();
            isRunning = false;
        }
    }

}
