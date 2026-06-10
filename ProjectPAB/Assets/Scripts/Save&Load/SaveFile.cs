using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;

public class SaveFile : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] string _saveFileName = "";
    public string SaveFileName
    {
        get { return _saveFileName; }
        set { _saveFileName = value; }
    }

    [SerializeField] string _saveDataName = "";
    public string SaveDataName
    {
        get { return _saveDataName; }
    }

    [SerializeField] string _saveType = "";
    public string SaveType
    {
        get { return _saveType; }
    }

    [Header("Content")]
    #region Content

    [SerializeField] Button _saveFileButton;
    public Button SaveFileButton
    {
        get { return _saveFileButton; }
    }
    [SerializeField] Button _deleteFileButton;
    public Button DeleteFileButton
    {
        get { return _deleteFileButton; }
    }

    [SerializeField] Image _saveFileImage;
    [SerializeField] TMP_Text _saveFileNameTxt;
    [SerializeField] TMP_Text _saveFileLastPlayedTxt;
    [SerializeField] TMP_Text _saveFileAreaTxt;

    #endregion

    void Awake()
    {
        TryChangeSaveImage();
    }

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

            _saveFileImage.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
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
            _saveFileNameTxt.text = "Save file name A";
            _saveFileLastPlayedTxt.text = "Last played: A";
        }
        else
        {
            _saveFileName = data.saveFileName;
            _saveDataName = data.saveDataName;
            _saveType = data.saveType;

            // _saveFileNameTxt.text = _saveFileName + "_" + _saveDataName;
            _saveFileNameTxt.text = _saveFileName;
            _saveFileLastPlayedTxt.text = DateTime.FromBinary(data.lastUpdated).ToString();
        }
    }
}
