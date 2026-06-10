using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class Settings : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] AudioMixer _audioMixer;
    [SerializeField] TMP_InputField _masterInput;
    [SerializeField] TMP_InputField _musicInput;
    [SerializeField] TMP_InputField _sfxInput;

    [SerializeField] Slider _masterSlider;
    [SerializeField] Slider _musicSlider;
    [SerializeField] Slider _sfxSlider;

    [Header("Video")]
    List<Resolution> _resolutions = new();
    [SerializeField] TMP_Dropdown _resDropDown;

    [SerializeField] TMP_Dropdown _fullScreenDrowdown;

    [SerializeField] Slider _fpsSlider;
    [SerializeField] TMP_InputField _fpsInput;

    // Start is called before the first frame update
    void Start()
    {
        GetAndSetResolution();


        #region Audio Start
        if (_audioMixer != null)
        {
            if (_masterSlider != null && _masterInput != null)
            {
                _audioMixer.SetFloat("MasterVol", PlayerPrefs.GetFloat("MasterVol"));
                _masterSlider.value = PlayerPrefs.GetFloat("MasterVol");
                _masterInput.text = PlayerPrefs.GetFloat("MasterVol").ToString("0");
            }

            if (_musicSlider != null && _musicInput != null)
            {
                _audioMixer.SetFloat("MusicVol", PlayerPrefs.GetFloat("MusicVol"));
                _musicSlider.value = PlayerPrefs.GetFloat("MusicVol");
                _musicInput.text = PlayerPrefs.GetFloat("MusicVol").ToString("0");
            }

            _audioMixer.SetFloat("SFXVol", PlayerPrefs.GetFloat("SfxVol"));
            _sfxSlider.value = PlayerPrefs.GetFloat("SfxVol");
            _sfxInput.text = PlayerPrefs.GetFloat("SfxVol").ToString("0");
        }
        #endregion
    }

    #region Audio
    public void SetMasterVol(float masterLvl)
    {
        _audioMixer.SetFloat("MasterVol", Mathf.Log10(masterLvl) * 20);
        PlayerPrefs.SetFloat("MasterVol", masterLvl);
        _masterInput.text = (_masterSlider.value * 100).ToString("0");
    }

    public void SetMusicVol(float musicLvl)
    {
        _audioMixer.SetFloat("MusicVol", Mathf.Log10(musicLvl) * 20);
        PlayerPrefs.SetFloat("MusicVol", musicLvl);
        _musicInput.text = (_musicSlider.value * 100).ToString("0");
    }
    public void SetSFXVol(float sfxLvl)
    {
        _audioMixer.SetFloat("SFXVol", Mathf.Log10(sfxLvl) * 20);
        PlayerPrefs.SetFloat("SfxVol", sfxLvl);
        _sfxInput.text = (_sfxSlider.value * 100).ToString("0");
    }
    public void SetRobotVol(float robotLvl)
    {
        _audioMixer.SetFloat("RobotVol", Mathf.Log10(robotLvl) * 20);
        PlayerPrefs.SetFloat("RobotVol", robotLvl);
        _sfxInput.text = (_sfxSlider.value * 100).ToString("0");
    }

    public void SetMasterVolInput()
    {
        float newMasterValue;

        float.TryParse(_masterInput.text, out newMasterValue);
        newMasterValue /= 100;
        if (newMasterValue < _masterSlider.minValue)
        {
            newMasterValue = _masterSlider.minValue;
            _masterSlider.value = newMasterValue;
        }
        else if (newMasterValue > _masterSlider.maxValue)
        {
            newMasterValue = _masterSlider.maxValue;
            _masterSlider.value = newMasterValue;
        }
        else
        {
            _masterSlider.value = newMasterValue;
        }

        _audioMixer.SetFloat("MasterVol", Mathf.Log10(newMasterValue) * 20);
        _masterInput.text = (newMasterValue * 100).ToString("0");
    }

    public void SetMusicVolInput()
    {
        float newMusicValue;

        float.TryParse(_musicInput.text, out newMusicValue);
        newMusicValue /= 100;
        if (newMusicValue < _musicSlider.minValue)
        {
            newMusicValue = _musicSlider.minValue;
            _musicSlider.value = newMusicValue;
        }
        else if (newMusicValue > _musicSlider.maxValue)
        {
            newMusicValue = _musicSlider.maxValue;
            _musicSlider.value = newMusicValue;
        }
        else
        {
            _musicSlider.value = newMusicValue;
        }
        _audioMixer.SetFloat("MusicVol", Mathf.Log10(newMusicValue) * 20);
        _musicInput.text = (newMusicValue * 100).ToString("0");
    }

    public void SetSfxVolInput()
    {
        float newSfxValue;

        float.TryParse(_sfxInput.text, out newSfxValue);
        newSfxValue /= 100;
        if (newSfxValue < _sfxSlider.minValue)
        {
            newSfxValue = _sfxSlider.minValue;
            _sfxSlider.value = newSfxValue;
        }
        else if (newSfxValue > _sfxSlider.maxValue)
        {
            newSfxValue = _sfxSlider.maxValue;
            _sfxSlider.value = newSfxValue;
        }
        else
        {
            _sfxSlider.value = newSfxValue;
        }
        _audioMixer.SetFloat("SFXVol", Mathf.Log10(newSfxValue) * 20);

        _sfxInput.text = (newSfxValue * 100).ToString("0");
    }

    #endregion

    #region Video

    #region Resolution

    void GetAndSetResolution()
    {
        // Clear the previous list of resolutions
        _resolutions.Clear();

        // Retrieve the distinct resolutions supported by the monitor
        Resolution[] allResolutions = Screen.resolutions
            .Distinct()
            .OrderByDescending(r => r.width * r.height)
            .ThenByDescending(r => r.refreshRateRatio)
            .ToArray();

        // Add resolutions to the list while ensuring no duplicates
        foreach (var res in allResolutions)
        {
            if (!_resolutions.Exists(r => r.width == res.width && r.height == res.height))
            {
                _resolutions.Add(res);
            }
        }

        // Clear and populate the dropdown options
        _resDropDown.ClearOptions();
        List<string> options = new();
        for (int i = 0; i < _resolutions.Count; i++)
        {
            // string option = $"{_resolutions[i].width}x{_resolutions[i].height} @ {_resolutions[i].refreshRateRatio}Hz";
            string option = $"{_resolutions[i].width}x{_resolutions[i].height}";
            options.Add(option);
        }
        _resDropDown.AddOptions(options);

        // Default to the highest resolution
        int defaultResolutionIndex = 0; // Set to the first resolution in the sorted list
        SetResolution(defaultResolutionIndex);
        _resDropDown.value = defaultResolutionIndex;
        _resDropDown.RefreshShownValue();

        // Configure FPS slider
        float refreshRate = _resolutions[defaultResolutionIndex].refreshRateRatio.numerator / (float)_resolutions[defaultResolutionIndex].refreshRateRatio.denominator;
        if (_fpsSlider != null)
        {
            _fpsSlider.maxValue = refreshRate;
            _fpsSlider.value = refreshRate;
            Application.targetFrameRate = (int)refreshRate;
        }

        SetScreenOptions(0); // Set default fullscreen mode
    }

    void SetResolution(int index)
    {
        if (index >= 0 && index < _resolutions.Count)
        {
            // Apply the selected resolution and refresh rate
            Resolution selectedResolution = _resolutions[index];
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreenMode, selectedResolution.refreshRateRatio);

            Debug.Log($"Resolution set to: {selectedResolution.width}x{selectedResolution.height} @ {selectedResolution.refreshRateRatio}Hz");

            // Update dropdown value
            _resDropDown.value = index;
            _resDropDown.RefreshShownValue();
        }
        else
        {
            Debug.LogWarning("Invalid resolution index selected.");
        }
    }

    public void NewResolution(int index)
    {
        _resDropDown.value = index;
        _resDropDown.RefreshShownValue();

        SetResolution(index);
    }

    #endregion

    #region Fullscreen

    public void SetScreenOptions(int index)
    {
        switch (index)
        {
            case 0:
                {
                    FullScreen();
                }
                break;
            case 1:
                {
                    Borderless();
                }
                break;
            case 2:
                {
                    Windowed();
                }
                break;
        }
    }

    void FullScreen()
    {
        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
    }
    void Borderless()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }
    void Windowed()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    #endregion

    public void DoVsync(bool value)
    {
        if (value)
        {
            QualitySettings.vSyncCount = 1;

            // Get the refresh rate as a float from refreshRateRatio
            float refreshRate = _resolutions[0].refreshRateRatio.numerator / (float)_resolutions[0].refreshRateRatio.denominator;

            Application.targetFrameRate = (int)refreshRate;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1; // Set to -1 to let Unity handle it
        }
    }

    public void LimitFPS(float value)
    {
        if (QualitySettings.vSyncCount == 0)
        {
            Application.targetFrameRate = (int)value;
        }

        _fpsInput.text = value.ToString("0");
    }

    public void LimitFPSInput()
    {
        float newFpsValue;

        float.TryParse(_fpsInput.text, out newFpsValue);
        if (newFpsValue < _fpsSlider.minValue)
        {
            newFpsValue = _fpsSlider.minValue;
            _fpsSlider.value = newFpsValue;
        }
        else if (newFpsValue > _fpsSlider.maxValue)
        {
            newFpsValue = _fpsSlider.maxValue;
            _fpsSlider.value = newFpsValue;
        }
        else
        {
            _fpsSlider.value = newFpsValue;
        }
    }

    #endregion
}