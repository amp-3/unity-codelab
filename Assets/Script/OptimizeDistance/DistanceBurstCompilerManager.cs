using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public enum SystemPositionType : byte
{
    Manual,
    Camera
}

public readonly struct CalcDistanceRequestData
{
    public static readonly CalcDistanceRequestData DONT_REQUEST = new CalcDistanceRequestData(Unit.Default);

    // リクエストするかどうか。falseなら計算しない
    public readonly bool isRequest;

    public readonly Vector3 position1;
    public readonly Vector3 position2;

    public readonly SystemPositionType systemPositionType;

    /// <summary>
    /// 2座標とも指定するコンストラクタ
    /// </summary>
    /// <param name="position1"></param>
    /// <param name="position2"></param>
    public CalcDistanceRequestData(in Vector3 position1, in Vector3 position2)
    {
        this.isRequest = true;

        this.position1 = position1;
        this.position2 = position2;
        this.systemPositionType = SystemPositionType.Manual;
    }

    /// <summary>
    /// Position2はSystemPositionTypeに応じた座標を指定するコンストラクタ
    /// </summary>
    /// <param name="position1"></param>
    /// <param name="systemPositionType"></param>
    public CalcDistanceRequestData(in Vector3 position1, SystemPositionType systemPositionType)
    {
        this.isRequest = true;

        this.position1 = position1;
        this.position2 = Vector3.zero;
        this.systemPositionType = systemPositionType;
    }

    /// <summary>
    /// Requestを行わないコンストラクタ
    /// </summary>
    /// <param name="unit"></param>
    private CalcDistanceRequestData(Unit unit)
    {
        this.isRequest = false;

        this.position1 = Vector3.zero;
        this.position2 = Vector3.zero;
        this.systemPositionType = SystemPositionType.Manual;
    }
}

public interface IDistanceRequester
{
    /// <summary>
    /// 距離算出のリクエストデータを返却する
    /// </summary>
    /// <returns></returns>
    void GetCalcDistanceRequestData(out CalcDistanceRequestData calcDistanceRequestData);

    /// <summary>
    /// 計算結果を返却する
    /// </summary>
    /// <param name="sqrDistance"></param>
    void ReturnSqrDistance(float sqrDistance);
}

public interface IRegistDistanceRequester
{
    void RegistDistanceRequester(IDistanceRequester distanceRequester);

    void UnregistDistanceRequester(IDistanceRequester distanceRequester);
}

public class DistanceBurstCompilerManager : MonoBehaviour, IRegistDistanceRequester
{
    private static DistanceBurstCompilerManager instance = null;
    public static DistanceBurstCompilerManager Instance => instance;

    private List<IDistanceRequester> distanceRequesterList = new List<IDistanceRequester>();

    private IDisposable disposable = null;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    void Start()
    {
        disposable = Observable.EveryGameObjectUpdate()
            .Subscribe(_ =>
            {
                OnUpdate();
            });
    }

    private void OnUpdate()
    {
        int count = distanceRequesterList.Count;

        if (count == 0) return;
#if SW
        System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
        sw1.Start();
#endif
        NativeArray<float3> positions1Native = new NativeArray<float3>(count, Allocator.TempJob);
        NativeArray<float3> positions2Native = new NativeArray<float3>(count, Allocator.TempJob);
        NativeArray<float> distancesNative = new NativeArray<float>(count, Allocator.TempJob);
#if SW
        sw1.Stop();
#endif

        Vector3 cameraPosition = CameraSingleton.Instance.transform.position;

#if SW
        System.Diagnostics.Stopwatch swloop = new System.Diagnostics.Stopwatch();
        swloop.Start();
#endif
        //for (int i = 0; i < distanceRequesterList.Count; i++)
        //{
        //    distanceRequesterList[i].GetCalcDistanceRequestData(out CalcDistanceRequestData calcDistanceRequestData);
        //}
#if SW
        swloop.Stop();
#endif

#if SW
        System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
        sw2.Start();
#endif
        for (int i = 0; i < distanceRequesterList.Count; i++)
        {
            distanceRequesterList[i].GetCalcDistanceRequestData(out CalcDistanceRequestData calcDistanceRequestData);

            positions1Native[i] = calcDistanceRequestData.position1;

            switch (calcDistanceRequestData.systemPositionType)
            {
                case SystemPositionType.Manual:
                    positions2Native[i] = calcDistanceRequestData.position2;
                    break;
                case SystemPositionType.Camera:
                    positions2Native[i] = cameraPosition;
                    break;
            }
        }
#if SW
        sw2.Stop();
#endif

#if SW
        System.Diagnostics.Stopwatch sw3 = new System.Diagnostics.Stopwatch();
        sw3.Start();
#endif
        CalculateSqrDistancesJob job = new CalculateSqrDistancesJob
        {
            Positions1 = positions1Native,
            Positions2 = positions2Native,
            SqrDistances = distancesNative
        };
#if SW
        sw3.Stop();
#endif

#if SW
        System.Diagnostics.Stopwatch sw4 = new System.Diagnostics.Stopwatch();
        sw4.Start();
#endif
        JobHandle handle = job.Schedule(positions1Native.Length, 64);
        handle.Complete();

#if SW
        sw4.Stop();
#endif

#if SW
        System.Diagnostics.Stopwatch sw5 = new System.Diagnostics.Stopwatch();
        sw5.Start();
#endif
        for (int i = 0; i < distanceRequesterList.Count; i++)
        {
            distanceRequesterList[i].ReturnSqrDistance(distancesNative[i]);
        }
#if SW
        sw5.Stop();
#endif

#if SW
        Debug.Log("1: " + sw1.ElapsedMilliseconds + "  2: " + sw2.ElapsedMilliseconds + "  3: " + sw3.ElapsedMilliseconds + "  4: " + sw4.ElapsedMilliseconds + "  5: " + sw5.ElapsedMilliseconds + "  loop: " + swloop.ElapsedMilliseconds);
#endif

        if (positions1Native.IsCreated) positions1Native.Dispose();
        if (positions2Native.IsCreated) positions2Native.Dispose();
        if (distancesNative.IsCreated) distancesNative.Dispose();
    }

    #region IRegistDistanceRequester
    public void RegistDistanceRequester(IDistanceRequester distanceRequester)
    {
        if (distanceRequester == null) return;

        distanceRequesterList.Add(distanceRequester);
    }

    public void UnregistDistanceRequester(IDistanceRequester distanceRequester)
    {
        if (distanceRequester == null) return;

        distanceRequesterList.Remove(distanceRequester);
    }
    #endregion


    [BurstCompile]
    struct CalculateSqrDistancesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Positions1;
        [ReadOnly] public NativeArray<float3> Positions2;
        [WriteOnly] public NativeArray<float> SqrDistances;

        public void Execute(int index)
        {
            float3 position1 = Positions1[index];
            float3 position2 = Positions2[index];
            SqrDistances[index] = math.distancesq(position1, position2);
        }
    }
}
