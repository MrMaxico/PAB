using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCube : MonoBehaviour, IDataPersistence
{

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            transform.position += new Vector3(-0.5f, 0, 0);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            transform.position += new Vector3(0.5f, 0, 0);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            transform.position += new Vector3(0, 0.5f, 0);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            transform.position += new Vector3(0, -0.5f, 0);
        }

    }

    public void LoadData(GameData data)
    {
        transform.position = data.cubePos;
    }

    public void SaveData(GameData data)
    {
        Debug.Log($"{data.saveDataName}_{data.saveFileName}");
        data.cubePos = transform.position;
    }
}
