using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;

public class StandardDistanceObject : MonoBehaviour
{
    private const float DISTANCE = 10f;
    private const float SQR_DISTANCE = DISTANCE * DISTANCE;

    private Transform _transform = null;

    private IDisposable disposable = null;

    private void Start()
    {
        this._transform = transform;

        disposable = Observable.EveryGameObjectUpdate()
            .Subscribe(_ =>
            {
                Vector3 cameraPosition = CameraSingleton.Instance.transform.position;
                Vector3 position = _transform.position;

                bool isVisible = Vector3Util.SqrDistance(position, cameraPosition) <= SQR_DISTANCE;

                if (gameObject.activeSelf != isVisible) gameObject.SetActive(isVisible);
            });
    }

    private void OnDestroy()
    {
        disposable.Dispose();
    }
}