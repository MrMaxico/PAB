using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestInput : MonoBehaviour
{

    [SerializeField] TMP_Text testTxt;
    [SerializeField] bool _keyDown;

    public static TestInput instance;

    [SerializeField] PlayerInput playerInput;

    public string _buttonName;

    [SerializeField] bool _rebinding;
    [SerializeField] float _rebindTime;
    [SerializeField] float _timeToRebind;

    private void Awake()
    {
        instance = this;
    }


    private void OnEnable()
    {
        playerInput.actions.FindAction("Test").canceled += OnTest;
        KeyRebinding.rebindComplete += OnComplete;
    }

    private void OnComplete()
    {
        _rebinding = true;
    }

    private void OnDisable()
    {
        playerInput.actions.FindAction("Test").canceled -= OnTest;
    }

    void OnTest(InputAction.CallbackContext context)
    {
        Debug.Log("Cancelling (test)");
    }

    private void Update()
    {

        if (_keyDown)
        {
            testTxt.text = "Yes";
        }
        else
        {
            testTxt.text = "No";
        }
        // if (_rebindTime < 0)
        // {
        //     Debug.Log($"Rebinded succesfully");
        //     _rebindTime = _timeToRebind;
        //     _rebinding = false;
        // }

        // does work with controller (switch pro controller sucks doo doo fart)
        if (Input.anyKey)
        {
            Debug.Log("Key Down");
            _keyDown = true;
        }
        else
        {
            _keyDown = false;
        }

        // For some reason this one does not work
        // if (keyboard != null)
        // {
        //     foreach (var key in keyboard.allKeys)
        //     {
        //         if (key.IsPressed())
        //         {
        //             Debug.Log("keys are being pressed");
        //             if (_rebinding)
        //             {
        //                 _rebindTime -= Time.deltaTime;
        //             }
        //             break;
        //         }
        //         else
        //         {
        //             _rebindTime = _timeToRebind;
        //         }
        //     }
        // }

        // this one does not need to work but it does

        // if (mouse != null)
        // {
        //     for (int i = 0; i < 5; i++) // Iterate over standard mouse buttons
        //     {
        //         if (Input.GetMouseButtonUp(i))
        //         {
        //             Debug.Log("Mouse button " + i + " is being pressed");
        //             if (_rebinding)
        //             {
        //                 _rebindTime -= Time.deltaTime;
        //             }
        //         }
        //         else
        //         {
        //             _rebindTime = _timeToRebind;
        //         }
        //     }
        // }

        // for some reason this one works

    }
}
