using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CameraSingleton : MonoBehaviour
{
    public static Camera Instance = null;

    [SerializeField]
    private Camera _camera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = _camera;
        }
    }
}