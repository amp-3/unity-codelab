using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Util
{
    /// <summary>
    /// (X, Y)を(X, 0f, Y)に変換する
    /// </summary>
    /// <param name="vec2"></param>
    /// <returns></returns>
    public static Vector3 ConvertVec2ToVec3(Vector2 vec2)
    {
        return new Vector3(vec2.x, 0f, vec2.y);
    }

    /// <summary>
    /// Vector3.SqrMagnitude()互換の高速演算
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float SqrMagnitude(in Vector3 v)
    {
        return v.x * v.x + v.y * v.y + v.z * v.z;
    }
    /// <summary>
    /// Vector3.Magnitude()互換の高速演算
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float Magnitude(in Vector3 v)
    {
        return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }

    /// <summary>
    /// Vector3.Normalize()互換の高速演算
    /// </summary>
    /// <param name="v"></param>
    /// <param name="vOut">正規化したベクトルを代入して返す</param>
    public static void Normalize(in Vector3 v, out Vector3 vOut)
    {
        float mag = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        vOut.x = v.x / mag; vOut.y = v.y / mag; vOut.z = v.z / mag;
    }

    /// <summary>
    /// Vector3.Dot()互換の外積の高速演算
    /// in による参照渡しのおかげでパフォーマンスが良い
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static float Dot(in Vector3 v1, in Vector3 v2)
    {
        return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
    }

    /// <summary>
    /// Vector3.Cross()互換の外積の高速演算
    /// </summary>
    /// <param name="v"></param>
    /// <param name="vOut">正規化したベクトルを代入して返す</param>
    public static void Cross(in Vector3 v1, in Vector3 v2, out Vector3 vOut)
    {
        vOut.x = v1.y * v2.z - v1.z * v2.y;
        vOut.y = v1.z * v2.x - v1.x * v2.z;
        vOut.z = v1.x * v2.y - v1.y * v2.x;
    }

    /// <summary>
    /// Vector3.Normalize()、Vector3.Magnitude()互換の高速演算
    /// ベクトルの正規化、Magnitudeを計算してそれぞれ返す(P24引用)
    /// https://www.slideshare.net/UnityTechnologiesJapan002/unite-2018-tokyo60fpsstg-96482513
    /// </summary>
    /// <param name="v"></param>
    /// <param name="vOut">正規化したベクトルを代入して返す</param>
    /// <returns>Magnitudeを返す</returns>
    public static float NormalizeAndReturnMagnitude(in Vector3 v, out Vector3 vOut)
    {
        float mag = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        vOut.x = v.x / mag; vOut.y = v.y / mag; vOut.z = v.z / mag;
        return mag;
    }

    public static float SqrDistance(in Vector3 v1, in Vector3 v2)
    {
        return
            (v2.x - v1.x) * (v2.x - v1.x) +
            (v2.y - v1.y) * (v2.y - v1.y) +
            (v2.z - v1.z) * (v2.z - v1.z);
    }
    public static float Distance(in Vector3 v1, in Vector3 v2)
    {
        return
            (float)Math.Sqrt((v2.x - v1.x) * (v2.x - v1.x) +
            (v2.y - v1.y) * (v2.y - v1.y) +
            (v2.z - v1.z) * (v2.z - v1.z));
    }

    /// <summary>
    /// 2点間の距離が、引数距離の範囲内かどうか
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="thresholdDistance">範囲内とする距離。同値なら範囲内にいると判定する</param>
    /// <returns></returns>
    public static bool IsInRange(in Vector3 v1, in Vector3 v2, float thresholdDistance)
    {
        //平方根を使わない処理で高速化
        return SqrDistance(v1, v2) <= thresholdDistance * thresholdDistance;
    }

    /// <summary>
    /// Vector3の高速加算
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="vOut"></param>
    public static void Addition(in Vector3 v1, in Vector3 v2, out Vector3 vOut)
    {
        vOut.x = v2.x + v1.x;
        vOut.y = v2.y + v1.y;
        vOut.z = v2.z + v1.z;
    }
    /// <summary>
    /// Vector3の高速引算
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="vOut"></param>
    public static void Subtraction(in Vector3 v1, in Vector3 v2, out Vector3 vOut)
    {
        vOut.x = v2.x - v1.x;
        vOut.y = v2.y - v1.y;
        vOut.z = v2.z - v1.z;
    }
}
