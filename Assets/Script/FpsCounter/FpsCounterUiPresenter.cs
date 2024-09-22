using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;

public class FpsCounterUiPresenter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text fpsCounter_GUIText = null;

    private FpsCounter fpsCounter = new FpsCounter();
    private IDisposable disposable = null;

    private float latestViewFps = -1f;

    private void Start()
    {
        disposable = Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                fpsCounter.OnUpdate();

                float fps = fpsCounter.GetFps();

                if (fps != latestViewFps)
                {
                    fpsCounter_GUIText.text = fps.ToString("F1");

                    latestViewFps = fps;
                }
            });
    }
}
