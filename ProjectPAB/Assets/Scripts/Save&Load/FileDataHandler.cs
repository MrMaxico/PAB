using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class FileDataHandler
{
    private string _dataDirPath = "";

    private string _saveDataName = "";
    private string _tempDataFileName = "";

    private bool _useEncryption;

    private int _maxAutoSaves;

    private readonly string encryptionCodeWord = "egassem sdrawkcab";

    private bool _isSaving;

    public FileDataHandler(string dataDirPath, string saveDataName, bool useEncryption, int maxAutoSaves)
    {
        this._dataDirPath = dataDirPath;
        this._saveDataName = saveDataName;
        this._useEncryption = useEncryption;
        this._maxAutoSaves = maxAutoSaves;
    }

    public bool CheckIfSaveFileExists(string saveFileName)
    {
        string directoryPath = Path.Combine(_dataDirPath, saveFileName);
        Debug.Log($"{directoryPath} exists: {Directory.Exists(directoryPath)}");

        if (Directory.Exists(directoryPath))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void DeleteSaveFile(string saveFileName)
    {
        try
        {
            string mainDirectoryPath = Path.Combine(_dataDirPath, saveFileName);
            if (Directory.Exists(mainDirectoryPath))
            {
                Directory.Delete(mainDirectoryPath, true);
                Debug.Log($"Deleted main save directory: {mainDirectoryPath}");
            }
            else
            {
                Debug.Log($"Main save directory not found: {mainDirectoryPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error occurred while deleting save data: {e}");
        }
    }

    #region GetSaveData

    public List<GameData> GetAutoSaveData(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName))
        {
            Debug.LogError("Profile ID is null or empty");
            return null;
        }

        string autoSaveDirectoryPath = Path.Combine(_dataDirPath, saveFileName, "auto");

        if (!Directory.Exists(autoSaveDirectoryPath))
        {
            Debug.LogWarning($"Auto save directory not found: {autoSaveDirectoryPath}");
            return new List<GameData>();
        }

        List<GameData> autoSaveData = new List<GameData>();

        try
        {
            foreach (string autoSaveFilePath in Directory.GetFiles(autoSaveDirectoryPath, "data*.game"))
            {
                string dataToLoad;
                using (FileStream stream = new FileStream(autoSaveFilePath, FileMode.Open))
                using (StreamReader reader = new StreamReader(stream))
                {
                    dataToLoad = reader.ReadToEnd();
                }

                if (_useEncryption)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                autoSaveData.Add(JsonUtility.FromJson<GameData>(dataToLoad));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error occurred when trying to load auto save files: {e}");
        }

        return autoSaveData;
    }

    public List<GameData> GetManualSaveData(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName))
        {
            Debug.LogError("Profile ID is null or empty");
            return null;
        }

        string manualSaveFilePath = Path.Combine(_dataDirPath, saveFileName, "manual", _saveDataName);

        if (!File.Exists(manualSaveFilePath))
        {
            Debug.LogWarning($"Manual save file not found: {manualSaveFilePath}");
            return new List<GameData>();
        }

        try
        {
            string dataToLoad;
            using (FileStream stream = new FileStream(manualSaveFilePath, FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
            {
                dataToLoad = reader.ReadToEnd();
            }

            if (_useEncryption)
            {
                dataToLoad = EncryptDecrypt(dataToLoad);
            }

            var manualSaveData = new List<GameData> { JsonUtility.FromJson<GameData>(dataToLoad) };
            return manualSaveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error occurred when trying to load manual save file: {e}");
            return new List<GameData>();
        }
    }

    #endregion

    #region GetSaveDataFromDirectory

    public Dictionary<string, GameData> GetAutoSaveDataFromDirectory()
    {
        Dictionary<string, GameData> profileDictionary = new Dictionary<string, GameData>();

        try
        {
            DirectoryInfo dataDirectory = new DirectoryInfo(_dataDirPath);
            foreach (DirectoryInfo saveFileDir in dataDirectory.EnumerateDirectories())
            {
                string saveFileName = saveFileDir.Name;
                string autoDirectoryPath = Path.Combine(saveFileDir.FullName, "auto");
                string[] autoSaveFiles = Directory.GetFiles(autoDirectoryPath, "*.game");

                if (!Directory.Exists(autoDirectoryPath) || autoSaveFiles.Length == 0)
                {
                    Debug.LogWarning($"Auto directory or file not found for profile: {saveFileName}");
                    continue;
                }

                List<GameData> profileDatas = GetAutoSaveData(saveFileName);
                for (int i = 0; i < autoSaveFiles.Length && i < profileDatas.Count; i++)
                {
                    string autoSaveFilePath = autoSaveFiles[i];
                    GameData currentData = profileDatas[i];

                    if (currentData != null)
                    {
                        string uniqueKey = $"{saveFileName}_{Path.GetFileName(autoSaveFilePath)}";
                        profileDictionary.Add(uniqueKey, currentData);
                        Debug.Log($"Loaded profile: {saveFileName}, Save file: {Path.GetFileName(autoSaveFilePath)}, Path: {autoSaveFilePath}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to load profile: {saveFileName}, Save file: {Path.GetFileName(autoSaveFilePath)}, Path: {autoSaveFilePath}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            // TODO - MAKE SURE THIS ONLY RETURNS AN ERROR WHEN SOMETHING GOES WRONG ( Currently also returns error when there is no auto save )
            Debug.LogWarning($"Warning occurred while loading auto profiles: {e}");
        }

        Debug.Log($"Auto profiles loaded: {profileDictionary.Count}");
        return profileDictionary;
    }

    public Dictionary<string, GameData> GetManualSaveDataFromDirectory()
    {
        Dictionary<string, GameData> profileDictionary = new Dictionary<string, GameData>();

        try
        {
            DirectoryInfo dataDirectory = new DirectoryInfo(_dataDirPath);
            foreach (DirectoryInfo profileDir in dataDirectory.EnumerateDirectories())
            {
                string profileId = profileDir.Name;
                string manualDirectoryPath = Path.Combine(profileDir.FullName, "manual");
                string manualFilePath = Path.Combine(manualDirectoryPath, _saveDataName);

                if (!Directory.Exists(manualDirectoryPath) || !File.Exists(manualFilePath))
                {
                    Debug.LogWarning($"Manual directory or file not found for path:\n{manualDirectoryPath}\n{manualFilePath}");
                    continue;
                }

                List<GameData> profileDatas = GetManualSaveData(profileId);
                if (profileDatas != null && profileDatas.Count > 0)
                {
                    profileDictionary.Add(profileId + "_" + _saveDataName, profileDatas[0]);
                    Debug.Log($"Loaded profile: {profileId}, Save file: {_saveDataName}, Path: {manualFilePath}");
                }
                else
                {
                    Debug.LogError($"Failed to load profile: {profileId}, Save file: {_saveDataName}, Path: {manualFilePath}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error occurred while loading manual profiles: {e}");
        }

        Debug.Log($"Manual profiles loaded: {profileDictionary.Count}");
        return profileDictionary;
    }

    #endregion

    #region GetSaveDataFromSaveFile

    public List<GameData> GetSaveDataFromSaveFile(string saveFileName)
    {
        List<GameData> manualSaveData = GetManualSaveData(saveFileName);
        List<GameData> autoSaveData = GetAutoSaveData(saveFileName);

        if (_useEncryption)
        {
            var decryptedDates = manualSaveData.Select(data => new
            {
                OriginalData = data,
                DecryptedLastUpdated = EncryptDecryptDateTime(DateTime.FromBinary(data.lastUpdated))
            });

            var sortedData = decryptedDates.OrderByDescending(data => data.DecryptedLastUpdated).Select(data => data.OriginalData).ToList();
        }
        else
        {
            manualSaveData = manualSaveData.OrderByDescending(data => DateTime.FromBinary(data.lastUpdated)).ToList();
        }

        if (_useEncryption)
        {
            var decryptedDates = autoSaveData.Select(data => new
            {
                OriginalData = data,
                DecryptedLastUpdated = EncryptDecryptDateTime(DateTime.FromBinary(data.lastUpdated))
            });

            var sortedData = decryptedDates.OrderByDescending(data => data.DecryptedLastUpdated).Select(data => data.OriginalData).ToList();
        }
        else
        {
            autoSaveData = autoSaveData.OrderByDescending(data => DateTime.FromBinary(data.lastUpdated)).ToList();
        }

        List<GameData> saveData = new List<GameData>();

        saveData.AddRange(manualSaveData);

        saveData.AddRange(autoSaveData);

        return saveData;
    }

    public List<GameData> GetNewestSaveDataFromSaveFiles()
    {
        Dictionary<string, GameData> autoDictionary = GetAutoSaveDataFromDirectory();
        Dictionary<string, GameData> manualDictionary = GetManualSaveDataFromDirectory();

        List<GameData> tempOrderedGameDatas = new();
        _saveDataName = "data.game";
        IEnumerable<DirectoryInfo> dirInfos = new DirectoryInfo(_dataDirPath).EnumerateDirectories();

        foreach (DirectoryInfo dirInfo in dirInfos)
        {
            List<GameData> newGameDatas = new();
            string saveFileName = dirInfo.Name;

            if (autoDictionary != null)
            {
                for (int i = 0; i < autoDictionary.Count; i++)
                {
                    string tempAutoKey = saveFileName + "_data" + i + ".game";
                    string autoFilePath = Path.Combine(_dataDirPath, saveFileName, "auto", "data" + i + ".game");

                    if (File.Exists(autoFilePath))
                    {
                        if (autoDictionary.TryGetValue(tempAutoKey, out GameData tempAutoGameData) && tempAutoGameData != null)
                        {
                            newGameDatas.Add(tempAutoGameData);
                            Debug.Log($"Time: {DateTime.FromBinary(tempAutoGameData.lastUpdated)}\nAuto save path: {tempAutoKey}");
                        }
                    }
                    else
                    {
                        Debug.Log($"File does not exist: {autoFilePath}");
                    }
                }
            }
            else
            {
                Debug.Log("Auto dictionary is null");
            }

            string tempManualKey = saveFileName + "_data.game";
            if (manualDictionary.TryGetValue(tempManualKey, out GameData tempManualGameData) && tempManualGameData != null)
            {
                newGameDatas.Add(tempManualGameData);
            }

            if (newGameDatas.Any())
            {
                if (_useEncryption)
                {
                    var decryptedDates = newGameDatas.Select(data => new
                    {
                        OriginalData = data,
                        DecryptedLastUpdated = EncryptDecryptDateTime(DateTime.FromBinary(data.lastUpdated))
                    });

                    newGameDatas = decryptedDates
                        .OrderByDescending(data => data.DecryptedLastUpdated)
                        .Select(data => data.OriginalData)
                        .ToList();
                }
                else
                {
                    newGameDatas = newGameDatas.OrderByDescending(data => DateTime.FromBinary(data.lastUpdated)).ToList();
                }

                tempOrderedGameDatas.Add(newGameDatas.First());
            }
        }

        List<GameData> OrderedGameDatas = tempOrderedGameDatas
            .Where(data => data != null)
            .OrderByDescending(data => DateTime.FromBinary(data.lastUpdated))
            .ToList();

        for (int i = 0; i < OrderedGameDatas.Count; i++)
        {
            Debug.Log($"Ordered based on time: {DateTime.FromBinary(OrderedGameDatas[i].lastUpdated)}");
        }

        return OrderedGameDatas;
    }

    #endregion

    #region Save

    public void ManualSave(GameData data, string saveFileName)
    {
        if (saveFileName == null)
        {
            Debug.Log($"Could not save doesnt have a save file name");
            return;
        }

        if (_isSaving)
        {
            Debug.Log("Could not save already saving");
            return;
        }

        _isSaving = true;

        string directoryPath = Path.Combine(_dataDirPath, saveFileName);
        string savePath = Path.Combine(_dataDirPath, saveFileName, "manual");
        string manualSavePath = Path.Combine(_dataDirPath, saveFileName, "manual", _saveDataName);

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.Log($"Creating new Save directory:\n {directoryPath}");
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                if (!Directory.Exists(savePath))
                {
                    Debug.Log($"Creating new manual directory: {savePath}");
                    Directory.CreateDirectory(Path.GetDirectoryName(manualSavePath));
                }
                else
                {
                    Debug.LogWarning($"{directoryPath}\nexists and will not create another directory");
                }
            }
            else
            {
                Debug.LogWarning($"{directoryPath}\nexists and will not create another directory");
            }

            data.saveFileName = saveFileName;
            data.saveType = "manual";
            data.saveDataName = _saveDataName;
            data.lastUpdated = DateTime.Now.ToBinary();

            if (ScreenShotManager.Instance != null)
            {
                int dotIndex = _saveDataName.LastIndexOf('.');
                string tempDataName = _saveDataName.Substring(0, dotIndex);

                ScreenShotManager.Instance.StartTakingScreenshot((screenShotData) =>
                {
                    string timeStamp = DateTime.FromBinary(data.lastUpdated).ToString("yyyy-MM-dd_HH-mm-ss");
                    string fileName = $"{tempDataName}_{timeStamp}.png";
                    string directoryPath = Path.Combine(Application.persistentDataPath, saveFileName, data.saveType);

                    try
                    {
                        string[] existingFiles = Directory.GetFiles(directoryPath, $"{tempDataName}_*.png");

                        foreach (string existingFile in existingFiles)
                        {
                            try
                            {
                                File.Delete(existingFile);
                                Debug.Log($"Deleted previous screenshot at: {existingFile}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Error deleting file {existingFile}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error trying to search for file: {e}");
                        return;
                    }

                    string filePath = Path.Combine(directoryPath, fileName);

                    File.WriteAllBytes(filePath, screenShotData);

                    Debug.Log($"New screenshot saved to: {filePath}");

                    ScreenShotManager.Instance.CreatedScreenShot();
                });
            }

            string dataToStore = JsonUtility.ToJson(data, true);

            if (_useEncryption)
            {
                dataToStore = EncryptDecrypt(dataToStore);
            }

            Debug.Log($"Creating file:\n{manualSavePath}");
            using (FileStream stream = new FileStream(manualSavePath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToStore);
                }
            }
            _isSaving = false;
        }
        catch (Exception e)
        {
            Debug.LogError("Error occured when trying to save data to file: " + manualSavePath + "\n" + e);
            _isSaving = false;
        }
    }

    public void AutoSave(GameData data, string saveFileName)
    {
        if (saveFileName == null)
        {
            Debug.Log($"No profile id");
            return;
        }

        if (_isSaving)
        {
            Debug.Log("Could not save already saving");
            return;
        }

        _isSaving = true;

        string directoryPath = Path.Combine(_dataDirPath, saveFileName, "auto");

        if (Directory.Exists(directoryPath))
        {
            Debug.Log($"Directory Exists:\n{directoryPath}");
            string filePath = Path.Combine(_dataDirPath, saveFileName, "auto");

            string[] files = Directory.GetFiles(filePath);

            // Filter out screenshot files from the count
            int fileCount = files.Count(file => !file.EndsWith(".png"));

            for (int i = 0; i < fileCount; i++)
            {
                _tempDataFileName = "data" + (i + 1) + ".game";
                string fullPath = Path.Combine(_dataDirPath, saveFileName, "auto", _tempDataFileName);
                if (!File.Exists(fullPath))
                {
                    Debug.Log($"Could not find save file:\n{fullPath}");
                }
                Debug.Log($"Found save file:\n{Path.GetFileName(files[i])}");
            }

            if (fileCount > _maxAutoSaves - 1)
            {
                List<GameData> gameDatas = GetAutoSaveData(saveFileName);

                List<GameData> orderedGameDatas = gameDatas.OrderByDescending(data => DateTime.FromBinary(data.lastUpdated)).ToList();

                int oldestIndex = gameDatas.IndexOf(orderedGameDatas.Last());

                _tempDataFileName = "data" + oldestIndex + ".game";

                for (int i = 0; i < orderedGameDatas.Count; i++)
                {
                    Debug.Log($"Index: {i} Time: {DateTime.FromBinary(orderedGameDatas[i].lastUpdated)}\nIndex: {i} Time: {DateTime.FromBinary(gameDatas[i].lastUpdated)}");
                }

                Debug.Log($"Replacing file {_tempDataFileName}");
            }

        }
        else
        {
            _tempDataFileName = "data" + 0 + ".game";
        }


        string autoSavePath = Path.Combine(_dataDirPath, saveFileName, "auto", _tempDataFileName);

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.Log($"Create new auto directory\n{directoryPath}");
                Directory.CreateDirectory(Path.GetDirectoryName(autoSavePath));
            }
            else
            {
                Debug.Log($"{directoryPath}\nexists and will not create another directory");
            }

            data.saveFileName = saveFileName;
            data.saveType = "auto";
            data.saveDataName = _tempDataFileName;
            data.lastUpdated = DateTime.Now.ToBinary();

            if (ScreenShotManager.Instance != null)
            {
                int dotIndex = _tempDataFileName.LastIndexOf('.');
                string tempDataName = _tempDataFileName.Substring(0, dotIndex);

                ScreenShotManager.Instance.StartTakingScreenshot((screenShotData) =>
                {
                    string timeStamp = DateTime.FromBinary(data.lastUpdated).ToString("yyyy-MM-dd_HH-mm-ss");
                    string fileName = $"{tempDataName}_{timeStamp}.png";
                    string directoryPath = Path.Combine(Application.persistentDataPath, saveFileName, data.saveType);

                    try
                    {
                        string[] existingFiles = Directory.GetFiles(directoryPath, $"{tempDataName}_*.png");

                        foreach (string existingFile in existingFiles)
                        {
                            try
                            {
                                File.Delete(existingFile);
                                Debug.Log($"Deleted previous screenshot at: {existingFile}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Error deleting file {existingFile}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error trying to search for file: {e}");
                        return;
                    }

                    string filePath = Path.Combine(directoryPath, fileName);

                    File.WriteAllBytes(filePath, screenShotData);

                    Debug.Log($"New screenshot saved to: {filePath}");
                    ScreenShotManager.Instance.CreatedScreenShot();
                });
            }

            string dataToStore = JsonUtility.ToJson(data, true);

            if (_useEncryption)
            {
                dataToStore = EncryptDecrypt(dataToStore);
            }

            Debug.Log($"Creating file:\n{autoSavePath}");
            using (FileStream stream = new FileStream(autoSavePath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToStore);
                }
            }
            _isSaving = false;
        }
        catch (Exception e)
        {
            Debug.LogError("Error occured when trying to save data to file: " + autoSavePath + "\n" + e);
            _isSaving = false;
        }

        Debug.Log($"Oldest index: {_tempDataFileName}");
    }

    #endregion

    #region Load

    public GameData LoadSaveFile(string saveFileName, string saveDataName)
    {
        string autoPath = Path.Combine(_dataDirPath, saveFileName, "auto", saveDataName);
        string manualPath = Path.Combine(_dataDirPath, saveFileName, "manual", saveDataName);

        GameData loadedData = new();

        if (File.Exists(autoPath))
        {
            try
            {
                string dataToLoad = "";
                using (FileStream stream = new FileStream(autoPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                if (_useEncryption)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
                // Debug.Log($"Loaded save file:\n{autoPath}");
            }
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to load data from file: " + autoPath + "\n" + e);
            }

            return loadedData;
        }

        if (File.Exists(manualPath))
        {
            try
            {
                string dataToLoad = "";
                using (FileStream stream = new FileStream(manualPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                if (_useEncryption)
                {
                    dataToLoad = EncryptDecrypt(dataToLoad);
                }

                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
                // Debug.Log($"Loaded save file:\n{autoPath}");
            }
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to load data from file: " + autoPath + "\n" + e);
            }
            return loadedData;
        }

        Debug.Log($"Could not find profile: {saveFileName} file: {saveDataName}\n{autoPath}");
        return null;
    }

    #endregion

    private string EncryptDecrypt(string data)
    {
        string modifiedData = "";
        for (int i = 0; i < data.Length; i++)
        {
            modifiedData += (char)(data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);
        }
        return modifiedData;
    }

    private string EncryptDecryptDateTime(DateTime dateTime)
    {
        // Encrypt the DateTime into a string
        string encryptedDateTime = "";
        long ticks = dateTime.Ticks;
        for (int i = 0; i < ticks.ToString().Length; i++)
        {
            encryptedDateTime += (char)(ticks.ToString()[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);
        }

        // Decrypt the string back into a string
        string decryptedDateTime = "";
        for (int i = 0; i < encryptedDateTime.Length; i++)
        {
            decryptedDateTime += (char)(encryptedDateTime[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);
        }

        return decryptedDateTime;
    }
}
