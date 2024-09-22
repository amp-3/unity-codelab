using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class OptimizeDistanceRendererObject : MonoBehaviour, IDistanceRequester
{
    private const float DISTANCE = 10f;
    private const float SQR_DISTANCE = DISTANCE * DISTANCE;

    [SerializeField]
    private Renderer[] rendererArray = null;

    private Transform _transform = null;

    private void Start()
    {
        this._transform = transform;
        DistanceBurstCompilerManager.Instance.RegistDistanceRequester(this);

        rendererArray = GetComponentsInChildren<Renderer>();
    }

    #region IDistanceRequester
    public void GetCalcDistanceRequestData(out CalcDistanceRequestData calcDistanceRequestData)
    {
        Vector3 position = _transform.position;

        calcDistanceRequestData = new CalcDistanceRequestData(
            position,
            SystemPositionType.Camera
        );
    }

    public void ReturnSqrDistance(float sqrDistance)
    {
        bool isVisible = sqrDistance <= SQR_DISTANCE;

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
    }
    #endregion

    private void OnDestroy()
    {
        DistanceBurstCompilerManager.Instance.UnregistDistanceRequester(this);
    }
}