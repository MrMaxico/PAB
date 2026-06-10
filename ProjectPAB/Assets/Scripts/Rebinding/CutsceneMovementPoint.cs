using UnityEngine;

public class CutsceneMovementPoint : MonoBehaviour
{
    [SerializeField] CutsceneRebinding _cutsceneRebinding;
    public CutsceneRebinding CutsceneRebinding
    { set { _cutsceneRebinding = value; } }

    [SerializeField] bool _canStartTimer;

    [SerializeField] float _maxSaveTime;
    [SerializeField] float _timeToSave;

    [SerializeField] Material _material;
    [SerializeField] Renderer _renderer;

    private void Start()
    {
        color = _renderer.material.color;
        _material = _renderer.material;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _canStartTimer = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _canStartTimer = false;
        }
    }

    Color color;

    private void Update()
    {
        color = _material.color;
        color.a = _timeToSave / _maxSaveTime;
        _material.color = color;

        if (_canStartTimer && _timeToSave > 0)
        {
            _timeToSave -= Time.deltaTime;
        }
        else if (_timeToSave < _maxSaveTime && !_canStartTimer)
        {
            _timeToSave += Time.deltaTime;
        }
        else if (_timeToSave > _maxSaveTime && !_canStartTimer)
        {
            _timeToSave = _maxSaveTime;
        }
        else if (_timeToSave <= 0)
        {
            _timeToSave = 0;
            _cutsceneRebinding.SaveNewRebind();
        }
    }
}