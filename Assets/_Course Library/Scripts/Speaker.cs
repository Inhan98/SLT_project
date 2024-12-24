using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Net.Sockets;
using System.Text;
using System;

public class Speaker : MonoBehaviour
{
    private AudioSource audioSource; 
    private MeshRenderer meshRenderer; // MeshRenderer 사용
    private GameManager gameManager;

    private NetworkStream stream; // matlab 통신을 위한 코드.

    public AudioClip personalizedHRTF; //Personalized HRTF
    public AudioClip genericHRTF; // Generic HRTF
    public AudioClip unrelatedHRTF; // Unrelated HRTF


    public AudioClip correctSound; // 정답일 때 재생할 소리
    public AudioClip wrongSound; // 오답일 때 재생할 소리

    public AudioClip clickSound; // 클릭할 때 재생할 소리

    public Material correctMaterial; // 정답일 때 적용할 재질
    public Material wrongMaterial; // 오답일 때 적용할 재질
    public Material defaultMaterial; // 기본 재질

    public Material clickMaterial;

    public bool isFake; // fake가 on 되어 있을 때 스피커에서 소리가 재생이 되지 않음.

    public List<int> RandomNoiseList = new List<int>();



    void Awake()
    {
        this.audioSource = this.GetComponent<AudioSource>();
        if (this.audioSource == null)
        {
            Debug.LogError("AudioSource component is missing on " + this.gameObject.name);
        }
        else
        {
            Debug.Log("AudioSource component found on " + this.gameObject.name);
        }

        this.meshRenderer = this.GetComponent<MeshRenderer>(); // MeshRenderer 컴포넌트 가져오기
        if (this.meshRenderer == null)
        {
            Debug.LogError("MeshRenderer component is missing on " + this.gameObject.name);
        }
        else
        {
            Debug.Log("MeshRenderer component found on " + this.gameObject.name);
        }

        this.gameManager = FindObjectOfType<GameManager>();
        if (this.gameManager == null)
        {
            Debug.LogError("GameManager component not found in the scene.");
        }
        else{
    
            Debug.Log("GameManager component found in the scene.");
            //stream = gameManager.stream;
        }
        //ConnectToMatlab();
    }

    void Start()
    {
        StartCoroutine(InitializeAfterStart());
    }

    private IEnumerator InitializeAfterStart()
    {
        yield return null;

        LogAllComponents();
        ResetMaterial();
    }



    public void SetStream(NetworkStream networkStream)
    {
        this.stream = networkStream;
    }

    // 매트랩으로 stream 보내는 코드
    public void SendSoundToMatlab(int cnt)
    {
        if(stream != null)
        {
            string soundType = GetSoundType(cnt);  // list에서 personalized, general, unrelated로 변환환
            string angle = ExtractAngleFromName(); // object name에서 각도 추출하는 함수
            string message = $"{soundType},{angle}";

            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length); // 신호 송신
            Debug.Log("Sound request sent to MATLAB: " + message);
        }

        else
        {
            Debug.LogError("Stream is not initialized.");
        }

    }

    private string ExtractAngleFromName()
    {
        string objectName = this.gameObject.name;
        int startIndex = objectName.LastIndexOf('_') + 1;
        return objectName.Substring(startIndex);

    }

    private string GetSoundType(int cnt)
    {
        switch(cnt)
        {
            case 0:
                return "Personalized";
            case 1:
                return "Generic";
            case 2:
                return "Unrelated";
            default:
                Debug.LogError("Invalid noise type.");
                return "Invalid";
        }
    }



    public void PlayWhiteNoise(int cnt) // 사운드 재생 함수 => 매트랩에서 사운드 재생 시 필요없음
    {  
        if (cnt==0)
        {
            this.audioSource.PlayOneShot(this.personalizedHRTF);
        }
        else if (cnt==1)
        {
            this.audioSource.PlayOneShot(this.genericHRTF);
        }
        else if (cnt==2)
        {
            this.audioSource.PlayOneShot(this.unrelatedHRTF);
        }
        else
        {
            Debug.LogError("There are no assigned white noise.");
        }

        
    }


    public void PlaySound()
    {
        if (this.audioSource != null)
        {
            this.audioSource.Play();
        }
    }

    public void PlayCorrectSound()
    {
        if (this.audioSource != null && this.correctSound != null)
        {
            this.audioSource.PlayOneShot(this.correctSound);
        }
    }

    public void PlayWrongSound()
    {
        if (this.audioSource != null && this.wrongSound != null)
        {
            this.audioSource.PlayOneShot(this.wrongSound);
        }
    }

    public void PlayClickSound()
    {
        if (this.audioSource != null && this.clickSound != null)
        {
            this.audioSource.PlayOneShot(this.clickSound);
        }        
    }



    public void ResetMaterial() // speaker color 초기화
    {
        Debug.Log("ResetMaterial called on " + this.gameObject.name);
        if (this.defaultMaterial == null)
        {
            Debug.LogError("Default material is not set on " + this.gameObject.name);
        }
        else
        {
            Debug.Log("Default material is set on " + this.gameObject.name);
        }
        SetMaterial(this.defaultMaterial);
    }

    public void SetMaterial(Material material) // 스피커 color 설정
    {
        Debug.Log("SetMaterial called on " + this.gameObject.name + " with material " + material.name);
        if (this.meshRenderer != null)
        {
            if (material == null)
            {
                Debug.LogError("Material is null on " + this.gameObject.name);
            }
            else
            {
                this.meshRenderer.material = material;
                Debug.Log("Material set on " + this.gameObject.name);
            }
        }
        else
        {
            Debug.LogError("MeshRenderer is null on " + this.gameObject.name + ". Cannot set material.");
        }
    }

    public void OnSelectEntered(SelectEnterEventArgs args) // 클리커 클릭 시 반응하는 함수.
    {
        Debug.Log("OnSelectEntered called on " + this.gameObject.name);
        if (this.gameManager != null)
        {
            this.gameManager.CheckSpeaker(this);
        }
        else
        {
            Debug.LogError("GameManager is null in OnSelectEntered for " + this.gameObject.name);
        }
    }

    private void LogAllComponents()
    {
        Component[] components = this.GetComponents<Component>();
        foreach (Component component in components)
        {
            Debug.Log(this.gameObject.name + " has component: " + component.GetType().Name);
        }
    }
}
