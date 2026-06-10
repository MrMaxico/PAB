
using UnityEngine;

[System.Serializable]
public class GameData
{
    public long lastUpdated;

    public string saveFileName;

    public string saveDataName;

    public string saveType;

    public Vector3 cubePos;

    public GameData()
    {
        cubePos = Vector3.zero;
    }
}
