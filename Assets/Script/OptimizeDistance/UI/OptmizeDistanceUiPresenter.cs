using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class OptmizeDistanceUiPresenter : MonoBehaviour
{
    [SerializeField]
    private DistanceTestObjectFactory distanceTestObjectFactory = null;

    [SerializeField]
    private TMP_InputField createCount_GUIInputField = null;
    [SerializeField]
    private Button[] createObjectButtonArray = null;

    private CompositeDisposable disposables = new CompositeDisposable();

    void Start()
    {
        for (int i = 0; i < createObjectButtonArray.Length; i++)
        {
            int index = i;

            createObjectButtonArray[i].GetComponentInChildren<TMP_Text>().text = distanceTestObjectFactory.GetObjectInfo(index);

            createObjectButtonArray[i].OnClickAsObservable()
                .Subscribe(_ =>
                {
                    distanceTestObjectFactory.CreateGameObject(index);
                }).AddTo(disposables);
        }

        createCount_GUIInputField.onValueChanged.AddListener(s =>
        {
            ApplyCreateCount();
        });

        ApplyCreateCount();
    }

    private void ApplyCreateCount()
    {
        int.TryParse(createCount_GUIInputField.text, out int createCount);

        distanceTestObjectFactory.SetCreateCount(createCount);
    }

    private void OnDestroy()
    {
        disposables.Dispose();
    }
}
