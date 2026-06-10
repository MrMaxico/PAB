using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenuManager : MonoBehaviour
{
    [Header("ScenesToLoad")]
    #region ScenesToLoad

    [SerializeField] string _gameScene;
    [SerializeField] string _mainMenuScene;

    #endregion

    [Header("Menu Navigation")]
    #region Menu Navigation

    #endregion

    [Header("Continue")]
    #region Continue

    [SerializeField] Button _continueButton;

    #endregion

    [Header("New Game")]
    #region New Game

    [SerializeField] GameObject _newGamePanel;
    [SerializeField] GameObject _firstNewGameButton;

    [SerializeField] string _currentSaveDataName;
    [SerializeField] string _currentSaveFileName;

    #endregion

    [Header("Load Game")]
    #region Save File

    [SerializeField] GameObject _loadGamePanel;
    [SerializeField] GameObject _firstLoadButton;

    [SerializeField] List<GameData> _gameData = new();
    [SerializeField] List<string> _saveDataName = new();
    [SerializeField] List<SaveData> _saveData = new();

    [SerializeField] Transform _saveDataParent;
    [SerializeField] GameObject _saveDataPrefab;

    [SerializeField] SaveFile _saveFileToDelete;

    [SerializeField] GameObject _confirmSaveFileDeletePanel;
    [SerializeField] GameObject _firstConfirmSaveFileDeleteButton;

    #endregion

    [Header("Options")]
    #region Options

    [SerializeField] GameObject _optionsPanel;
    [SerializeField] GameObject _firstOptionsButton;

    [Header("Audio")]
    [SerializeField] GameObject _audioPanel;
    [SerializeField] GameObject _firstAudioButton;

    [Header("Video")]
    [SerializeField] GameObject _videoPanel;
    [SerializeField] GameObject _firstVideoButton;

    [Header("Controls")]
    [SerializeField] GameObject _controlsPanel;
    [SerializeField] GameObject _firstControlsButton;

    #endregion

    [Header("Quit")]
    #region Quit

    [SerializeField] GameObject _quitPanel;

    #endregion

    [SerializeField] GameObject _gameEventSystem;

    // TODO - make this work good 
    #region Do some more testing

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnLoaded;

    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnLoaded;
    }

    public void OnSceneUnLoaded(Scene scene)
    {
        if (scene.name == _mainMenuScene)
        {
            Debug.Log($"Unloaded scene {scene.name}");
            // _gameEventSystem.SetActive(true);
            RefreshSaveFiles();
        }
    }

    #endregion

    #region TEMPORARY

    [SerializeField] GameObject _pauseCanvas;
    [SerializeField] GameObject _loadPanel;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _pauseCanvas.SetActive(!_pauseCanvas.activeSelf);
        }
    }

    #endregion


    private void Awake()
    {
        _currentSaveFileName = DataPersistenceManager.instance.GetCurrentSaveFileName();
    }

    private void Start()
    {
        // TODO - maybe only do when there is save data to load
        // RefreshSaveFiles();
    }

    public void RefreshSaveFiles()
    {
        _gameData.Clear();

        for (int i = 0; i < _saveData.Count; i++)
        {
            Destroy(_saveData[i].gameObject);
        }

        _saveData.Clear();

        GetAllSaveFiles();

        for (int i = 0; i < _saveData.Count; i++)
        {
            int index = i;
            _saveData[index].SaveDataButton.onClick.AddListener(delegate { LoadSaveFileButton(_saveData[index]); });
        }
    }

    public void AutoSaveBtn()
    {
        DataPersistenceManager.instance.ChangeSelectedSaveFile(_currentSaveFileName);
        DataPersistenceManager.instance.SaveAutoGame();
        RefreshSaveFiles();
    }

    public void ManualSaveBtn()
    {
        DataPersistenceManager.instance.ChangeSelectedSaveFile(_currentSaveFileName);
        DataPersistenceManager.instance.SaveManualGame();
        RefreshSaveFiles();
    }

    public void LoadBtn()
    {
        _loadPanel.SetActive(!_loadPanel.activeSelf);
    }

    public void LoadSaveFileButton(SaveData saveSlot)
    {
        // TODO - FIGURE OUT HOW TO MAKE THIS WORK THROUGH SAVE SLOT MAYBE
        Debug.Log($"Load save file name: {saveSlot.SaveFileName} save file name: {saveSlot.SaveDataName} ");
        DataPersistenceManager.instance.LoadGame(saveSlot.SaveFileName, saveSlot.SaveDataName);
    }

    public void GetAllSaveFiles()
    {
        _gameData = DataPersistenceManager.instance.GetSaveDataFromSaveFile(_currentSaveFileName);

        for (int i = 0; i < _gameData.Count; i++)
        {
            _saveData.Add(Instantiate(_saveDataPrefab, _saveDataParent).GetComponent<SaveData>());
            _saveDataName.Add(_gameData[i].saveDataName);
            _saveData[i].SetData(_gameData[i]);
        }
    }

    public void QuitBtn()
    {
        _quitPanel.SetActive(true);
    }

    public void QuitPanelYesBtn()
    {
        DataPersistenceManager.instance.SaveManualGame();

        SceneManager.LoadSceneAsync(_mainMenuScene);

        // TODO - check if this works
        // SceneManager.LoadSceneAsync(_mainMenuScene, LoadSceneMode.Additive);
    }

    public void QuitPanelNoBtn()
    {
        SceneManager.LoadScene(_mainMenuScene);
    }

    public void QuitPanelCancelBtn()
    {
        _quitPanel.SetActive(false);
    }

    public void SetCurrentSaveFileAndData(string newSaveFileName, string newSaveDataName)
    {
        Debug.Log($"SetCurrentSaveFileAndData");
        _currentSaveFileName = newSaveFileName;
        _currentSaveDataName = newSaveDataName;

        DataPersistenceManager.instance.ChangeSelectedSaveFile(_currentSaveFileName);
        DataPersistenceManager.instance.ChangeSelectedSaveData(_currentSaveDataName);

        // DataPersistenceManager.instance.RefreshDataPersistenceObjects();
    }
}