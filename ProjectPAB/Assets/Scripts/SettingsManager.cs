using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Options")]
    #region Options

    [Header("Video")]
    #region Video



    #endregion

    [Header("Accessibility")]
    #region Accessibility

    #endregion

    [Header("Extra")]
    #region Extra

    [Tooltip("Determines if the camera will fly from the save spot towards the AI-controlled player at startup.")]
    [SerializeField] bool _cinematicMode;
    [SerializeField] Button _cinematicModeBtn;

    [Tooltip("Determines if the character is AI-controlled at startup. Becomes player-controlled once input is detected.")]
    [SerializeField] bool _hasFreeWill;
    [SerializeField] Button _hasFreeWillBtn;

    #endregion

    #endregion

    private void OnValidate()
    {
        if (!_hasFreeWill && _cinematicMode)
        {
            _cinematicMode = false;

            if (_cinematicModeBtn != null)
            {
                _cinematicModeBtn.interactable = false;
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Another instance of SettingsManager already exists. Deleting this (new) instance.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }
}
