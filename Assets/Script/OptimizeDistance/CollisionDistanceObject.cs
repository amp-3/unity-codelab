using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;

public class CollisionDistanceObject : MonoBehaviour
{
    private const float DISTANCE = 10f;
    private const float SQR_DISTANCE = DISTANCE * DISTANCE;

    [SerializeField]
    private OnCollisionNotifierComponent onCollisionNotifierComponent = null;

    private IDisposable disposable = null;

    private void Start()
    {
        disposable = onCollisionNotifierComponent.onCollisionEnterObservable
            .Subscribe(collision =>
            {
                Camera _camera = collision.gameObject.GetComponent<Camera>();

                bool isVisible = _camera != null;

                if (gameObject.activeSelf != isVisible) gameObject.SetActive(isVisible);
            });
    }

    private void OnDestroy()
    {
        disposable.Dispose();
    }
}