using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class DistanceTestObjectFactory : MonoBehaviour
{
    [SerializeField]
    private int createCount = 10000;
    [SerializeField]
    private float putPositionRnadomRange = 5f;

    [SerializeField]
    private GameObject[] prefabArray = null;

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

    private void AllDestroy()
    {
        foreach (GameObject go in gameObjectList)
        {
            Destroy(go);
        }

        gameObjectList.Clear();
    }
}