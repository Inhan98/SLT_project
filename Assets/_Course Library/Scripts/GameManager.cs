using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR;
using System.IO;
using System.Text;
using UnityEngine.UI;
//using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public Transform speakersParent; // 스피커들이 포함된 빈 게임 오브젝트
    private List<Speaker> speakers = new List<Speaker>();
    private List<Speaker> nonSpeakerObjects = new List<Speaker>();
    private Speaker currentSpeaker;
    private Speaker specificSpeaker;
    private string filePath;

    //private bool isLookingAtCenterSpeaker = false;
    public Transform centerSpeakerTransform;
    private Renderer centerSpeakerRenderer;
    public Color initialColor = Color.white;
    public Color targetColor = Color.blue;
    public Material defaultMaterial;

    private int trials = 0;

    private List<string> HRTFlist = new List<string> { "Personalized", "Generic", "Unrelated" };

    private string HRTF_type;

    private const int totalTrials = 105;
    private bool isClickable = true;

    private List<int> RandomNoiseList = new List<int>();
    private List<int> RandomSpeakersList = new List<int>();

    public Text trialsText;

    public GameObject vrUICanvas;

    void Start()
    {
        InitializeSpeakers();
        SetupLogFile();
        //StartCoroutine(DelayedResetGame());

        centerSpeakerRenderer = centerSpeakerTransform.GetComponent<Renderer>();
        if(centerSpeakerRenderer == null)
        {
            Debug.LogError("Center Speaker does not have a Renderer component.");
        }

        if (vrUICanvas != null)
        {
            vrUICanvas.transform.SetParent(Camera.main.transform);
            vrUICanvas.transform.localPosition = new Vector3(1.45f, 0.85f, -2.0f);

            vrUICanvas.transform.localRotation = Quaternion.identity;
            //vrUICanvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        }

        StartCoroutine(StartTrials());
        UpdateTrialsText();

    }


    private void SetupLogFile()
    {
        string directoryPath = "C:/Users/inhan/Desktop/VR/SpeakerLogs";
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
                speakers.Add(speaker);
                Debug.Log("Register " + speaker.gameObject.name);
            }
            else
            {
                nonSpeakerObjects.Add(speaker);
                Debug.Log("Don't register " + child.gameObject.name);
                //Debug.LogError($"No Speaker component found on {child.name}");
            }
        }
    }

    private IEnumerator StartTrials()
    {
        ShuffleNoise();
        ShuffleSpeakers();
        while (trials < totalTrials)
        {
            //yield return StartCoroutine(WaitForGazeAtCenterSpeaker());
            isClickable = true;

            PlayRandomSpeaker(RandomNoiseList[trials], RandomSpeakersList[trials]);
            //PlayRandomSpeaker();
            yield return new WaitUntil(() => currentSpeaker == null);
            yield return new WaitForSeconds(2.0f);
            ResetSpeakerColor();
            trials++;
            UpdateTrialsText();
        }
        Debug.Log("All trials completed.");
    }

    private void UpdateTrialsText()
    {
        if (trialsText != null)
        {
            trialsText.text = "Trials: " + trials + "/" + totalTrials;
        }
    }

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

    public void PlayRandomSpeaker()
    {
        int randomIndex = UnityEngine.Random.Range(0, speakers.Count);
        currentSpeaker = speakers[randomIndex];

        currentSpeaker.PlaySound();
        Debug.Log("Playing Speaker: "+currentSpeaker.gameObject.name);

        //LogSelection(trials.ToString(),currentSpeaker.gameObject.name, currentSpeaker.gameObject.name, "X", "Played");
    }

    public void PlayRandomSpeaker(int cnt)
    {
        int randomIndex = UnityEngine.Random.Range(0, speakers.Count);
        currentSpeaker = speakers[randomIndex];
        HRTF_type = HRTFlist[cnt];

        currentSpeaker.PlayWhiteNoise(cnt);
        Debug.Log("Playing Speaker: "+currentSpeaker.gameObject.name);

        LogSelection(trials.ToString(),currentSpeaker.gameObject.name, currentSpeaker.gameObject.name, HRTF_type,"Played");
    }


    public void PlayRandomSpeaker(int cnt, int randomIndex)
    {
        currentSpeaker = speakers[randomIndex];
        HRTF_type = HRTFlist[cnt];

        currentSpeaker.PlayWhiteNoise(cnt);
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
