using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : MonoBehaviour
{
    [Header("Debugging")]
    [SerializeField] bool _initializeDataIfNull;

    [Header("File Storage Config")]

    [SerializeField] string _saveDataName = "";
    public string SaveDataName
    {
        get { return _saveDataName; }
        set { _saveDataName = value; }
    }

    [SerializeField] string _saveFileName = "";

    [SerializeField] bool _useEncryption;
    [SerializeField] int _maxAutoSaves;

    public static DataPersistenceManager instance { get; private set; }

    private GameData _gameData;
    private List<IDataPersistence> _dataPersistenceObjects;
    private FileDataHandler _dataHandler;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Another instance of DataPersistenceManager already exists. Deleting this (new) instance.");
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        this._dataHandler = new FileDataHandler(Application.persistentDataPath, _saveDataName, _useEncryption, _maxAutoSaves);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // SceneManager.sceneUnloaded += OnSceneUnLoaded;

    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // SceneManager.sceneUnloaded -= OnSceneUnLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            Debug.LogWarning($"Should not load in the {scene.name}");
            return;
        }
        if (scene.name == "Game")
        {
            Debug.LogWarning($"Loaded scene: {scene.name}");

            this._gameData = _dataHandler.LoadSaveFile(_saveFileName, _saveDataName);

            if (this._gameData == null)
            {
                Debug.LogWarning($"Should not load when there is no data");
                return;
            }

            this._dataPersistenceObjects = FindAllDataPersistenceObjects();

            Debug.Log($"there are : {_dataPersistenceObjects.Count} data persistence object(s)");

            FindObjectOfType<GameMenuManager>().SetCurrentSaveFileAndData(_saveFileName, _saveDataName);

            LoadGame(_saveFileName, _saveDataName);
        }
    }

    public bool CheckIfSelectedSaveFileExists(string newSaveFileName)
    {
        return _dataHandler.CheckIfSaveFileExists(newSaveFileName);
    }

    #region ChangeCurrentSave

    public void ChangeSelectedSaveFile(string newSaveFileName)
    {
        this._saveFileName = newSaveFileName;
    }

    public void ChangeSelectedSaveData(string newSaveDataName)
    {
        this._saveDataName = newSaveDataName;
    }

    #endregion

    #region Delete

    public void DeleteSelectedSaveFile(string saveDataName)
    {
        _dataHandler.DeleteSaveFile(saveDataName);
    }

    #endregion

    #region NewGame

    public void NewGame()
    {
        this._gameData = new GameData();

        _gameData.lastUpdated = System.DateTime.Now.ToBinary();

        this._dataPersistenceObjects = FindAllDataPersistenceObjects();

        _dataHandler.ManualSave(_gameData, _saveFileName);
    }

    #endregion

    #region Save

    public void SaveManualGame()
    {
        if (this._gameData == null)
        {
            Debug.LogWarning("No data was found. A new game must be started before data can be saved");
            this._gameData = new GameData();
            // return;
        }

        // pass the data to other scripts so they can update it
        foreach (IDataPersistence dataPersistenceObj in _dataPersistenceObjects)
        {
            dataPersistenceObj.SaveData(_gameData);
        }

        // save that data to a file using the file data handler
        _dataHandler.ManualSave(_gameData, _saveFileName);
    }

    public void SaveAutoGame()
    {
        if (this._gameData == null)
        {
            Debug.LogWarning("No data was found. A new game must be started before data can be saved");
            // return;
        }

        // pass the data to other scripts so they can update it
        foreach (IDataPersistence dataPersistenceObj in _dataPersistenceObjects)
        {
            dataPersistenceObj.SaveData(_gameData);
        }

        // save that data to a file using the file data handler
        _dataHandler.AutoSave(_gameData, _saveFileName);
    }

    #endregion

    #region Load

    public void LoadGame(string saveFileName, string saveDataName)
    {
        Debug.Log($"Trying to load save file: ({saveFileName}) save data: ({saveDataName})");

        this._gameData = _dataHandler.LoadSaveFile(saveFileName, saveDataName);

        if (this._gameData == null && _initializeDataIfNull)
        {
            NewGame();
        }

        if (this._gameData == null)
        {
            Debug.Log("No game data was found. A new game needs to be started before it can be loaded");
            return;
        }

        foreach (IDataPersistence dataPersistenceObj in _dataPersistenceObjects)
        {
            dataPersistenceObj.LoadData(_gameData);
        }

        Debug.Log($"Loaded save file: ({saveFileName}) save data: ({saveDataName})");
    }

    #endregion

    #region GetSave

    public List<GameData> GetSaveDataFromSaveFile(string saveFileName)
    {
        return _dataHandler.GetSaveDataFromSaveFile(saveFileName);
    }

    public List<GameData> GetNewestSaveFiles()
    {
        return _dataHandler.GetNewestSaveDataFromSaveFiles();
    }

    public string GetCurrentSaveFileName()
    {
        return _saveFileName;
    }

    #endregion

    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>();

        return new List<IDataPersistence>(dataPersistenceObjects);
    }

    public bool HasGameData()
    {
        return _gameData != null;
    }
}
