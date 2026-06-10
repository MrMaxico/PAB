using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveData : MonoBehaviour
{
    [Header("Profile")]
    #region Profile

    [SerializeField] string _saveFileName = "";
    public string SaveFileName
    {
        get { return _saveFileName; }
        set { _saveFileName = value; }
    }

    [SerializeField] string _saveType = "";

    [SerializeField] string _saveDataName = "";
    public string SaveDataName
    {
        get { return _saveDataName; }
    }

    #endregion

    [Header("Content")]
    #region Content

    [SerializeField] Button _saveDataButton;
    public Button SaveDataButton
    {
        get { return _saveDataButton; }
    }

    [SerializeField] TMP_Text _saveDataLastPlayedTxt;
    [SerializeField] TMP_Text _saveTypeTxt;
    [SerializeField] Image _saveDataImage;

    #endregion

    private void OnEnable()
    {
        ScreenShotManager.Instance.CreatedScreenshot += TryChangeSaveImage;
    }

    private void OnDisable()
    {
        ScreenShotManager.Instance.CreatedScreenshot -= TryChangeSaveImage;
    }

    public void TryChangeSaveImage()
    {
        Debug.Log("TryChangeSaveImage");

        int dotIndex = _saveDataName.LastIndexOf('.');
        string tempDataName = _saveDataName.Substring(0, dotIndex);

        string screenshotPattern = $"{tempDataName}*.png";
        string[] files = Directory.GetFiles(Path.Combine(Application.persistentDataPath, _saveFileName, _saveType), screenshotPattern, SearchOption.AllDirectories);

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

            _saveDataImage.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogWarning("No screenshot found.");
        }

    }

    public void SetData(GameData data)
    {
        if (data == null)
        {
            _saveDataLastPlayedTxt.text = "Last played: A";
        }
        else
        {
            _saveFileName = data.saveFileName;
            _saveDataName = data.saveDataName;
            _saveType = data.saveType;

            _saveDataLastPlayedTxt.text = DateTime.FromBinary(data.lastUpdated).ToString();
            _saveTypeTxt.text = _saveType + " save";

            int dotIndex = _saveDataName.LastIndexOf('.');
            string tempDataName = _saveDataName.Substring(0, dotIndex);

            string screenshotPattern = $"{tempDataName}*.png";
            string[] files = Directory.GetFiles(Path.Combine(Application.persistentDataPath, _saveFileName, _saveType), screenshotPattern, SearchOption.AllDirectories);

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

                _saveDataImage.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogWarning("No screenshot found.");
            }
        }
    }
}
