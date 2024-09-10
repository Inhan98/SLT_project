using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR;
using System.IO;
using System.Text;

public class GameManager : MonoBehaviour
{
    public Transform speakersParent; // 스피커들이 포함된 빈 게임 오브젝트
    private List<Speaker> speakers = new List<Speaker>();
    private Speaker currentSpeaker;
    private Speaker specificSpeaker;
    private string filePath;

    void Start()
    {
        InitializeSpeakers();
        StartCoroutine(DelayedResetGame());


        string directoryPath = "C:/Users/inhan/Desktop/VR/SpeakerLogs";
        if(!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        filePath = Path.Combine(directoryPath, "SpeakerLog_"+ DateTime.Now.ToString("yyyyMMdd_HHmmss")+".csv");

        if (!File.Exists(filePath))
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("Time, Selected Speaker, Correct Speaker, Result");
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
            if (speaker != null)
            {
                
                speakers.Add(speaker);
            }
            else
            {
                Debug.LogError($"No Speaker component found on {child.name}");
            }
        }
    }

    private IEnumerator DelayedResetGame()
    {
        yield return null;
        ResetGame();
    }

    public void ResetGame()
    {
        if (speakers.Count == 0)
        {
            Debug.LogError("No speakers found. Make sure the speakers are assigned and have the Speaker component.");
            return;
        }

        foreach (var speaker in speakers)
        {
            Debug.Log("Register Speaker "+speaker.gameObject.name);
            speaker.ResetMaterial();
        }
        Invoke("PlayRandomSpeaker", 2.0f); // 2초 후에 랜덤 스피커 재생
    }

    public void PlayRandomSpeaker()
    {
        int randomIndex = UnityEngine.Random.Range(0, speakers.Count);
        currentSpeaker = speakers[randomIndex];

        currentSpeaker.PlaySound();
        Debug.Log("Playing Speaker: "+currentSpeaker.gameObject.name);
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
        Quaternion headsetRotation = InputTracking.GetLocalRotation(XRNode.Head);
        Vector3 eulerAngles = headsetRotation.eulerAngles;
        float yaw = eulerAngles.x;
        string result;

        if (currentSpeaker)
        {
            if (selectedSpeaker == currentSpeaker)
            {
                selectedSpeaker.SetMaterial(selectedSpeaker.correctMaterial);
                selectedSpeaker.PlayCorrectSound();

                result = "Correct";
                Debug.Log("Current Heading (Yaw): " + yaw);
                Debug.Log("Correct! You selected the right speaker.");
            }
            else
            {
                selectedSpeaker.SetMaterial(selectedSpeaker.wrongMaterial);
                selectedSpeaker.PlayWrongSound();
                result = "Wrong";
                Debug.Log("Current Heading (Yaw): " + yaw);
                Debug.Log("Wrong! You selected the wrong speaker.");
            }
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

        }
        LogSelection(selectedSpeaker.gameObject.name, specificSpeaker ? specificSpeaker.gameObject.name : "None", result);
    }


    private void LogSelection(string selectedSpeakerName, string correctSpeakerName, string result)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
        {
            writer.WriteLine($"{timestamp}, {selectedSpeakerName}, {correctSpeakerName}, {result}");
        }

        Debug.Log($"Log saved: {timestamp}, Selected: {selectedSpeakerName}, Correct: {correctSpeakerName}, Result: {result}");

    }

}
