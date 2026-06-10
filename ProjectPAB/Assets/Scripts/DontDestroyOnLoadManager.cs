using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.SceneManagement;

public class DontDestroyOnLoadManager : MonoBehaviour
{

    // this script might or might not get deleted

    public static DontDestroyOnLoadManager instance { get; private set; }
    [SerializeField] Camera _mainCamera;
    [SerializeField] Camera _screenShotCamera;
    [SerializeField] EventSystem _eventSystem;
    [SerializeField] Light _directionalLight;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Another instance of DontDestroyOnLoadManager already exists. Deleting this (new) instance.");
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(_mainCamera.gameObject);
        DontDestroyOnLoad(_screenShotCamera.gameObject);
        DontDestroyOnLoad(_eventSystem.gameObject);
        DontDestroyOnLoad(_directionalLight.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Camera[] cameras = FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != _mainCamera && cameras[i] != _screenShotCamera)
            {
                Destroy(cameras[i].gameObject);
            }
        }

        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] != _eventSystem)
            {
                Destroy(eventSystems[i].gameObject);
            }
        }

        Light[] directionalLights = FindObjectsOfType<Light>();
        for (int i = 0; i < directionalLights.Length; i++)
        {
            if (directionalLights[i] != _directionalLight)
            {
                Destroy(directionalLights[i].gameObject);
            }
        }
    }
}
