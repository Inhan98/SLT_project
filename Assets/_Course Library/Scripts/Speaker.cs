using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Speaker : MonoBehaviour
{
    private AudioSource audioSource;
    private MeshRenderer meshRenderer; // MeshRenderer 사용
    private GameManager gameManager;

    public AudioClip correctSound; // 정답일 때 재생할 소리
    public AudioClip wrongSound; // 오답일 때 재생할 소리
    public Material correctMaterial; // 정답일 때 적용할 재질
    public Material wrongMaterial; // 오답일 때 적용할 재질
    public Material defaultMaterial; // 기본 재질

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

    public void ResetMaterial()
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

    public void SetMaterial(Material material)
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

    public void OnSelectEntered(SelectEnterEventArgs args)
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
