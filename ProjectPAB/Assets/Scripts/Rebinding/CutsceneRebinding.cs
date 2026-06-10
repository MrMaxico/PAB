using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public class CutsceneRebinding : MonoBehaviour
{

    private Keyboard _keyboard;
    private KeyboardState _keyboardState;

    [SerializeField] List<Keybind> _cutsceneActions;

    [SerializeField] TMP_Text _uiRebindTutorialTxt;

    [SerializeField] bool _completedRebind;
    [SerializeField] bool _isRebinding;
    [SerializeField] bool _startedRebind;

    [SerializeField] int _bindingIndex;

    [SerializeField] string _lastPressedKey;
    [SerializeField] string _currentKeyboardInputIndex;

    [SerializeField] string _currentPressedKey;
    [SerializeField] string _previousPressedKey;


    [SerializeField] bool _pressingBindedKey;
    [SerializeField] List<string> _bindedKeys = new(); // CHECK FOR PREVIOUS BINDINGS PLS THANKS

    [SerializeField] GameObject[] _movementPoints;

    [Serializable]
    public struct Keybind
    {
        public InputActionReference inputActionReference;
        public int actionIndex;
        public bool excludeMouse;
        public string tutorialtext;

        public Keybind(InputActionReference inputActionReference, int actionIndex, bool excludeMouse, string tutorialtext)
        {
            this.inputActionReference = inputActionReference;
            this.actionIndex = actionIndex;
            this.excludeMouse = excludeMouse;
            this.tutorialtext = tutorialtext;
        }
    }

    private void Awake()
    {
        _keyboard = InputSystem.GetDevice<Keyboard>();
        _keyboardState = new KeyboardState();

        for (int i = 0; i < _movementPoints.Length; i++)
        {
            _movementPoints[i].SetActive(false);
            _movementPoints[i].GetComponentInChildren<CutsceneMovementPoint>().CutsceneRebinding = this;
        }
        _movementPoints[0].SetActive(true);
    }

    private void OnEnable()
    {
        KeyRebinding.rebindComplete += OnCompleteRebind;
        KeyRebinding.rebindStarted += StartRebind;
        KeyRebinding.rebindCanceled += OnCancelRebind;
    }

    private void OnDisable()
    {
        KeyRebinding.rebindComplete -= OnCompleteRebind;
        KeyRebinding.rebindStarted -= StartRebind;
        KeyRebinding.rebindCanceled -= OnCancelRebind;
    }

    private void StartRebind(InputAction action, int arg2)
    {
        _startedRebind = true;
    }

    void OnCompleteRebind()
    {
        Debug.Log($"Rebind has been completed with binding: ( {KeyRebinding.GetBindingName(_cutsceneActions[_bindingIndex].inputActionReference, _cutsceneActions[_bindingIndex].actionIndex)} )");

        Debug.Log($"composite: ({KeyRebinding.GetBindingName(_cutsceneActions[0].inputActionReference, 0)})");

        _completedRebind = true;

        _startedRebind = false;

        _isRebinding = false;

        Debug.Log($"_currentKeyboardInputIndex = {GetKeyboardItem1FromItem2(KeyRebinding.GetBindingName(_cutsceneActions[_bindingIndex].inputActionReference, _cutsceneActions[_bindingIndex].actionIndex))}");
        _currentKeyboardInputIndex = GetKeyboardItem1FromItem2(KeyRebinding.GetBindingName(_cutsceneActions[_bindingIndex].inputActionReference, _cutsceneActions[_bindingIndex].actionIndex));
    }

    void OnCancelRebind()
    {
        Debug.Log("TEST");
        _isRebinding = false;
        _startedRebind = false;
        StartNewRebind(_bindingIndex);
    }

    private void Start()
    {
        StartNewRebind(0);

        _uiRebindTutorialTxt.text = _cutsceneActions[0].tutorialtext;
        // KeyRebinding.LoadBindingOverride("Move"); // TO LOAD 
    }

    private List<(string, string, KeyCode)> stringPairs = new List<(string, string, KeyCode)>
    {
        ("None", "", KeyCode.None),
        ("Space", "Space", KeyCode.Space),
        ("Enter", "Enter", KeyCode.Return),
        ("Tab", "Tab", KeyCode.Tab),
        ("Backquote", "Grave", KeyCode.BackQuote),
        ("Backquote", "``", KeyCode.BackQuote),
        ("Quote", "'", KeyCode.Quote),
        ("Quote", "Acute", KeyCode.Quote),
        ("Semicolon", ";", KeyCode.Semicolon),
        ("Comma", ",", KeyCode.Comma),
        ("Period", ".", KeyCode.Period),
        ("Slash", "/", KeyCode.Slash),
        ("Backslash", "\\", KeyCode.Backslash),
        ("LeftBracket", "[", KeyCode.LeftBracket),
        ("RightBracket", "]", KeyCode.RightBracket),
        ("Minus", "-", KeyCode.Minus),
        ("Equals", "=", KeyCode.Equals),
        ("A", "A", KeyCode.A),
        ("B", "B", KeyCode.B),
        ("C", "C", KeyCode.C),
        ("D", "D", KeyCode.D),
        ("E", "E", KeyCode.E),
        ("F", "F", KeyCode.F),
        ("G", "G", KeyCode.G),
        ("H", "H", KeyCode.H),
        ("I", "I", KeyCode.I),
        ("J", "J", KeyCode.J),
        ("K", "K", KeyCode.K),
        ("L", "L", KeyCode.L),
        ("M", "M", KeyCode.M),
        ("N", "N", KeyCode.N),
        ("O", "O", KeyCode.O),
        ("P", "P", KeyCode.P),
        ("Q", "Q", KeyCode.Q),
        ("R", "R", KeyCode.R),
        ("S", "S", KeyCode.S),
        ("T", "T", KeyCode.T),
        ("U", "U", KeyCode.U),
        ("V", "V", KeyCode.V),
        ("W", "W", KeyCode.W),
        ("X", "X", KeyCode.X),
        ("Y", "Y", KeyCode.Y),
        ("Z", "Z", KeyCode.Z),
        ("Digit1", "1", KeyCode.Alpha1),
        ("Digit2", "2", KeyCode.Alpha2),
        ("Digit3", "3", KeyCode.Alpha3),
        ("Digit4", "4", KeyCode.Alpha4),
        ("Digit5", "5", KeyCode.Alpha5),
        ("Digit6", "6", KeyCode.Alpha6),
        ("Digit7", "7", KeyCode.Alpha7),
        ("Digit8", "8", KeyCode.Alpha8),
        ("Digit9", "9", KeyCode.Alpha9),
        ("Digit0", "0", KeyCode.Alpha0),
        ("LeftShift", "Shift", KeyCode.LeftShift),
        ("RightShift", "Right Shift", KeyCode.RightShift),
        ("LeftAlt", "Alt", KeyCode.LeftAlt),
        ("RightAlt", "Right Alt", KeyCode.RightAlt),
        ("AltGr", "Right Alt", KeyCode.AltGr),
        ("LeftCtrl", "Ctrl", KeyCode.LeftControl),
        ("LeftWindows", "Left Windows", KeyCode.LeftWindows),
        ("RightWindows", "Right Windows", KeyCode.RightWindows),
        ("RightCtrl", "Right Ctrl", KeyCode.RightControl),
        ("LeftMeta", "Left Windows", KeyCode.LeftCommand), // macOS Command key
        ("RightMeta", "Right Windows", KeyCode.RightCommand), // macOS Command key
        ("LeftApple", "Left Windows", KeyCode.LeftCommand), // macOS Command key
        ("RightApple", "Right Windows", KeyCode.RightCommand), // macOS Command key
        ("LeftCommand", "Left Command", KeyCode.LeftCommand), // macOS Command key
        ("RightCommand", "Right Command", KeyCode.RightCommand), // macOS Command key
        ("ContextMenu", "Application", KeyCode.Menu),
        ("Escape", "Escape", KeyCode.Escape),
        ("LeftArrow", "Left", KeyCode.LeftArrow),
        ("RightArrow", "Right", KeyCode.RightArrow),
        ("UpArrow", "Up", KeyCode.UpArrow),
        ("DownArrow", "Down", KeyCode.DownArrow),
        ("Backspace", "Backspace", KeyCode.Backspace),
        ("PageDown", "Pgdown", KeyCode.PageDown),
        ("PageUp", "Pgup", KeyCode.PageUp),
        ("Home", "Home", KeyCode.Home),
        ("End", "End", KeyCode.End),
        ("Insert", "Insert", KeyCode.Insert),
        ("Delete", "Delete", KeyCode.Delete),
        ("CapsLock", "Caps Lock", KeyCode.CapsLock),
        ("NumLock", "Num Lock", KeyCode.Numlock),
        ("PrintScreen", "Prnt Scrn", KeyCode.Print),
        ("ScrollLock", "Scroll Lock", KeyCode.ScrollLock),
        ("Pause", "Break", KeyCode.Pause),
        ("NumpadEnter", "Num Enter", KeyCode.KeypadEnter),
        ("NumpadDivide", "Num /", KeyCode.KeypadDivide),
        ("NumpadMultiply", "*", KeyCode.KeypadMultiply),
        ("NumpadPlus", "+", KeyCode.KeypadPlus),
        ("NumpadMinus", "-", KeyCode.KeypadMinus),
        ("NumpadPeriod", "Num Decimal", KeyCode.KeypadPeriod),
        ("NumpadEquals", "", KeyCode.KeypadEquals),
        ("Numpad0", "Num 0", KeyCode.Keypad0),
        ("Numpad1", "Num 1", KeyCode.Keypad1),
        ("Numpad2", "Num 2", KeyCode.Keypad2),
        ("Numpad3", "Num 3", KeyCode.Keypad3),
        ("Numpad4", "Num 4", KeyCode.Keypad4),
        ("Numpad5", "Num 5", KeyCode.Keypad5),
        ("Numpad6", "Num 6", KeyCode.Keypad6),
        ("Numpad7", "Num 7", KeyCode.Keypad7),
        ("Numpad8", "Num 8", KeyCode.Keypad8),
        ("Numpad9", "Num 9", KeyCode.Keypad9),
        ("F1", "F1", KeyCode.F1),
        ("F2", "F2", KeyCode.F2),
        ("F3", "F3", KeyCode.F3),
        ("F4", "F4", KeyCode.F4),
        ("F5", "F5", KeyCode.F5),
        ("F6", "F6", KeyCode.F6),
        ("F7", "F7", KeyCode.F7),
        ("F8", "F8", KeyCode.F8),
        ("F9", "F9", KeyCode.F9),
        ("F10", "F10", KeyCode.F10),
        ("F11", "F11", KeyCode.F11),
        ("F12", "F12", KeyCode.F12),
        ("OEM1", "", KeyCode.None), // OEM keys vary and need specific context
        ("OEM2", "", KeyCode.None), // OEM keys vary and need specific context
        ("OEM3", "", KeyCode.None), // OEM keys vary and need specific context
        ("OEM4", "", KeyCode.None), // OEM keys vary and need specific context
        ("OEM5", "", KeyCode.None), // OEM keys vary and need specific context
        ("IMESelected", "", KeyCode.None) // IME selection is context-specific
    };

    private string GetKeyboardItem1FromItem2(string possibleItem2)
    {
        foreach (var pair in stringPairs)
        {
            if (pair.Item2 == possibleItem2)
            {
                return pair.Item1;
            }
        }
        return null;
    }

    // private KeyCode GetKeyboardItem3FromItem2(string possibleItem2)
    // {
    //     foreach (var pair in stringPairs)
    //     {
    //         if (pair.Item2 == possibleItem2)
    //         {
    //             Debug.Log($"{pair.Item1} == {possibleItem2}");
    //             return pair.Item3;
    //         }
    //     }
    //     return KeyCode.None;
    // }

    private void Update()
    {

        if (Input.anyKey && _completedRebind)
        {
            // THIS NEEDS TO USE THE NEW ONES SO IT WORKS WITH THAT
            // THIS NEEDS TO USE THE NEW ONES SO IT WORKS WITH THAT

            // if (!_newInput)
            // {
            foreach (var pair in stringPairs)
            {
                if (Input.GetKey(pair.Item3))
                {
                    for (int i = 0; i < _bindedKeys.Count; i++)
                    {
                        if (_bindedKeys[i] == pair.Item2)
                        {
                            _pressingBindedKey = true;
                            Debug.Log("RETURN");
                            return;
                        }
                    }

                    _pressingBindedKey = false;

                    InputSystem.QueueStateEvent(_keyboard, _keyboardState);
                    _keyboardState.Press((Key)System.Enum.Parse(typeof(Key), _currentKeyboardInputIndex));


                    if (_currentPressedKey == "")
                    {
                        _currentPressedKey = pair.Item2;
                        _previousPressedKey = pair.Item2;
                        Debug.Log("FIRST TIME KEY PRESSED");
                    }
                    if (_previousPressedKey != pair.Item2)
                    {
                        Debug.Log($"NEW INPUT {pair.Item2}");

                        _previousPressedKey = _currentPressedKey;
                        _currentPressedKey = pair.Item2;

                        _keyboardState.Release((Key)System.Enum.Parse(typeof(Key), _currentKeyboardInputIndex));
                        InputSystem.QueueStateEvent(_keyboard, _keyboardState);

                        InputSystem.QueueStateEvent(_keyboard, _keyboardState);
                        _keyboardState.Press((Key)System.Enum.Parse(typeof(Key), pair.Item1));

                        StartNewRebind(_bindingIndex);

                        break; // Currently for not checking more needs a bigger solution later
                    }
                    _currentPressedKey = pair.Item2;
                }
                // }
            }
        }
    }

    void StartNewRebind(int index)
    {
        if (index >= _cutsceneActions.Count)
        {
            Debug.LogError("OUT OF RANGE");
            SceneManager.LoadScene("MainMenu");
            return;
        }
        if (_pressingBindedKey)
        {
            Debug.LogWarning("This key is already binded");
            return;
        }

        if (!_isRebinding && !_startedRebind)
        {
            _isRebinding = true;
            _completedRebind = false;
            Debug.Log("REBIND");

            KeyRebinding.StartRebind(_cutsceneActions[index].inputActionReference, _cutsceneActions[index].excludeMouse, _cutsceneActions[index].actionIndex);
        }
    }

    public void SaveNewRebind()
    {
        Debug.Log("SAVE BINDING");

        KeyRebinding.SaveBindingOverride(_cutsceneActions[_bindingIndex].inputActionReference.action);
        _completedRebind = false;
        Debug.Log($"composite: ({KeyRebinding.GetBindingName(_cutsceneActions[0].inputActionReference, 0)})");

        _bindedKeys.Add(KeyRebinding.GetBindingName(_cutsceneActions[_bindingIndex].inputActionReference, _cutsceneActions[_bindingIndex].actionIndex));


        if (_bindingIndex < _movementPoints.Length)
        {
            _movementPoints[_bindingIndex].SetActive(false);
            _bindingIndex++;
            if (_bindingIndex < _movementPoints.Length)
            {
                _movementPoints[_bindingIndex].SetActive(true);
            }
        }
        else
        {
            _bindingIndex++;
        }


        if (_cutsceneActions.Count > _bindingIndex)
        {
            _uiRebindTutorialTxt.text = _cutsceneActions[_bindingIndex].tutorialtext; // Fade in logix or somtehing
            // Debug.LogError("SHOULD STOP OR SOMETHING"); 
        }
        else
        {
            _uiRebindTutorialTxt.text = "DONE :)";
        }

        _keyboardState.Release((Key)System.Enum.Parse(typeof(Key), _currentKeyboardInputIndex));
        InputSystem.QueueStateEvent(_keyboard, _keyboardState);

        KeyRebinding.AddBindedInput(GetKeyboardItem1FromItem2(_currentPressedKey), 0);

        _currentKeyboardInputIndex = "";
        _currentPressedKey = "";
        _previousPressedKey = "";

        StartNewRebind(_bindingIndex);
    }
}