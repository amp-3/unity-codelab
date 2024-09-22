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
using UnityEngine.Jobs;

public enum PositionType : byte
{
    Manual,
    Transform,
    Camera
}

public readonly struct CalcDistanceRequestData
{
    public enum PositionField : byte
    {
        Position1,
        Position2
    }

    public static readonly CalcDistanceRequestData DONT_REQUEST = new CalcDistanceRequestData(Unit.Default);

    // リクエストするかどうか。falseなら計算しない
    public readonly bool isRequest;

    public readonly Vector3 position1;
    public readonly Vector3 position2;

    public readonly Transform tranfsorm1;
    public readonly Transform tranfsorm2;

    public readonly int transformInstanceId1;
    public readonly int transformInstanceId2;

    public readonly PositionType positionType1;
    public readonly PositionType positionType2;

    public CalcDistanceRequestData(Vector3 position1, Vector3 position2, int transformInstanceId1, int transformInstanceId2, PositionType positionType1, PositionType positionType2)
    {
        this.isRequest = true;

        this.position1 = position1;
        this.position2 = position2;
        this.tranfsorm1 = null;
        this.tranfsorm2 = null;
        this.transformInstanceId1 = transformInstanceId1;
        this.transformInstanceId2 = transformInstanceId2;
        this.positionType1 = positionType1;
        this.positionType2 = positionType2;
    }

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
        this.tranfsorm1 = null;
        this.tranfsorm2 = null;
        this.transformInstanceId1 = 0;
        this.transformInstanceId2 = 0;
        this.positionType1 = PositionType.Manual;
        this.positionType2 = PositionType.Manual;
    }

    /// <summary>
    /// Position2はSystemPositionTypeに応じた座標を指定するコンストラクタ
    /// </summary>
    /// <param name="position1"></param>
    /// <param name="positionType2"></param>
    public CalcDistanceRequestData(in Vector3 position1, PositionType positionType2)
    {
        this.isRequest = true;

        this.position1 = position1;
        this.position2 = Vector3.zero;
        this.tranfsorm1 = null;
        this.tranfsorm2 = null;
        this.transformInstanceId1 = 0;
        this.transformInstanceId2 = 0;
        this.positionType1 = PositionType.Manual;
        this.positionType2 = positionType2;
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
        this.tranfsorm1 = null;
        this.tranfsorm2 = null;
        this.transformInstanceId1 = 0;
        this.transformInstanceId2 = 0;
        this.positionType1 = PositionType.Manual;
        this.positionType2 = PositionType.Manual;
    }
}

public readonly struct RequestGetTransformPositionData
{
    public readonly CalcDistanceRequestData.PositionField positionField;
    public readonly int targetTransformInstanceId;
    public readonly int nativeArrayIndex;

    public RequestGetTransformPositionData(CalcDistanceRequestData.PositionField positionField, int targetTransformInstanceId, int nativeArrayIndex)
    {
        this.positionField = positionField;
        this.targetTransformInstanceId = targetTransformInstanceId;
        this.nativeArrayIndex = nativeArrayIndex;
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

    // 計算要求リスト
    private List<IDistanceRequester> distanceRequesterList = new List<IDistanceRequester>();

    // TranfsormからPositionを取得する処理用の一時リスト
    private List<RequestGetTransformPositionData> requestGetTransformPositionDataListForWork = new List<RequestGetTransformPositionData>();



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

        requestGetTransformPositionDataListForWork.Clear();

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

            switch (calcDistanceRequestData.positionType1)
            {
                case PositionType.Manual:
                    positions1Native[i] = calcDistanceRequestData.position1;
                    break;
                case PositionType.Transform:
                    // Transform取得リクエストに追加
                    requestGetTransformPositionDataListForWork.Add(new RequestGetTransformPositionData(
                        CalcDistanceRequestData.PositionField.Position1,
                        calcDistanceRequestData.transformInstanceId1,
                        i
                        ));
                    break;
                case PositionType.Camera:
                    positions1Native[i] = cameraPosition;
                    break;
            }

            switch (calcDistanceRequestData.positionType2)
            {
                case PositionType.Manual:
                    positions2Native[i] = calcDistanceRequestData.position2;
                    break;
                case PositionType.Transform:
                    // Transform取得リクエストに追加
                    requestGetTransformPositionDataListForWork.Add(new RequestGetTransformPositionData(
                        CalcDistanceRequestData.PositionField.Position2,
                        calcDistanceRequestData.transformInstanceId2,
                        i
                        ));
                    break;
                case PositionType.Camera:
                    positions2Native[i] = cameraPosition;
                    break;
            }
        }
#if SW
        sw2.Stop();

        System.Diagnostics.Stopwatch sw3 = new System.Diagnostics.Stopwatch();
        sw3.Start();
#endif

        int requestGetTransformPositionDataListCount = requestGetTransformPositionDataListForWork.Count;
        TransformAccessArray transformAccessArray = new TransformAccessArray(requestGetTransformPositionDataListCount);
        //ResizeTransformAccessArray(requestGetTransformPositionDataListCount);
        NativeArray<float3> resultTransformPositionArray = new NativeArray<float3>(requestGetTransformPositionDataListCount, Allocator.TempJob);

        for (int i = 0; i < requestGetTransformPositionDataListForWork.Count; i++)
        {
            RequestGetTransformPositionData requestGetTransformPositionData = requestGetTransformPositionDataListForWork[i];

            transformAccessArray.Add(requestGetTransformPositionData.targetTransformInstanceId);
        }

        var getTransformPositionJob = new TransformPositionJobParallel()
        {
            Positions = resultTransformPositionArray
        };

        // ジョブをスケジュールして実行
        JobHandle getTransformPositionHandle = getTransformPositionJob.Schedule(transformAccessArray);
        getTransformPositionHandle.Complete();

        for (int i = 0; i < requestGetTransformPositionDataListForWork.Count; i++)
        {
            RequestGetTransformPositionData requestGetTransformPositionData = requestGetTransformPositionDataListForWork[i];
            float3 resultTransformPosition = resultTransformPositionArray[i];

            switch (requestGetTransformPositionData.positionField)
            {
                case CalcDistanceRequestData.PositionField.Position1:
                    positions1Native[requestGetTransformPositionData.nativeArrayIndex] = resultTransformPosition;
                    break;
                case CalcDistanceRequestData.PositionField.Position2:
                    positions2Native[requestGetTransformPositionData.nativeArrayIndex] = resultTransformPosition;
                    break;
            }
        }

        transformAccessArray.Dispose();
        resultTransformPositionArray.Dispose();

#if SW
        sw3.Stop();

        System.Diagnostics.Stopwatch sw4 = new System.Diagnostics.Stopwatch();
        sw4.Start();
#endif
        CalculateSqrDistancesJob job = new CalculateSqrDistancesJob
        {
            Positions1 = positions1Native,
            Positions2 = positions2Native,
            SqrDistances = distancesNative
        };
#if SW
        sw4.Stop();
#endif

#if SW
        System.Diagnostics.Stopwatch sw5 = new System.Diagnostics.Stopwatch();
        sw5.Start();
#endif
        JobHandle handle = job.Schedule(positions1Native.Length, 64);
        handle.Complete();

#if SW
        sw5.Stop();

        System.Diagnostics.Stopwatch sw6 = new System.Diagnostics.Stopwatch();
        sw6.Start();
#endif
        for (int i = 0; i < distanceRequesterList.Count; i++)
        {
            distanceRequesterList[i].ReturnSqrDistance(distancesNative[i]);
        }
#if SW
        sw6.Stop();
#endif

#if SW
        Debug.Log("1: " + sw1.ElapsedMilliseconds + "   2: " + sw2.ElapsedMilliseconds + "   3: " + sw3.ElapsedMilliseconds + "   4: " + sw4.ElapsedMilliseconds + "   5: " + sw5.ElapsedMilliseconds + "   6: " + sw6.ElapsedMilliseconds + "   loop: " + swloop.ElapsedMilliseconds);
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

    /// <summary>
    /// TransformAccessArrayの要素数を引数の値にリサイズする
    /// </summary>
    /// <param name="length"></param>
    //private void ResizeTransformAccessArray(int length)
    //{
    //    int currentLength = transformAccessArray.length;
    //    int diffLength = length - currentLength;

    //    if (diffLength == 0)
    //    {
    //        return;
    //    }
    //    else if (diffLength > 0)
    //    {
    //        for (int i = 0; i < diffLength; i++)
    //        {
    //            transformAccessArray.Add(null);
    //        }
    //    }
    //    else // if(diffLength < 0)
    //    {
    //        for (int i = diffLength - 1; i >= 0; i--)
    //        {
    //            transformAccessArray.RemoveAtSwapBack(transformAccessArray.length - 1);
    //        }
    //    }
    //}

    private void OnDestroy()
    {
    }

    /// <summary>
    /// Sqrの距離を計算して返却するJob
    /// </summary>
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

    /// <summary>
    /// Transformから高速にPositionを取得して返却するJob
    /// </summary>
    [BurstCompile]
    struct TransformPositionJobParallel : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<float3> Positions;

        public void Execute(int index, TransformAccess transform)
        {
            Positions[index] = transform.position;
        }
    }
}
