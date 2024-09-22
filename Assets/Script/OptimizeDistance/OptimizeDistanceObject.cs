using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class OptimizeDistanceObject : MonoBehaviour, IDistanceRequester
{
    private const float DISTANCE = 10f;
    private const float SQR_DISTANCE = DISTANCE * DISTANCE;

    private void Start()
    {
        DistanceBurstCompilerManager.Instance.RegistDistanceRequester(this);
    }

    #region IDistanceRequester
    public void GetCalcDistanceRequestData(out CalcDistanceRequestData calcDistanceRequestData)
    {
        //Vector3 position = transform.position;
        //Vector3 cameraPosition = CameraSingleton.Instance.transform.position;

        //return new CalcDistanceRequestData(
        //    position,
        //    cameraPosition
        //);


        Vector3 position = transform.position;

        calcDistanceRequestData = new CalcDistanceRequestData(
            position,
            SystemPositionType.Camera
        );
    }


    public void ReturnSqrDistance(float sqrDistance)
    {
        bool isVisible = sqrDistance <= SQR_DISTANCE;

        if (gameObject.activeSelf != isVisible) gameObject.SetActive(isVisible);
    }
    #endregion

    private void OnDestroy()
    {
        DistanceBurstCompilerManager.Instance.UnregistDistanceRequester(this);
    }
}