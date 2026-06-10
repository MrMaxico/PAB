using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] Image _titleScreenImage;

    [SerializeField] GameObject _menuPanel;

    [Header("Save")]
    #region Save

    [SerializeField] string _currentSaveFileName;
    [SerializeField] string _currentSaveDataName;

    #endregion

    [Header("Scenes")]
    #region Scenes

    [SerializeField] string _gameScene;
    [SerializeField] string _mainMenuScene;

    #endregion

    [Header("Continue")]
    #region Continue

    [SerializeField] Button _continueButton;

    #endregion

    [Header("New Game")]
    #region New Game

    [SerializeField] GameObject _newGamePanel;

    [SerializeField] Button _newGameButton;
    [SerializeField] TMP_InputField _newGameInputField;

    #endregion

    [Header("Save Load")]
    #region Save Load

    [SerializeField] GameObject _saveLoadPanel;

    [SerializeField] Button _loadButton;
    [SerializeField] Button _saveFileButton;
    [SerializeField] Button _saveBackButton;

    [SerializeField] List<GameData> _gameData = new();
    [SerializeField] List<SaveFile> _saveFiles = new();

    [SerializeField] Transform _saveFileParent;
    [SerializeField] GameObject _saveFilePrefab;

    #endregion

    [Header("Options")]
    #region Options

    [SerializeField] GameObject _optionsPanel;

    [SerializeField] Button _optionsButton;
    [SerializeField] Button _optionsNav;

    #endregion

    [Header("Quit")]
    #region Quit

    [SerializeField] Button _quitButton;

    #endregion

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ScreenShotManager.Instance.CreatedScreenshot += TryChangeSaveImage;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ScreenShotManager.Instance.CreatedScreenshot -= TryChangeSaveImage;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == _gameScene)
        {
            FindObjectOfType<GameMenuManager>().SetCurrentSaveFileAndData(_currentSaveFileName, _currentSaveDataName);
        }
        else if (scene.name == _mainMenuScene)
        {
            RefreshSaveFiles();
        }
    }

    private void TryChangeSaveImage()
    {
        TryChangeMainMenuImage(_saveFiles[0]);
    }

    private void Start()
    {
        RefreshSaveFiles();

        SceneManager.LoadSceneAsync(_gameScene, LoadSceneMode.Additive);
    }

    public void RefreshSaveFiles()
    {
        _gameData.Clear();

        for (int i = 0; i < _saveFiles.Count; i++)
        {
            Destroy(_saveFiles[i].gameObject);
        }

        _saveFiles.Clear();

        GetAllSaveFiles();

        if (_saveFiles.Count > 0)
        {
            _saveFileButton = _saveFiles[0].SaveFileButton;
        }

        for (int i = 0; i < _saveFiles.Count; i++)
        {
            int index = i;
            _saveFiles[index].SaveFileButton.onClick.AddListener(delegate { LoadSaveFile(_saveFiles[index]); });

            if (_saveFiles.Count == 1)
            {
                Navigation saveFileNav = _saveFiles[index].SaveFileButton.navigation;
                saveFileNav.mode = Navigation.Mode.Explicit;

                saveFileNav.selectOnDown = _saveBackButton;
                saveFileNav.selectOnUp = _saveBackButton;
                saveFileNav.selectOnRight = _saveFiles[index].DeleteFileButton;

                _saveFiles[index].SaveFileButton.navigation = saveFileNav;

                Navigation deleteNav = _saveFiles[index].DeleteFileButton.navigation;
                deleteNav.mode = Navigation.Mode.Explicit;

                deleteNav.selectOnLeft = _saveFiles[index].SaveFileButton;

                _saveFiles[index].DeleteFileButton.navigation = deleteNav;
            }
            else if (index == _saveFiles.Count - 1)
            {
                Navigation saveFileNav = _saveFiles[index].SaveFileButton.navigation;
                saveFileNav.mode = Navigation.Mode.Explicit;

                saveFileNav.selectOnUp = _saveFiles[index - 1].SaveFileButton;

                // saveFileNav.selectOnDown = _saveFiles[0].SaveFileButton; // back button
                saveFileNav.selectOnDown = _saveBackButton;

                saveFileNav.selectOnRight = _saveFiles[index].DeleteFileButton;

                _saveFiles[index].SaveFileButton.navigation = saveFileNav;

                Navigation deleteNav = _saveFiles[index].DeleteFileButton.navigation;
                deleteNav.mode = Navigation.Mode.Explicit;

                deleteNav.selectOnLeft = _saveFiles[index].SaveFileButton;

                _saveFiles[index].DeleteFileButton.navigation = deleteNav;
            }
            else if (index == 0)
            {
                Navigation saveFileNav = _saveFiles[index].SaveFileButton.navigation;
                saveFileNav.mode = Navigation.Mode.Explicit;

                // saveFileNav.selectOnUp = _saveFiles[_saveFiles.Count - 1].SaveFileButton; // back button
                saveFileNav.selectOnUp = _saveBackButton;

                saveFileNav.selectOnDown = _saveFiles[index + 1].SaveFileButton;

                saveFileNav.selectOnRight = _saveFiles[index].DeleteFileButton;

                _saveFiles[index].SaveFileButton.navigation = saveFileNav;

                Navigation deleteNav = _saveFiles[index].DeleteFileButton.navigation;
                deleteNav.mode = Navigation.Mode.Explicit;

                deleteNav.selectOnLeft = _saveFiles[index].SaveFileButton;

                _saveFiles[index].DeleteFileButton.navigation = deleteNav;
            }
            else
            {
                Debug.Log($"index: {index} Down: {index + 1} Up: {index - 1}");
                Navigation saveFileNav = _saveFiles[index].SaveFileButton.navigation;
                saveFileNav.mode = Navigation.Mode.Explicit;

                saveFileNav.selectOnUp = _saveFiles[index - 1].SaveFileButton;
                saveFileNav.selectOnDown = _saveFiles[index + 1].SaveFileButton;
                saveFileNav.selectOnRight = _saveFiles[index].DeleteFileButton;

                _saveFiles[index].SaveFileButton.navigation = saveFileNav;

                Navigation deleteNav = _saveFiles[index].DeleteFileButton.navigation;
                deleteNav.mode = Navigation.Mode.Explicit;

                deleteNav.selectOnLeft = _saveFiles[index].SaveFileButton;

                _saveFiles[index].DeleteFileButton.navigation = deleteNav;
            }
            _saveFiles[index].DeleteFileButton.onClick.AddListener(delegate { DeleteSaveFileButton(index); });
        }

        Navigation backNav = _saveBackButton.navigation;
        backNav.mode = Navigation.Mode.Explicit;

        if (_saveFiles.Count > 0)
        {
            backNav.selectOnUp = _saveFiles[_saveFiles.Count - 1].SaveFileButton;
            backNav.selectOnDown = _saveFiles[0].SaveFileButton;
        }
    }

    public void GetAllSaveFiles()
    {
        _gameData = DataPersistenceManager.instance.GetNewestSaveFiles();

        if (_gameData.Count == 0)
        {
            EventSystem.current.firstSelectedGameObject = _newGameButton.gameObject;

            _continueButton.interactable = false;
            _loadButton.interactable = false;

            Navigation newGameButtonNav = _newGameButton.navigation;

            newGameButtonNav.selectOnUp = _quitButton;
            newGameButtonNav.selectOnDown = _optionsButton;

            _newGameButton.navigation = newGameButtonNav;

            Navigation optionsButtonNav = _optionsButton.navigation;

            optionsButtonNav.selectOnUp = _newGameButton;
            optionsButtonNav.selectOnDown = _quitButton;

            _optionsButton.navigation = optionsButtonNav;

            Navigation quitButtonNav = _quitButton.navigation;

            quitButtonNav.selectOnDown = _newGameButton;

            _quitButton.navigation = quitButtonNav;

            return;
        }
        else
        {
            EventSystem.current.firstSelectedGameObject = _continueButton.gameObject;

            _continueButton.interactable = true;
            _loadButton.interactable = true;

            Navigation newGameButtonNav = _newGameButton.navigation;

            newGameButtonNav.selectOnUp = _continueButton;
            newGameButtonNav.selectOnDown = _loadButton;

            _newGameButton.navigation = newGameButtonNav;

            Navigation optionsButtonNav = _optionsButton.navigation;

            optionsButtonNav.selectOnUp = _loadButton;
            optionsButtonNav.selectOnDown = _quitButton;

            _optionsButton.navigation = optionsButtonNav;

            Navigation quitButtonNav = _quitButton.navigation;

            quitButtonNav.selectOnDown = _continueButton;

            _quitButton.navigation = quitButtonNav;

        }

        for (int i = 0; i < _gameData.Count; i++)
        {
            _saveFiles.Add(Instantiate(_saveFilePrefab, _saveFileParent).GetComponent<SaveFile>());
            _saveFiles[i].SetData(_gameData[i]);
        }

        DataPersistenceManager.instance.ChangeSelectedSaveFile(_saveFiles[0].SaveFileName);
        DataPersistenceManager.instance.ChangeSelectedSaveData(_saveFiles[0].SaveDataName);

        _currentSaveFileName = _saveFiles[0].SaveFileName;
        _currentSaveDataName = _saveFiles[0].SaveDataName;

        TryChangeMainMenuImage(_saveFiles[0]);

        //TryChangeMainMenuImage(_saveFiles[0]);
    }

    #region Continue

    public void ContinueBtn()
    {
        DataPersistenceManager.instance.ChangeSelectedSaveFile(_saveFiles[0].SaveFileName);
        DataPersistenceManager.instance.ChangeSelectedSaveData(_saveFiles[0].SaveDataName);

        FindObjectOfType<GameMenuManager>().SetCurrentSaveFileAndData(_saveFiles[0].SaveFileName, _saveFiles[0].SaveDataName);

        SceneManager.UnloadSceneAsync(_mainMenuScene);
    }

    #endregion

    #region New Game 

    public void NewGameBtn()
    {
        EventSystem.current.SetSelectedGameObject(_newGameInputField.gameObject);

        _newGamePanel.SetActive(false);
        _saveLoadPanel.SetActive(false);
        _optionsPanel.SetActive(false);

        _menuPanel.SetActive(true);
        _newGamePanel.SetActive(true);
    }

    public void NewGame()
    {
        // TODO - this works for now maybe find better way later
        _currentSaveDataName = "data.game";
        DataPersistenceManager.instance.ChangeSelectedSaveData(_currentSaveDataName);

        if (_currentSaveFileName == "")
        {
            Debug.LogWarning("Save file name can not be nothing");
            return;
        }

        if (!DataPersistenceManager.instance.CheckIfSelectedSaveFileExists(_currentSaveFileName))
        {
            DataPersistenceManager.instance.NewGame();
            RefreshSaveFiles();
        }
        else
        {
            Debug.LogWarning($"{_currentSaveFileName} already exists not creating a new game");
            return;
        }

        FindObjectOfType<GameMenuManager>().SetCurrentSaveFileAndData(_currentSaveFileName, _currentSaveDataName);

        DataPersistenceManager.instance.LoadGame(_currentSaveFileName, _currentSaveDataName);

        _menuPanel.SetActive(false);
        _newGamePanel.SetActive(false);
        _saveLoadPanel.SetActive(false);
        _optionsPanel.SetActive(false);

        EventSystem.current.SetSelectedGameObject(_newGameButton.gameObject);
    }

    public void SaveFileNameInputField()
    {
        _currentSaveFileName = _newGameInputField.text;
    }

    #endregion

    #region Save&Load

    public void LoadBtn() // Add functionality for menu navigation 
    {
        if (_saveFiles.Count > 0)
        {
            // EventSystem.current.firstSelectedGameObject = null;
            EventSystem.current.SetSelectedGameObject(_saveFileButton.gameObject);
        }
        else
        {
            // EventSystem.current.firstSelectedGameObject = null;
            EventSystem.current.SetSelectedGameObject(_saveBackButton.gameObject);
        }

        _newGamePanel.SetActive(false);
        _saveLoadPanel.SetActive(false);
        _optionsPanel.SetActive(false);

        _menuPanel.SetActive(true);
        _saveLoadPanel.SetActive(true);
    }

    public void LoadSaveFile(SaveFile saveSlot)
    {
        DataPersistenceManager.instance.ChangeSelectedSaveFile(saveSlot.SaveFileName);
        DataPersistenceManager.instance.ChangeSelectedSaveData(saveSlot.SaveDataName);

        _currentSaveFileName = saveSlot.SaveFileName;
        _currentSaveDataName = saveSlot.SaveDataName;

        DataPersistenceManager.instance.LoadGame(saveSlot.SaveFileName, saveSlot.SaveDataName);

        FindObjectOfType<GameMenuManager>().SetCurrentSaveFileAndData(_currentSaveFileName, _currentSaveDataName);

        SceneManager.UnloadSceneAsync(_mainMenuScene);
    }

    public void DeleteSaveFileButton(int index)
    {
        Debug.Log("Delete");
        // TODO - make this work if a save file has already been deleted
        DataPersistenceManager.instance.DeleteSelectedSaveFile(_saveFiles[index].SaveFileName);

        SaveFile saveSlotToDelete = _saveFiles[index];

        _gameData.RemoveAt(index);
        _saveFiles.RemoveAt(index);

        Destroy(saveSlotToDelete.gameObject);

        RefreshSaveFiles();
    }

    #endregion

    #region Options

    public void OptionsBtn() // Add functionality for menu navigation 
    {
        EventSystem.current.SetSelectedGameObject(_optionsNav.gameObject);

        _newGamePanel.SetActive(false);
        _saveLoadPanel.SetActive(false);
        _optionsPanel.SetActive(false);

        _menuPanel.SetActive(true);
        _optionsPanel.SetActive(true);
    }

    #endregion

    #region Quit

    public void QuitBtn()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        Application.Quit();
#endif
    }

    #endregion

    public void BackBtn()
    {
        _menuPanel.SetActive(false);
        _newGamePanel.SetActive(false);
        _saveLoadPanel.SetActive(false);
        _optionsPanel.SetActive(false);
    }

    void TryChangeMainMenuImage(SaveFile saveFile) // REWORK ( this needs to grab the erea the save was made in world instead of the exact save img and should set it on the main menu )
    {
        int dotIndex = saveFile.SaveDataName.LastIndexOf('.');
        string tempDataName = saveFile.SaveDataName.Substring(0, dotIndex);

        Debug.Log($"Trying to get title screen image from: {saveFile.SaveFileName} {saveFile.SaveDataName} {saveFile.SaveType}");

        string screenshotPattern = $"{tempDataName}*.png";
        string[] files = Directory.GetFiles(Path.Combine(Application.persistentDataPath, saveFile.SaveFileName, saveFile.SaveType), screenshotPattern, SearchOption.AllDirectories);

        if (files.Length > 0)
        {
            string latestFile = files[files.Length - 1];

            byte[] byteArray = File.ReadAllBytes(latestFile);

            Texture2D texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            try
            {
                // Attempt to load the Raw
                texture2D.LoadRawTextureData(byteArray); // Load raw texture data
            }
            catch
            {

                try
                {
                    // If loading as Raw fails, load the PNG
                    texture2D.LoadImage(byteArray); // Load the PNG data

                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to load as either Raw texture or Png: {e}");
                }
            }

            texture2D.Apply();

            _titleScreenImage.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogWarning("No screenshot found.");
        }
    }

}
