using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class DistanceTestObjectFactory : MonoBehaviour
{
    [SerializeField]
    private float putPositionRnadomRange = 5f;

    [SerializeField]
    private GameObject[] prefabArray = null;

    private int createCount = 1;

    private List<GameObject> gameObjectList = new List<GameObject>();

    private void Start()
    {
        Application.targetFrameRate = 0;
    }

    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.Z))
        {
            CreateGameObject(prefabArray[0], createCount);
        }
        else if (UnityEngine.Input.GetKeyDown(KeyCode.X))
        {
            CreateGameObject(prefabArray[1], createCount);
        }
        else if (UnityEngine.Input.GetKeyDown(KeyCode.C))
        {
            CreateGameObject(prefabArray[2], createCount);
        }
        else if (UnityEngine.Input.GetKeyDown(KeyCode.V))
        {
            CreateGameObject(prefabArray[3], createCount);
        }
        else if (UnityEngine.Input.GetKeyDown(KeyCode.B))
        {
            CreateGameObject(prefabArray[4], createCount);
        }
    }

    public void CreateGameObject(int index)
    {
        CreateGameObject(prefabArray[index], createCount);
    }

    public string GetObjectInfo(int index)
    {
        return prefabArray[index].name;
    }

    private void CreateGameObject(GameObject prefab, int count)
    {
        AllDestroy();

        for (int i = 0; i < count; i++)
        {
            Vector3 position = new Vector3(
                UnityEngine.Random.Range(-putPositionRnadomRange, putPositionRnadomRange),
                UnityEngine.Random.Range(-putPositionRnadomRange, putPositionRnadomRange),
                UnityEngine.Random.Range(-putPositionRnadomRange, putPositionRnadomRange));

            gameObjectList.Add(Instantiate(prefab, position, Quaternion.identity, null));
        }
    }

    public int GetCreateCount()
    {
        return createCount;
    }

    public void SetCreateCount(int count)
    {
        this.createCount = count;
    }

    private void AllDestroy()
    {
        foreach (GameObject go in gameObjectList)
        {
            Destroy(go);
        }

        gameObjectList.Clear();
    }
}