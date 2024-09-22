using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;

public class StandardDistanceRendererObject : MonoBehaviour
{
    private const float DISTANCE = 10f;
    private const float SQR_DISTANCE = DISTANCE * DISTANCE;

    [SerializeField]
    private Renderer[] rendererArray = null;

    private IDisposable disposable = null;

    private void Start()
    {
        rendererArray = GetComponentsInChildren<Renderer>();

        disposable = Observable.EveryGameObjectUpdate()
            .Subscribe(_ =>
            {
                Vector3 cameraPosition = CameraSingleton.Instance.transform.position;
                Vector3 position = transform.position;

                bool isVisible = Vector3Util.SqrDistance(position, cameraPosition) <= SQR_DISTANCE;

                if (rendererArray != null)
                {
                    foreach (Renderer renderer in rendererArray)
                    {
                        if (renderer != null)
                        {
                            if (renderer.enabled != isVisible) renderer.enabled = isVisible;
                        }
                    }
                }
            });
    }

    private void OnDestroy()
    {
        disposable.Dispose();
    }
}