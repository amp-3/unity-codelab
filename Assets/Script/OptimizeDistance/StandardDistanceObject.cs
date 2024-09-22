using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;

public class StandardDistanceObject : MonoBehaviour
{
    private void Start()
    {
        Observable.EveryGameObjectUpdate()
            .Subscribe(_ =>
            {
                Vector3 cameraPosition = CameraSingleton.Instance.transform.position;
                Vector3 position = transform.position;

                bool isVisible = Vector3Util.Distance(position, cameraPosition) < 10f;

                if (gameObject.activeSelf != isVisible) gameObject.SetActive(isVisible);
            }).AddTo(this);
    }
}