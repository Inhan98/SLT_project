using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR;
using System.IO;
using System.Text;
using UnityEngine.UI;
using System.Net.Sockets;

using System;
//using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public Transform speakersParent; // 스피커들이 포함된 빈 게임 오브젝트
    private List<Speaker> speakers = new List<Speaker>(); // 스피커 오브젝트(0도, 30도, 60도, 90도, 120도, 150도, 180도)
    private List<Speaker> nonSpeakerObjects = new List<Speaker>(); // 가짜 스피커(위의 스피커를 제외한 나머지 스피커 10,20,40...)
    private Speaker currentSpeaker; // 사운드가 재생된 스피커
    private Speaker specificSpeaker; // 관리자가 지정한 스피커(외부 통신을 통해 스피커를 지정하면 해당 스피커가 울림) => 현재 프로그램에서는 무시해도 됨.
    private string filePath; // 파일 경로

    private TcpClient client; // 외부 통신을 위한 tcpclient
    public NetworkStream stream; // 외부 통신을 위한 stream 


    //private bool isLookingAtCenterSpeaker = false;
    public Transform centerSpeakerTransform; // 장면 스피커(0도)
    private Renderer centerSpeakerRenderer; // 정면 스피커 렌더러

    public Color initialColor = Color.white;  // 정면을 보고 있을 때, 정면 스피커의 initial color
    public Color targetColor = Color.blue; // 정면을 바라보고 있을 때, 정면 스피커의 target color
    public Material defaultMaterial;

    private int trials = 0;

    private List<string> HRTFlist = new List<string> { "Personalized", "Generic", "Unrelated" }; // 개인화된 HRTF: personalized, General HRTF: Generic, 신경망으로 구현된 HRTF: Unrelated

    private string HRTF_type;

    private const int totalTrials = 105; // 전체 테스트 수
    private bool isClickable = true; 

    private List<int> RandomNoiseList = new List<int>(); // 105번의 테스트 중 HRTF 순서 (0: , 1: ,2:)
    private List<int> RandomSpeakersList = new List<int>(); // 105번의 테스트 중 재생되는 스피커 순서 (0,1,2,3,4,5,6)

    private List<int> RandomSoundList = new List<int>(); // 105번의 테스트 중 사운드 순서 (0~20)

    public Text trialsText;  // 테스트 진행 상황을 표시할 UI 텍스트

    public GameObject vrUICanvas; // VR UI 캔버스

    void Start()
    {
        // 초기 설정 메서드 호출
        InitializeSpeakers();
        SetupLogFile();
        ConnectToMatlab();
        //ConnectToMatlab();
        //StartCoroutine(DelayedResetGame());

        InitializeSpeakersStream();

        // 정면 스피커 렌더러 설정

        centerSpeakerRenderer = centerSpeakerTransform.GetComponent<Renderer>();
        if(centerSpeakerRenderer == null)
        {
            Debug.LogError("Center Speaker does not have a Renderer component.");
        }

        if (vrUICanvas != null)
        {
            // vrUICanvas.transform.SetParent(Camera.main.transform);
            // vrUICanvas.transform.localPosition = new Vector3(1.45f, 0.85f, -2.0f);

            // vrUICanvas.transform.localRotation = Quaternion.identity;
            // //vrUICanvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        }
        
        StartCoroutine(StartTrials()); // 테스트 시작 코루틴 호출
        UpdateTrialsText(); // 테스트 상태 업데이트

    }
    // 스피커 스트림 초기화

    void InitializeSpeakersStream()
    {
        foreach (var speaker in speakers)
        {
            speaker.SetStream(stream);
        }
    }

     // MATLAB 서버와 연결
    void ConnectToMatlab()
    {
        try{
            client = new TcpClient("127.0.0.1", 4504);
            stream = client.GetStream();
            Debug.Log("Connected to MATLAB server.");
        }
        catch (SocketException e)
        {
            Debug.LogError("Socket error: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("General error: " + e.Message);
        }
    }


    // 애플리케이션 종료 시 연결 해제
    void OnApplicationQuit()
    {
        if (stream != null)
        {
            stream.Close();
        }
        if (client != null)
        {
            client.Close();
        }
    }

    // 로그 파일 설정
    private void SetupLogFile()
    {
        string directoryPath = "C:/Users/inhan/Desktop/VR/SpeakerLogs"; // Log File 위치. 재설정 필수수
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        filePath = Path.Combine(directoryPath, "SpeakerLog_"+ DateTime.Now.ToString("yyyyMMdd_HHmmss")+".csv");
        if (!File.Exists(filePath))
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("Time, Trials, Selected Speaker, Correct Speaker, HRTF, Result");
            }
        }

    }

    // 스피커 초기화
    void InitializeSpeakers()
    {
        if (speakersParent == null)
        {
            Debug.LogError("Speakers Parent is not assigned in the Inspector");
            return;
        }

        foreach (Transform child in speakersParent)
        {
            Speaker speaker = child.GetComponent<Speaker>();
            if (speaker != null && speaker.isFake == false)
            {
                speakers.Add(speaker); // 사운드가 재생되는 진짜 스피커
                Debug.Log("Register " + speaker.gameObject.name);
            }
            else
            {
                nonSpeakerObjects.Add(speaker); // 사운드가 재생되지 않는 가짜짜 스피커
                Debug.Log("Don't register " + child.gameObject.name);
                //Debug.LogError($"No Speaker component found on {child.name}");
            }
        }
    }

    // 테스트 시작 코루틴
    private IEnumerator StartTrials()
    {
        ShuffleSound(); //랜덤 순서 설정
        while (trials < totalTrials)
        {
            yield return StartCoroutine(WaitForGazeAtCenterSpeaker()); // 정면 스피커 바라보기 
            isClickable = true;

            PlayRandomSpeaker(RandomNoiseList[trials], RandomSpeakersList[trials]); // 랜덤 스피커 재생
            //PlayRandomSpeaker();
            yield return new WaitUntil(() => currentSpeaker == null); // 재생 완료 대기
            yield return new WaitForSeconds(2.0f); // 2초 대기
            ResetSpeakerColor();  // 스피커 색상 초기화
            trials++; 
            UpdateTrialsText(); // 테스트 상태 업데이트
        }
        Debug.Log("All trials completed.");
    }

    // 테스트 진행 상황 업데이트
    private void UpdateTrialsText()
    {
        if (trialsText != null)
        {
            trialsText.text = "Trials: " + trials + "/" + totalTrials;
        }
    }


    // 정면 스피커 바라보기 대기
    private IEnumerator WaitForGazeAtCenterSpeaker()
    {
        Debug.Log("Look at the center speaker for 3 seconds to start.");

        float lookTime = 0.0f;
        while( lookTime < 3.0f)
        {
            Quaternion headsetRotation = InputTracking.GetLocalRotation(XRNode.Head);
            Vector3 forward = headsetRotation * Vector3.forward;

            Vector3 toCenterSpeaker = (centerSpeakerTransform.position - Camera.main.transform.position).normalized;

            Vector3 headEulerAngles = headsetRotation.eulerAngles; 
            Debug.Log($"Head angle (Pitch: {headEulerAngles.x}, Yaw: {headEulerAngles.y}, Roll: {headEulerAngles.z})");

            if (Vector3.Dot(forward, toCenterSpeaker) > 0.95f)
            {
                lookTime += Time.deltaTime;


                float colorLerpValue = lookTime / 3.0f;
                Color currentColor = Color.Lerp(initialColor, targetColor, colorLerpValue);
                centerSpeakerRenderer.material.color = currentColor;
            }
            else
            {
                lookTime = 0.0f;
                centerSpeakerRenderer.material.color = initialColor;
            }

            yield return null;

        }

        Debug.Log("Look confirmed. Starting random speaker sound.");
        centerSpeakerRenderer.material = defaultMaterial;
    }

    private IEnumerator DelayedResetGame()
    {
        yield return null;
        ResetGame();
    }

    public void ResetSpeakerColor()
    {
        foreach (var speaker in speakers)
        {
            Debug.Log("Register Speaker "+speaker.gameObject.name);
            speaker.ResetMaterial();
        }
        foreach (var speaker in nonSpeakerObjects)
        {
            Debug.Log("Register Speaker "+speaker.gameObject.name);
            speaker.ResetMaterial();
        }
    }

    public void ShuffleNoise()
    {
        for (int i = 0; i <35; i++)
        {
            RandomNoiseList.Add(0);
            RandomNoiseList.Add(1);
            RandomNoiseList.Add(2);
        }
        print("Total count of Noises:  "+ RandomNoiseList.Count);
        Debug.Log("Before shuffle(Noise): " + string.Join(", ", RandomNoiseList));
        ShuffleList(RandomNoiseList);
        Debug.Log("After shuffle(Noise): " + string.Join(", ", RandomNoiseList));
    }

    public void ShuffleSpeakers()
    {
        int SpeakerNums = GetSpeakerCount();
        for (int i = 0; i <15; i++)
        {
            for (int j=0; j < SpeakerNums; j++)
                RandomSpeakersList.Add(j);
        }
        print("Total count of Speakers:  "+ RandomSpeakersList.Count);
        Debug.Log("Before shuffle(Speakers): " + string.Join(", ", RandomSpeakersList));
        ShuffleList(RandomSpeakersList);
        Debug.Log("After shuffle(Speakers): " + string.Join(", ", RandomSpeakersList));


    }


    // 사운드 셔플
    public void ShuffleSound()
    {
        RandomSoundList.Clear();
        RandomSpeakersList.Clear();
        RandomNoiseList.Clear();

        int soundnum = 21;
        for (int i = 0 ; i < soundnum; i++)
        {
            for (int j=0 ; j< 5; j++){
                RandomSoundList.Add(i);
            }
        }
        Debug.Log("Before shuffle: " + string.Join(", ", RandomSoundList));
        ShuffleList(RandomSoundList);
        Debug.Log("After shuffle: " + string.Join(", ", RandomSoundList));

        foreach(int value in RandomSoundList)
        {
            int SpeakerNum = value%7;
            int NoiseNum = value/7;

            RandomSpeakersList.Add(SpeakerNum);
            RandomNoiseList.Add(NoiseNum);

        }
        Debug.Log("After shuffle(Noise): " + string.Join(", ", RandomNoiseList));
        Debug.Log("After shuffle(Speakers): " + string.Join(", ", RandomSpeakersList));

        var pairCounts = new Dictionary<(int, int), int>();
        for (int i = 0; i < RandomSpeakersList.Count; i++)
        {
            var pair = (RandomNoiseList[i], RandomSpeakersList[i]);
            if (pairCounts.ContainsKey(pair))
                pairCounts[pair]++;
            else
                pairCounts[pair] = 1;
        }

        // 페어 개수를 출력
        for (int noise = 0; noise <= 2; noise++)
        {
            for (int speaker = 0; speaker <= 6; speaker++)
            {
                var pair = (noise, speaker);
                if (pairCounts.ContainsKey(pair))
                {
                    Debug.Log($"Pair (Noise: {noise}, Speaker: {speaker}): {pairCounts[pair]} times");
                }
                else
                {
                    Debug.Log($"Pair (Noise: {noise}, Speaker: {speaker}): 0 times");
                }
            }
        }
    }


    public void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {

            int randomIndex = UnityEngine.Random.Range(0, list.Count);

            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void ResetGame()
    {
        if (speakers.Count == 0)
        {
            Debug.LogError("No speakers found. Make sure the speakers are assigned and have the Speaker component.");
            return;
        }

        ResetSpeakerColor();

        Invoke("PlayRandomSpeaker", 2.0f); // 2초 후에 랜덤 스피커 재생
    }

    //랜덤 스피커 재생
    public void PlayRandomSpeaker()
    {
        int randomIndex = UnityEngine.Random.Range(0, speakers.Count);
        currentSpeaker = speakers[randomIndex];

        currentSpeaker.PlaySound();
        Debug.Log("Playing Speaker: "+currentSpeaker.gameObject.name);

        //LogSelection(trials.ToString(),currentSpeaker.gameObject.name, currentSpeaker.gameObject.name, "X", "Played");
    }
    //랜덤 스피커 재생
    public void PlayRandomSpeaker(int cnt)
    {
        int randomIndex = UnityEngine.Random.Range(0, speakers.Count);
        currentSpeaker = speakers[randomIndex];
        HRTF_type = HRTFlist[cnt];

        //currentSpeaker.PlayWhiteNoise(cnt);
        currentSpeaker.SendSoundToMatlab(cnt);
        Debug.Log("Playing Speaker: "+currentSpeaker.gameObject.name);

        LogSelection(trials.ToString(),currentSpeaker.gameObject.name, currentSpeaker.gameObject.name, HRTF_type,"Played");
    }

    // 매트랩으로 사운드 송신
    public void PlayRandomSpeaker(int cnt, int randomIndex)
    {
        currentSpeaker = speakers[randomIndex];
        HRTF_type = HRTFlist[cnt];

        currentSpeaker.SendSoundToMatlab(cnt);
        //currentSpeaker.PlayWhiteNoise(cnt);
        Debug.Log("Playing Speaker: "+currentSpeaker.gameObject.name);

        //LogSelection(trials.ToString(),currentSpeaker.gameObject.name, currentSpeaker.gameObject.name, HRTF_type,"Played");
    }



    public void PlaySoundOnSpecificSpeaker(int speakerIndex)
    {
        try
        {
            Debug.Log("PlaySoundOnSpecificSpeaker called with index: " + speakerIndex);

            if (speakerIndex >= 0 && speakerIndex < speakers.Count)
            {
                specificSpeaker = speakers[speakerIndex];
                if (specificSpeaker == null)
                {
                    Debug.LogError("specificSpeaker is null.");
                    return;
                }

                Debug.Log("Specific Speaker: " + specificSpeaker.gameObject.name);

                specificSpeaker.PlaySound();  // 소리 재생
                Debug.Log("Playing sound on specific speaker: " + specificSpeaker.gameObject.name);
                currentSpeaker = null;
            }
            else
            {
                Debug.LogError("Invalid speaker index: " + speakerIndex);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("An error occurred: " + ex.Message);
        }
    }


    // 스피커 체크
    public void CheckSpeaker(Speaker selectedSpeaker)
    {
        if(!isClickable) return;

        isClickable = false;

        Quaternion headsetRotation = InputTracking.GetLocalRotation(XRNode.Head);
        Vector3 eulerAngles = headsetRotation.eulerAngles;
        float yaw = eulerAngles.x;
        string result;

        if (currentSpeaker)
        {
            if (selectedSpeaker == currentSpeaker)
            {
                //selectedSpeaker.SetMaterial(selectedSpeaker.correctMaterial);
                //selectedSpeaker.PlayCorrectSound();
                selectedSpeaker.SetMaterial(selectedSpeaker.clickMaterial);
                selectedSpeaker.PlayClickSound();
                result = "Correct";
                Debug.Log("Current Heading (Yaw): " + yaw);
                Debug.Log("Correct! You selected the right speaker.");
            }
            else
            {
                //selectedSpeaker.SetMaterial(selectedSpeaker.wrongMaterial);
                //selectedSpeaker.PlayWrongSound();
                selectedSpeaker.SetMaterial(selectedSpeaker.clickMaterial);
                selectedSpeaker.PlayClickSound();
                result = "Wrong";
                Debug.Log("Current Heading (Yaw): " + yaw);
                Debug.Log("Wrong! You selected the wrong speaker.");
            }
            LogSelection(trials.ToString(),selectedSpeaker.gameObject.name, currentSpeaker ? currentSpeaker.gameObject.name : "None", HRTF_type,result);
        }
        else 
        {
            if(selectedSpeaker == specificSpeaker)
            {
                selectedSpeaker.SetMaterial(selectedSpeaker.correctMaterial);
                selectedSpeaker.PlayCorrectSound();

                result = "Correct";
                Debug.Log("specific Heading (Yaw): " + yaw);
                Debug.Log("Correct! You selected the right speaker.");
            }
            else
            {
                selectedSpeaker.SetMaterial(selectedSpeaker.wrongMaterial);
                selectedSpeaker.PlayWrongSound();
                result = "Wrong";
                Debug.Log("specific Heading (Yaw): " + yaw);
                Debug.Log("Wrong! You selected the wrong speaker.");
            }
            LogSelection(trials.ToString(),selectedSpeaker.gameObject.name, specificSpeaker ? specificSpeaker.gameObject.name : "None", HRTF_type, result);

        }
        

        currentSpeaker = null;
    }


    private void LogSelection(string trials, string selectedSpeakerName, string correctSpeakerName, string HRTF_type,string result)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
        {
            writer.WriteLine($"{timestamp}, {trials}, {selectedSpeakerName}, {correctSpeakerName}, {HRTF_type},{result}");
        }

        Debug.Log($"Log saved: {timestamp}, Trials: {trials}, Selected: {selectedSpeakerName}, Correct: {correctSpeakerName}, HRTF: {HRTF_type},Result: {result}");

    }

    public int GetSpeakerCount()
    {
        return speakers.Count;
    }


    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
    }

}
