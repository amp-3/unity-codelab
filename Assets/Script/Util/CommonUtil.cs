using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class CommonUtil
{
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    [DllImport("User32.dll")]
    private static extern bool GetCursorPos(ref Win32Point pt);
    [DllImport("User32", EntryPoint = "GetSystemMetrics")]
    public static extern int GetSystemMetrics(SystemMetrics nIndex);
    public enum SystemMetrics : int
    {
        SM_CXSCREEN = 0,
        SM_CYSCREEN = 1,
    };
    /// <summary>
    /// グローバルなマウス座標を取得　左上が(0,0)でUnityのInput.mousePositionとはYのみ逆転しているので注意
    /// </summary>
    /// <returns></returns>
    public static Vector2 GetMousePosition()
    {
        Win32Point win32Point = new Win32Point();
        GetCursorPos(ref win32Point);
        return new Vector2(win32Point.X, win32Point.Y);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public int X;
        public int Y;
    }

    public enum AxisType3D
    {
        X,
        Y,
        Z
    }
    public enum AxisType2D
    {
        X,
        Y
    }

    /// <summary>
    /// 対象のオブジェクトのレイヤー設定
    /// </summary>
    /// <param name="target">対象</param>
    /// <param name="layer">設定するレイヤーの値</param>
    /// <param name="isRecursive">再帰的に子要素にも行うかどうか</param>
    public static void SetLayer(GameObject target, int layer, bool isRecursive = false)
    {
        if (target == null)
        {
            return;
        }

        if (target.layer != layer) target.layer = layer;

        if (isRecursive)
        {
            foreach (Transform child in target.transform)
            {
                SetLayer(child.gameObject, layer, isRecursive);
            }
        }
    }
    /// <summary>
    /// GameObjectのActive切り替え処理
    /// 重複してSetActiveを呼ばないようにしている
    /// </summary>
    /// <param name="_target">Active切り替え対象</param>
    /// <param name="_isActive">Activeかどうか</param>
    public static void SetGameObjectActivation(GameObject _target, bool _isActive)
    {
        if (_target == null || _target.activeSelf == _isActive)
        {
            return;
        }

        _target.SetActive(_isActive);
    }

    #region Compare
    /// <summary>
    /// 2つのbinaryのバイナリデータが同一か確認する
    /// いずれかまたは両方がnullの場合はfalseを返す
    /// TODO:シングルスレッドブースト→要素を分割してJobSystem+BurstComplerで回す
    /// </summary>
    /// <param name="binary1"></param>
    /// <param name="binary2"></param>
    /// <returns></returns>
    public static bool IsEqualBinary(byte[] binary1, byte[] binary2)
    {
        //いずれかnullならfalseを返す
        if (binary1 == null || binary2 == null) return false;
        //要素数が異なれば異なるバイナリデータとしてfalseを返す
        if (binary1.Length != binary2.Length) return false;

        //各byteを走査して一致を確認
        for (int i = 0; i < binary1.Length; i++)
        {
            //1バイトでも異なるようならfalseを返す
            if (binary1[i] != binary2[i]) return false;
        }

        //全てのbyteで一致を確認した場合はtrueを返す
        return true;
    }

    #endregion

    #region Judge
    /// <summary>
    /// 2直線の交点が存在するかを判定する
    /// </summary>
    /// <param name="lineA_Start"></param>
    /// <param name="lineA_End"></param>
    /// <param name="lineB_Start"></param>
    /// <param name="lineB_End"></param>
    /// <returns></returns>
    public static bool IsIntersection(Vector3 lineA_Start, Vector3 lineA_End, Vector3 lineB_Start, Vector3 lineB_End)
    {
        var ta = (lineB_Start.x - lineB_End.x) * (lineA_Start.z - lineB_Start.z) + (lineB_Start.z - lineB_End.z) * (lineB_Start.x - lineA_Start.x);
        var tb = (lineB_Start.x - lineB_End.x) * (lineA_End.z - lineB_Start.z) + (lineB_Start.z - lineB_End.z) * (lineB_Start.x - lineA_End.x);
        var tc = (lineA_Start.x - lineA_End.x) * (lineB_Start.z - lineA_Start.z) + (lineA_Start.z - lineA_End.z) * (lineA_Start.x - lineB_Start.x);
        var td = (lineA_Start.x - lineA_End.x) * (lineB_End.z - lineA_Start.z) + (lineA_Start.z - lineA_End.z) * (lineA_Start.x - lineB_End.x);

        return tc * td < 0 && ta * tb < 0;
        // return tc * td <= 0 && ta * tb <= 0; // 端点を含む場合
    }

    /// <summary>
    /// AB CDで構成される２直線の3次元な交点(あるいは最近点)を求め、交点を引数のResultへ代入する
    /// http://www.sousakuba.com/Programming/gs_two_lines_intersect.html
    /// </summary>
    /// <param name="result">Vector3 2個の配列  0=計算できず（平行であったりA=B C=Dのばあい）1=交点があった resultに交点を格納 2=交点がない resultには最近点を格納</param>
    /// <param name="lineA_Start"></param>
    /// <param name="lineA_End"></param>
    /// <param name="lineB_Start"></param>
    /// <param name="lineB_End"></param>
    /// <returns></returns>
    public static int IntersectLines(ref Vector3[] result,
                         Vector3 lineA_Start, Vector3 lineA_End, Vector3 lineB_Start, Vector3 lineB_End)
    {
        result = new Vector3[2];

        //A=B C=Dのときは計算できない
        if ((lineA_End - lineA_Start).sqrMagnitude == 0 || (lineB_End - lineB_Start).sqrMagnitude == 0)
        {
            return 0;
        }

        Vector3 AB = lineA_End - lineA_Start;
        Vector3 CD = lineB_End - lineB_Start;

        Vector3 n1 = AB.normalized;
        Vector3 n2 = CD.normalized;

        float work1 = Vector3.Dot(n1, n2);
        float work2 = 1 - work1 * work1;

        //直線が平行な場合は計算できない 平行だとwork2が0になる
        if (work2 == 0) { return 0; }

        Vector3 AC = lineB_Start - lineA_Start;

        float d1 = (Vector3.Dot(AC, n1) - work1 * Vector3.Dot(AC, n2)) / work2;
        float d2 = (work1 * Vector3.Dot(AC, n1) - Vector3.Dot(AC, n2)) / work2;

        //AB上の最近点
        result[0].x = lineA_Start.x + d1 * n1.x;
        result[0].y = lineA_Start.y + d1 * n1.y;
        result[0].z = lineA_Start.z + d1 * n1.z;

        //BC上の最近点
        result[1].x = lineB_Start.x + d2 * n2.x;
        result[1].y = lineB_Start.y + d2 * n2.y;
        result[1].z = lineB_Start.z + d2 * n2.z;

        //交差の判定 誤差は用途に合わせてください
        if ((result[0] - result[1]).sqrMagnitude < 0.000001f)
        {
            //交差した
            return 1;
        }

        //交差しなかった。
        return 2;
    }

    /// <summary>
    /// 点と円が衝突しているかチェック
    /// http://www.gamecorder.net/collision/collision2.php
    /// </summary>
    /// <param name="circleCenter"></param>
    /// <param name="r"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static bool ApproxCircleInPoint(Vector2 circleCenter, float r, Vector2 point)
    {
        return ((circleCenter.x - point.x) * (circleCenter.x - point.x) + (circleCenter.y - point.y) * (circleCenter.y - point.y) <= r * r);
    }
    /// <summary>
    /// 多角形の中に点が内包されているかを判定
    /// </summary>
    /// <param name=""></param>
    /// <param name=""></param>
    /// <returns></returns>
    public static bool JudgePolygonIncludePoint(Vector2 p1, List<Vector2> comparisonArr)
    {
        float deg = 0;
        float p1x = p1.x;
        float p1y = p1.y;
        float p3x;
        float p3y;

        for (var index = 0; index < comparisonArr.Count; index++)
        {
            float p2x = comparisonArr[index].x;
            float p2y = comparisonArr[index].y;
            if (index < comparisonArr.Count - 1)
            {
                p3x = comparisonArr[index + 1].x;
                p3y = comparisonArr[index + 1].y;
            }
            else
            {
                p3x = comparisonArr[0].x;
                p3y = comparisonArr[0].y;
            }

            float ax = p2x - p1x;
            float ay = p2y - p1y;
            float bx = p3x - p1x;
            float by = p3y - p1y;

            var cos = (ax * bx + ay * by) / (Mathf.Sqrt(ax * ax + ay * ay) * Mathf.Sqrt(bx * bx + by * by));
            deg += GetDegree(Mathf.Acos(cos));
        }

        return Mathf.Round(deg) == 360;
    }
    /// <summary>
    /// 点の多角形に対する内外判定
    /// Winding Number Algorithmを使用
    /// http://www.nttpc.co.jp/technology/number_algorithm.html
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="comparisonArr"></param>
    /// <returns></returns>
    public static bool JudgePolygonIncludePoint_beta(Vector2 p1, List<Vector2> comparisonArr)
    {
        int wn = 0;

        for (int i = 0; i < comparisonArr.Count - 1; i++)
        {
            // 上向きの辺、下向きの辺によって処理が分かれる。
            // 上向きの辺。点Pがy軸方向について、始点と終点の間にある。ただし、終点は含まない。(ルール1)
            if ((comparisonArr[i].y <= p1.y) && (comparisonArr[i + 1].y > p1.y))
            {
                // 辺は点pよりも右側にある。ただし、重ならない。(ルール4)
                // 辺が点pと同じ高さになる位置を特定し、その時のxの値と点pのxの値を比較する。
                float vt = (p1.y - comparisonArr[i].y) / (comparisonArr[i + 1].y - comparisonArr[i].y);
                if (p1.x < (comparisonArr[i].x + (vt * (comparisonArr[i + 1].x - comparisonArr[i].x))))
                {
                    ++wn;  //ここが重要。上向きの辺と交差した場合は+1

                }
            }
            // 下向きの辺。点Pがy軸方向について、始点と終点の間にある。ただし、始点は含まない。(ルール2)
            else if ((comparisonArr[i].y > p1.y) && (comparisonArr[i + 1].y <= p1.y))
            {
                // 辺は点pよりも右側にある。ただし、重ならない。(ルール4)
                // 辺が点pと同じ高さになる位置を特定し、その時のxの値と点pのxの値を比較する。
                float vt = (p1.y - comparisonArr[i].y) / (comparisonArr[i + 1].y - comparisonArr[i].y);
                if (p1.x < (comparisonArr[i].x + (vt * (comparisonArr[i + 1].x - comparisonArr[i].x))))
                {
                    --wn;  //ここが重要。下向きの辺と交差した場合は-1

                }
            }
            // ルール1,ルール2を確認することで、ルール3も確認できている。
        }

        //奇数なら内側
        return wn % 2 == 1;
    }
    /// <summary>
    /// 2つの期間が重なり合うかどうかを判定する
    /// http://koseki.hatenablog.com/entry/20111021/range
    /// https://stackoverflow.com/questions/13513932/algorithm-to-detect-overlapping-periods
    /// </summary>
    public static bool JudgePeriodOverlaps(float start1, float end1, float start2, float end2)
    {
        return start1 < end2 && start2 < end1;
    }
    /// <summary>
    /// ShortがLongの期間内に存在するかどうかを判定
    /// </summary>
    /// <param name="startShort"></param>
    /// <param name="endShort"></param>
    /// <param name="startLong"></param>
    /// <param name="endLong"></param>
    /// <returns></returns>
    public static bool JudgePeriodInclude(float startShort, float endShort, float startLong, float endLong)
    {
        return startLong < startShort && endLong > endShort;
    }
    public static bool JudgeDateTimeWithinThePeriod(DateTime target, DateTime startTime, DateTime endTime)
    {
        return target >= startTime && target <= endTime;
    }
    public static bool IsPassLayerMask(int layerNum, LayerMask collisionTargetLayer)
    {
        int layerMask = 1 << layerNum;
        return (collisionTargetLayer & layerMask) == layerMask;
    }
    #endregion

    #region Calc
    // p2からp1への角度を求める
    // @param p1 自分の座標
    // @param p2 相手の座標
    // @return 2点の角度(Degree)
    public static float GetAim(Vector2 p1, Vector2 p2)
    {
        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;
        float rad = Mathf.Atan2(dy, dx);
        return rad * Mathf.Rad2Deg;
    }
    /// <summary>
    ///     <paramref name="ra" />から<paramref name="rb" />への回転から、軸<paramref name="axis" />に関するねじれ角を得ます。
    ///     https://teratail.com/questions/145498
    /// </summary>
    /// <param name="ra">起点の回転。</param>
    /// <param name="rb">終点の回転。</param>
    /// <param name="axis">ねじれ軸。</param>
    /// <returns>軸<paramref name="axis" />に関する0°以上360°未満のねじれ角。</returns>
    public static float GetTwistAroundAxis(Quaternion ra, Quaternion rb, Vector3 axis)
    {
        // 軸を正規化する
        if (axis == Vector3.zero)
        {
            axis = Vector3.forward;
        }
        axis.Normalize();

        // da、db、rab、rdadbを求める
        var da = ra * axis;
        var db = rb * axis;
        var rab = rb * Quaternion.Inverse(ra);
        var rdadb = Quaternion.FromToRotation(da, db);

        // rdadbからrabへの回転を求めたのち、その軸と角度を抽出する
        Vector3 deltaAxis;
        float deltaAngle;
        var delta = rab * Quaternion.Inverse(rdadb);
        delta.ToAngleAxis(out deltaAngle, out deltaAxis);

        // dbとdeltaAxisは同一直線上にあるはずだが、向きは逆かもしれない
        // 角度の正負を統一するため、向きの逆転の有無を調べる
        // deltaAngleSignはdbとdeltaAxisの向きが一致していれば1、逆転していれば-1になる
        var deltaAngleSign = Mathf.Sign(Vector3.Dot(db, deltaAxis));

        // 角度の符号を補正した上で0°～360°におさめて返す
        var result = (deltaAngleSign * deltaAngle) % 360.0f;
        if (result < 0.0f)
        {
            result += 360.0f;
        }
        return result;
    }
    /// <summary>
    /// UnityAPIに依存しないLookAt処理
    /// http://edom18.hateblo.jp/entry/2018/04/18/104054
    /// </summary>
    /// <param name="lookAtTargetPos"></param>
    /// <param name="selfPos"></param>
    /// <returns></returns>
    public static Quaternion LookAt(Vector3 lookAtTargetPos, Vector3 selfPos)
    {
        Vector3 z = (lookAtTargetPos - selfPos).normalized;
        Vector3 x = Vector3.Cross(Vector3.up, z).normalized;
        Vector3 y = Vector3.Cross(z, x).normalized;

        Matrix4x4 m = Matrix4x4.identity;
        m[0, 0] = x.x; m[0, 1] = y.x; m[0, 2] = z.x;
        m[1, 0] = x.y; m[1, 1] = y.y; m[1, 2] = z.y;
        m[2, 0] = x.z; m[2, 1] = y.z; m[2, 2] = z.z;

        Quaternion rot = GetLookAtRotation(m);
        return rot;
    }
    private static Quaternion GetLookAtRotation(Matrix4x4 m)
    {
        float[] elem = new float[4];
        elem[0] = m.m00 - m.m11 - m.m22 + 1.0f;
        elem[1] = -m.m00 + m.m11 - m.m22 + 1.0f;
        elem[2] = -m.m00 - m.m11 + m.m22 + 1.0f;
        elem[3] = m.m00 + m.m11 + m.m22 + 1.0f;

        int biggestIdx = 0;
        for (int i = 0; i < elem.Length; i++)
        {
            if (elem[i] > elem[biggestIdx])
            {
                biggestIdx = i;
            }
        }

        if (elem[biggestIdx] < 0)
        {
            //Wrong matrix
            return new Quaternion();
        }

        float[] q = new float[4];
        float v = Mathf.Sqrt(elem[biggestIdx]) * 0.5f;
        q[biggestIdx] = v;
        float mult = 0.25f / v;

        switch (biggestIdx)
        {
            case 0:
                q[1] = (m.m10 + m.m01) * mult;
                q[2] = (m.m02 + m.m20) * mult;
                q[3] = (m.m21 - m.m12) * mult;
                break;
            case 1:
                q[0] = (m.m10 + m.m01) * mult;
                q[2] = (m.m21 + m.m12) * mult;
                q[3] = (m.m02 - m.m20) * mult;
                break;
            case 2:
                q[0] = (m.m02 + m.m20) * mult;
                q[1] = (m.m21 + m.m12) * mult;
                q[3] = (m.m10 - m.m01) * mult;
                break;
            case 3:
                q[0] = (m.m21 - m.m12) * mult;
                q[1] = (m.m02 - m.m20) * mult;
                q[2] = (m.m10 - m.m01) * mult;
                break;
        }

        return new Quaternion(q[0], q[1], q[2], q[3]);
    }

    /// <summary>
    /// 引数のベクトルに対して法線方向のベクトルを求める
    /// 法線と垂直な面と平行なベクトルであれば、どの方向を向いていても法線と垂直なベクトルになるので一意には求まらない
    /// https://detail.chiebukuro.yahoo.co.jp/qa/question_detail/q11136377331
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static Vector3 GetTangent(Vector3 n)
    {
        if (Mathf.Abs(Vector3.Dot(Vector3.up, n)) >= 1.0)
            return Vector3.right;
        else
            return Vector3.Cross(Vector3.up, n);
    }

    /// <summary>
    /// 面積を求める
    /// http://www.ss.cs.meiji.ac.jp/CCP032.html
    /// </summary>
    /// <param name="pointList"></param>
    /// <returns></returns>
    public static float GetArea(List<Vector2> pointList)
    {
        int i, j, n = pointList.Count;
        float s = 0;
        for (i = 0; i < n; i++)
        {
            j = (i + 1) % n;
            s += pointList[i].x * pointList[j].y - pointList[j].x * pointList[i].y;
        }
        if (s < 0.0)
            s = -s;
        return s / 2.0f;
    }
    /// <summary>
    /// 面積を求める
    /// http://www.ss.cs.meiji.ac.jp/CCP032.html
    /// </summary>
    /// <param name="pointList"></param>
    /// <returns></returns>
    public static float GetArea(Vector2[] pointArray)
    {
        int i, j, n = pointArray.Length;
        float s = 0;
        for (i = 0; i < n; i++)
        {
            j = (i + 1) % n;
            s += pointArray[i].x * pointArray[j].y - pointArray[j].x * pointArray[i].y;
        }
        if (s < 0.0)
            s = -s;
        return s / 2.0f;
    }

    /// <summary>
    /// ラジアン角から角度に変換する
    /// </summary>
    /// <param name="radian"></param>
    /// <returns></returns>
    public static float GetDegree(float radian)
    {
        return (float)(radian / Math.PI * 180f);
    }

    /// <summary>
    /// ある点を基点に回転を行った後の位置を取得する
    /// </summary>
    /// <param name="point">回転する対象の現在位置</param>
    /// <param name="pivot">回転する基点</param>
    /// <param name="angles">回転量</param>
    /// <returns>回転した結果の位置</returns>
    public static Vector3 RotatePointAroundPivot(in Vector3 point, in Vector3 pivot, in Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }
    /// <summary>
    /// ある点を基点に回転を行った後の位置を取得する
    /// </summary>
    /// <param name="point">回転する対象の現在位置</param>
    /// <param name="pivot">回転する基点</param>
    /// <param name="angles">回転量</param>
    /// <returns>回転した結果の位置</returns>
    public static Vector3 RotatePointAroundPivot(in Vector3 point, in Vector3 pivot, in Quaternion angles)
    {
        return angles * (point - pivot) + pivot;
    }

    public static Vector3 GetLargestBoundSize(GameObject rootParent)
    {
        List<GameObject> allObj = SearchAllChildrenObj(rootParent);
        Vector3 largestBoundSize = new Vector3(0, 0, 0);
        foreach (GameObject oneObj in allObj)
        {
            Renderer renderer = oneObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                largestBoundSize = new Vector3(Mathf.Max(renderer.bounds.size.x, largestBoundSize.x),
                    Mathf.Max(renderer.bounds.size.y, largestBoundSize.y),
                    Mathf.Max(renderer.bounds.size.z, largestBoundSize.z));
            }
        }

        return largestBoundSize;
    }

    /// <summary>
    /// ある点が与えられたとき、線分上から最も近い場所を返す(2D限定)
    /// http://sampo.hatenadiary.jp/entry/20070626/p1
    /// </summary>
    /// <param name="A">線分の始点</param>
    /// <param name="B">線分の終点</param>
    /// <param name="P">点の座標</param>
    /// <returns></returns>
    public static Vector2 NearestOnLinePoint(Vector2 A, Vector2 B, Vector2 P)
    {
        Vector2 a, b;
        float r;

        a.x = B.x - A.x;
        a.y = B.y - A.y;
        b.x = P.x - A.x;
        b.y = P.y - A.y;

        r = (a.x * b.x + a.y * b.y) / (a.x * a.x + a.y * a.y);

        if (r <= 0)
        {
            return A;
        }
        else if (r >= 1)
        {
            return B;
        }
        else
        {
            Vector2 result;
            result.x = A.x + r * a.x;
            result.y = A.y + r * a.y;
            return result;
        }
    }

    /// <summary>
    /// この関数は、ポイントから線への投影点を返します。
	/// この線は無限と見なされます。線が有限であれば、代わりにProjectPointOnLineSegment()を使用します。
    /// </summary>
    /// <param name="linePoint"></param>
    /// <param name="lineVec"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
    {
        return Math3d.ProjectPointOnLine(linePoint, lineVec, point);
    }

    /// <summary>
    /// この関数は、点から線分への投影点を返します。
    /// 投影点が線分の外側にある場合、投影された点は適切な線の端にクランプされます。
    /// 線分がセグメントではなく無限大の場合は、代わりにProjectPointOnLine()を使用します。
    /// </summary>
    /// <param name="linePoint1"></param>
    /// <param name="linePoint2"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
    {
        return Math3d.ProjectPointOnLineSegment(linePoint1, linePoint2, point);
    }


    /// <summary>
    /// 点と線分の距離を求める
    /// http://qiita.com/yellow_73/items/bcd4e150e7caa0210ee6
    /// </summary>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <returns></returns>
    public static float DistancePointAndLine(float x0, float y0, float x1, float y1, float x2, float y2)
    {
        var a = x2 - x1;
        var b = y2 - y1;
        var a2 = a * a;
        var b2 = b * b;
        var r2 = a2 + b2;
        var tt = -(a * (x1 - x0) + b * (y1 - y0));
        if (tt < 0)
        {
            return (x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0);
        }
        if (tt > r2)
        {
            return (x2 - x0) * (x2 - x0) + (y2 - y0) * (y2 - y0);
        }
        var f1 = a * (y1 - y0) - b * (x1 - x0);
        return (f1 * f1) / r2;
    }

    public static Vector3[] GetSnapPoint(Vector3 pointA, Vector3 pointB)
    {
        Vector3[] snapPoint = new Vector3[2];
        float extendLengthForCheck = 250f;

        //各ポイントから上下と左右に伸ばした仮想的な線分同士を交差チェックし、交点を収集する
        //PointA横展開　PointB縦展開
        Vector3[] result = new Vector3[2];
        Vector3 pointAWideLeft = pointA + Vector3.left * extendLengthForCheck;
        Vector3 pointAWideRight = pointA + Vector3.right * extendLengthForCheck;
        Vector3 pointBTallTop = pointB + Vector3.forward * extendLengthForCheck;
        Vector3 pointBTallBottom = pointB + Vector3.back * extendLengthForCheck;
        if (IntersectLines(ref result, pointAWideLeft, pointAWideRight, pointBTallTop, pointBTallBottom) == 1) snapPoint[0] = result[0];
        //else Debug.Log("交点が検出されませんでした");

        Vector3 pointAWideTop = pointA + Vector3.forward * extendLengthForCheck;
        Vector3 pointAWideBottom = pointA + Vector3.back * extendLengthForCheck;
        Vector3 pointBTallLeft = pointB + Vector3.left * extendLengthForCheck;
        Vector3 pointBTallRight = pointB + Vector3.right * extendLengthForCheck;
        if (IntersectLines(ref result, pointAWideTop, pointAWideBottom, pointBTallLeft, pointBTallRight) == 1) snapPoint[1] = result[0];
        //else Debug.Log("交点が検出されませんでした");

        return snapPoint;
    }
    public static Vector3 SetVectorValueByAxis(Vector3 vector, AxisType3D axisEnum, float value)
    {
        if (axisEnum == AxisType3D.X) vector.x = value;
        else if (axisEnum == AxisType3D.Y) vector.y = value;
        else if (axisEnum == AxisType3D.Z) vector.z = value;

        return vector;
    }
    /// <summary>
    /// より小さい倍数を求める（倍数で切り捨てられるような値）
    ///（例）倍数 = 10 のとき、12 → 10, 17 → 10
    /// http://fantom1x.blog130.fc2.com/blog-entry-247.html?sp
    /// </summary>
    /// <param name="value">入力値</param>
    /// <param name="multiple">倍数</param>
    /// <returns>倍数で切り捨てた値</returns>
    public static float MultipleFloor(float value, float multiple)
    {
        return Mathf.Floor(value / multiple) * multiple;
    }
    /// <summary>
    /// より大きい倍数を求める（倍数で繰り上がるような値）
    ///（例）倍数 = 10 のとき、12 → 20, 17 → 20
    /// </summary>
    /// <param name="value">入力値</param>
    /// <param name="multiple">倍数</param>
    /// <returns>倍数で切り上げた値</returns>
    public static float MultipleCeil(float value, float multiple)
    {
        return Mathf.Ceil(value / multiple) * multiple;
    }
    /// <summary>
    /// 倍数での四捨五入のような値を求める（ｎおきの数の中間の値で切り捨て・切り上げをする）
    ///（例）倍数 = 10 のとき、12 → 10, 17 → 20
    /// </summary>
    /// <param name="value">入力値</param>
    /// <param name="multiple">倍数</param>
    /// <returns>倍数の中間の値で、切り捨て・切り上げした値
    public static float MultipleRound(float value, float multiple)
    {
        return MultipleFloor(value + multiple * 0.5f, multiple); //四捨五入的
                                                                 //return Math.Round(value / multiple) * multiple; //五捨六入的（正の数のとき）
    }
    #endregion

    #region Search
    public static List<GameObject> SearchAllChildrenObj(GameObject rootParent)
    {
        List<GameObject> stackObj = new List<GameObject>();
        stackObj.Add(rootParent);
        return getChildrenObj(rootParent, stackObj);
    }
    private static List<GameObject> getChildrenObj(GameObject target, List<GameObject> stackObj)
    {

        foreach (Transform item in target.transform)
        {
            stackObj.Add(item.gameObject);
            if (item.transform.childCount >= 1) getChildrenObj(item.gameObject, stackObj);
        }
        return stackObj;            //最終結果をSearchAllChildrenObjのreturnへ流すため…しかし、本関数ではreturnが有効に使われず、オブジェクトの再利用で成り立っているため、SAO()のreturnをstackObj;に変更しても良い
    }

    //public static List<GameObject> SearchAllParentsObj(GameObject obj)
    //{
    //    List<GameObject> stackObj = new List<GameObject>();
    //    stackObj.Add(obj);
    //    return getParentsObj(obj, stackObj);
    //}
    //private static List<GameObject> getParentsObj(GameObject target, List<GameObject> stackObj)
    //{

    //    foreach (Transform item in target.transform)
    //    {
    //        stackObj.Add(item.gameObject);
    //        if (item.transform.root != null) getParentsObj(item.gameObject, stackObj);
    //    }
    //    return stackObj;            //最終結果をSearchAllChildrenObjのreturnへ流すため…しかし、本関数ではreturnが有効に使われず、オブジェクトの再利用で成り立っているため、SAO()のreturnをstackObj;に変更しても良い
    //}
    /// <summary>
    /// オブジェクトの中に存在するenableなMeshRendererを一つ返す
    /// </summary>
    /// <param name="rootParent"></param>
    /// <param name="isIncludeRoot"></param>
    /// <returns></returns>
    public static MeshRenderer GetOneMeshObject(GameObject rootParent, bool isIncludeRoot)
    {
        //ルートオブジェクトの場合は自身の所持しているメッシュをチェック
        if (isIncludeRoot)
        {
            MeshRenderer meshRenderer = rootParent.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.enabled) return meshRenderer;      //meshコンポーネントが見つかった場合は返して探索を中断
        }

        foreach (Transform item in rootParent.transform)
        {
            MeshRenderer meshRenderer = item.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.enabled) return meshRenderer;      //meshコンポーネントが見つかった場合は返して探索を中断
            else
            {
                //print(rootParent.name + "____" + item.name + (meshRenderer==null));
                if (item.transform.childCount >= 1)
                {
                    MeshRenderer temp = GetOneMeshObject(item.gameObject, false);
                    if (temp != null && temp.enabled) return temp;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// コマンドライン引数から、オプション名(optionName)に対応するオプション引数を取得する
    /// 例）test.exe -optionA hoge -> targetStr="-optionA"のときhogeを取得
    /// オプション名が見つかった場合でもオプション引数が存在しない場合はnullを返す
    /// 文字列の先頭から検索し、始めに条件に該当したものを返却する
    /// </summary>
    /// <param name="args">コマンドライン引数の配列</param>
    /// <param name="optionName">検索対象のオプション名。ハイフン込みで指定する</param>
    /// <returns></returns>
    public static string GetOptionArgByCommandLine(string[] args, string optionName)
    {
        if (args.IsNullOrEmpty()) return null;

        for (int i = 0; i < args.Length; ++i)
        {
            if (args[i] == optionName)
            {
                if (IsExistNextArg(args, i) && !string.IsNullOrEmpty(args[i + 1]) && !IsCommandName(args[i + 1]))       //次の引数が存在し、かつコマンド名ではないこと
                {
                    return args[i + 1];
                }
                break;
            }
        }

        return null;
    }
    private static bool IsExistNextArg(string[] args, int nowIndex)
    {
        return nowIndex + 1 < args.Length;
    }
    private static bool IsCommandName(string arg)
    {
        //次の文字列要素の1文字目を取得
        char nextArgFirstChar = arg[0];
        //コマンドライン名の指定を表す'-'が存在すればtrue
        return nextArgFirstChar == '-';     // || nextArgFirstChar == '/';      //Unix環境ではフルパスの先頭がスラッシュで始まるためコマンド判定してしまうと誤動作するため判定しない
    }
    #endregion

    #region List
    /// <summary>ArrayからIndexの値を取得する。添字の範囲外だった場合はnullを返す</summary>
    public static GetValueSafeAccessResult<T> GetValueSafeAccess<T>(T[] array, int index) where T : class
    {
        return GetValueSafeAccess(array, index, null);
    }
    /// <summary>ArrayからIndexの値を取得する。添字の範囲外だった場合は引数のdefaultValueOnOutOfRangeを返す</summary>
    public static GetValueSafeAccessResult<T> GetValueSafeAccess<T>(T[] array, int index, in T defaultValueOnOutOfRange)
    {
        Debug.Assert(array != null, "Arrayがnullです");
        Debug.Assert(index >= 0, "indexが-1以下です");
        if (array == null
            || index <= -1
            || index >= array.Length) return new GetValueSafeAccessResult<T>(false, 0, defaultValueOnOutOfRange);

        return new GetValueSafeAccessResult<T>(true, index, array[index]);
    }
    /// <summary>ListからIndexの値を取得する。添字の範囲外だった場合はnullを返す</summary>
    public static GetValueSafeAccessResult<T> GetValueSafeAccess<T>(IReadOnlyList<T> list, int index) where T : class
    {
        return GetValueSafeAccess(list, index, null);
    }
    /// <summary>ListからIndexの値を取得する。添字の範囲外だった場合は引数のdefaultValueOnOutOfRangeを返す</summary>
    public static GetValueSafeAccessResult<T> GetValueSafeAccess<T>(IReadOnlyList<T> list, int index, in T defaultValueOnOutOfRange)
    {
        Debug.Assert(list != null, "Listがnullです");
        Debug.Assert(index >= 0, "indexが-1以下です");
        if (list == null
            || index <= -1
            || index >= list.Count) return new GetValueSafeAccessResult<T>(false, 0, defaultValueOnOutOfRange);

        return new GetValueSafeAccessResult<T>(true, index, list[index]);
    }
    public readonly struct GetValueSafeAccessResult<T>
    {
        /// <summary>indexがListの範囲内であればtrue</summary>
        public readonly bool isContains;
        public readonly int index;
        public readonly T value;

        public GetValueSafeAccessResult(bool isContains, int index, T value)
        {
            this.isContains = isContains;
            this.index = index;
            this.value = value;
        }
    }


    /// <summary>
    /// 第1引数のListの要素数をList.Add()またはRemove()を用いて引数の第2引数の整数と一致させる
    /// Add時、Remove時の処理はコールバックで指定する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetList">操作対象</param>
    /// <param name="count">Listの要素数の指定</param>
    /// <param name="onAdd"></param>
    /// <param name="onRemove"></param>
    public static void FixListItemCount<T>(IList<T> targetList, int count, Func<int, T> onAdd, Action<int, T> onRemove)
    {
        int dCount = count - targetList.Count;
        //要素数をあわせる
        if (dCount <= -1)
        {
            ////要素0の場合は処理を行わない　Count=0の場合：targetList.Count=0、i=-1、dCount=1
            //if (targetList.Count == 0) return;

            int dCountPlus = Mathf.Abs(dCount);
            int listCount = targetList.Count;
            for (int i = targetList.Count - 1; i >= (listCount - dCountPlus); i--)
            {
                onRemove(i, targetList[i]);
                targetList.RemoveAt(i);
            }
        }
        else if (dCount >= 1)
        {
            for (int i = 0; i < dCount; i++)
            {
                targetList.Add(onAdd(i));
            }
        }
    }
    /// <summary>
    /// 第1引数のListの要素数をList.Add()またはRemove()を用いて引数の第2引数の整数と一致させる
    /// Add時はnew (T)して要素を追加する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetList">操作対象</param>
    /// <param name="count"></param>
    public static void FixListItemCountAddNew<T>(IList<T> targetList, int count) where T : new()
    {
        int dCount = count - targetList.Count;

        //要素数をあわせる
        if (dCount <= -1)
        {
            ////要素0の場合は処理を行わない　Count=0の場合：targetList.Count=0、i=-1、dCount=1
            //if (targetList.Count == 0) return;

            int dCountPlus = Mathf.Abs(dCount);
            int listCount = targetList.Count;
            for (int i = targetList.Count - 1; i >= (listCount - dCountPlus); i--)
            {
                targetList.RemoveAt(i);
            }
        }
        else if (dCount >= 1)
        {
            for (int i = 0; i < dCount; i++)
            {
                targetList.Add(new T());
            }
        }
    }
    /// <summary>
    /// 第1引数のListの要素数をList.Add()またはRemove()を用いて引数の第2引数のList要素数と一致させる
    /// Add時はnew (T)して要素を追加する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="targetList">操作対象</param>
    /// <param name="sourceList">要素数の参照List</param>
    public static void FixListItemCountAddNew<T, U>(IList<T> targetList, IList<U> sourceList) where T : new()
    {
        FixListItemCountAddNew(targetList, sourceList.Count);
    }
    /// <summary>
    /// 第1引数のListの要素数をList.Add()またはRemove()を用いて引数の第2引数のList要素数と一致させる
    /// Add時はdefault(T)して要素を追加する
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetList">操作対象</param>
    /// <param name="sourceList">要素数の参照List</param>
    public static void FixListItemCountAddDefault<T>(IList<T> targetList, IList<T> sourceList)
    {
        int dCount = sourceList.Count - targetList.Count;
        //要素数をあわせる
        if (dCount <= -1)
        {
            int dCountPlus = Mathf.Abs(dCount);
            int listCount = targetList.Count;
            for (int i = targetList.Count - 1; i >= (listCount - dCountPlus); i--)
            {
                //Debug.Log(i + "  " + (i > (targetList.Count - dCountPlus)));
                targetList.RemoveAt(i);
            }
        }
        else if (dCount >= 1)
        {
            for (int i = 0; i < dCount; i++)
            {
                targetList.Add(default(T));
            }
        }
    }

    /// <summary>
    /// OldListからみてNewListにおいて削除されたオブジェクトを検出した上で、oldListから削除して余剰オブジェクトがない状態に保つ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="newList"></param>
    /// <param name="oldList"></param>
    /// <param name="equalFunc"></param>
    /// <param name="onRemove"></param>
    public static void DetectRemoveByListWithRemoveOldList<T>(IEnumerable<T> newList, IList<T> oldList, Func<T, T, bool> equalFunc, Action<T> onRemove)
    {
        List<T> removedList = CommonUtil.DetectRemoveByList(newList, oldList,
                equalFunc);
        //if(removedList == null) Debug.Log("DetectRemoveByListWithRemoveOldList null 削除候補＝0");
        if (removedList != null)
        {
            foreach (T dData in removedList)
            {
                //一致するオブジェクトをリストから削除
                T removeTarget = default;
                foreach (T oneData in oldList)
                {
                    if (equalFunc(dData, oneData))
                    {
                        removeTarget = oneData;
                        break;
                    }
                }

                //リストから削除
                oldList.Remove(removeTarget);
                //削除対象を通知
                if (onRemove != null) onRemove(removeTarget);
            }
        }
    }
    /// <summary>
    /// OldListからみてNewListにおいて追加されたオブジェクトを検出した上で、oldListに追加して不足オブジェクトがない状態に保つ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="newList"></param>
    /// <param name="oldList"></param>
    /// <param name="equalFunc"></param>
    /// <param name="onAdd"></param>
    public static void DetectAddByListWithAddOldList<T>(IEnumerable<T> newList, IList<T> oldList, Func<T, T, bool> equalFunc, Action<T> onAdd)
    {
        List<T> addedList = CommonUtil.DetectAddByList(newList, oldList,
                equalFunc);
        if (addedList != null)
        {
            foreach (T dData in addedList)
            {
                //リストに追加
                oldList.Add(dData);
                //追加対象を通知
                if (onAdd != null) onAdd(dData);
            }
        }
    }

    /// <summary>
    /// OldListからみてNewListにおいて削除されたオブジェクトを返す
    /// NewListになくてOldListにあるオブジェクトを返す
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="newList"></param>
    /// <param name="oldList"></param>
    /// <param name="removedObjCallback">一致条件</param>
    public static List<T> DetectRemoveByList<T>(IEnumerable<T> newList, IEnumerable<T> oldList, Func<T, T, bool> equalFunc)
    {
        return _DetectRemoveByList(newList, oldList, equalFunc);
    }
    /// <summary>
    /// OldListからみてNewListにおいて追加されたオブジェクトを返す
    /// NewListにあってOldListにないオブジェクトを返す
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="newList"></param>
    /// <param name="oldList"></param>
    /// <param name="addedObjCallback">一致条件</param>
    public static List<T> DetectAddByList<T>(IEnumerable<T> newList, IEnumerable<T> oldList, Func<T, T, bool> equalFunc)
    {
        //DetectRemoveByListと入れ替えると追加の検出となる
        return _DetectRemoveByList(oldList, newList, equalFunc);
    }
    /// <summary>
    /// NewListになくてOldListにあるオブジェクトを返す
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="newList"></param>
    /// <param name="oldList"></param>
    /// <param name="removedObjCallback"></param>
    private static List<T> _DetectRemoveByList<T>(IEnumerable<T> newList, IEnumerable<T> oldList, Func<T, T, bool> equalFunc)
    {
        List<T> dObjList = null;
        foreach (T oneOldObj in oldList)
        {
            bool isEqual = false;
            foreach (T oneNewObj in newList)
            {
                if (equalFunc(oneOldObj, oneNewObj))
                {
                    isEqual = true;
                    break;
                }
            }

            if (!isEqual)
            {
                //newList.Count==0の場合はequalFuncが実行されず、その式の評価に関わらずisEqual==falseで処理される
                //古いオブジェクトが新しいListに存在しないならTargetに追加を発行
                if (dObjList == null) dObjList = new List<T>();
                dObjList.Add(oneOldObj);
            }
        }
        return dObjList;
    }

    /// <summary>
    /// NewListになくてOldListにあるオブジェクトを返す
    /// 異なる型に対応
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="newList"></param>
    /// <param name="oldList"></param>
    /// <param name="removedObjCallback"></param>
    public static List<T> _DetectRemoveByList2<T, U>(IEnumerable<U> newList, IEnumerable<T> oldList, Func<U, T, bool> equalFunc)
    {
        List<T> dObjList = null;
        foreach (T oneOldObj in oldList)
        {
            bool isEqual = false;
            foreach (U oneNewObj in newList)
            {
                if (equalFunc(oneNewObj, oneOldObj))
                {
                    isEqual = true;
                    break;
                }
            }

            if (!isEqual)
            {
                //古いオブジェクトが新しいListに存在しないならTargetに追加を発行
                if (dObjList == null) dObjList = new List<T>();
                dObjList.Add(oneOldObj);
            }
        }
        return dObjList;
    }
    #endregion

    #region GenerateString
    public static string GetUniqueDateTimeString()
    {
        return DateTime.Now.ToString("yyMMddHHmmssffff");
    }
    public static long GetUniqueDateTimeNum()
    {
        return long.Parse(GetUniqueDateTimeString());
    }
    public static string GetNowDateStrSeparateTime()
    {
        return DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }
    public static string GetDateTimeToStrMs(DateTime dateTime)
    {
        return dateTime.ToString("yyMMddHHmmssffff");
    }
    public static string GetDateTimeToStrHHmmssfff(DateTime dateTime)      //int桁に対応
    {
        return dateTime.ToString("HHmmssfff");
    }
    public static string GetDateTimeToSeparateTimeStr(DateTime dateTime)
    {
        return dateTime.ToString("yyyyMMdd_HHmmss");
    }
    #endregion

    #region EditString
    /// <summary>
    /// 指定文字数まで文字列をカットする
    /// しきい値以下ならそのまま返却する
    /// </summary>
    /// <param name="str">対象の文字列</param>
    /// <param name="length">残したい文字数</param>
    /// <param name="isCutEnd">Trueなら文字列の末尾をカットし、文頭からlengthの数だけ残した文字列を返却する</param>
    /// <returns></returns>
    public static string SubstringHelper(string str, int length, bool isCutEnd = true)
    {
        if (string.IsNullOrEmpty(str)) return str;
        if (length < 0)
        {
            Debug.LogError("[CommonUtil] Substring 指定長に負の値は指定できません。処理を行わずにオリジナルを返却します");
            return str;
        }

        int originalLength = str.Length;
        if (originalLength <= length) return str;                       //指定文字数以下ならカットの必要がないのでそのまま返却する

        if (isCutEnd) return str.Substring(0, length);                  //先頭を残す
        else return str.Substring(originalLength - length, length);     //末尾を残す
    }

    /// <summary>
    /// 指定文字数まで文字列をカットする
    /// しきい値以下ならそのまま返却する
    /// サロゲートペア（代用対）や結合文字列に対応した文字列カウントを行う
    /// </summary>
    /// <param name="str">対象の文字列</param>
    /// <param name="length">残したい文字数</param>
    /// <param name="isCutEnd">Trueなら文字列の末尾をカットし、文頭からlengthの数だけ残した文字列を返却する</param>
    /// <returns></returns>
    public static string SubstringByTextElementsHelper(string str, int length, bool isCutEnd = true)
    {
        if (string.IsNullOrEmpty(str)) return str;
        if (length < 0)
        {
            Debug.LogError("[CommonUtil] Substring 指定長に負の値は指定できません。処理を行わずにオリジナルを返却します");
            return str;
        }

        //StringInfoで文字列長やSubstring処理を行う
        System.Globalization.StringInfo stringInfo = new System.Globalization.StringInfo(str);

        int originalLength = stringInfo.LengthInTextElements;
        if (originalLength <= length) return str;                       //指定文字数以下ならカットの必要がないのでそのまま返却する

        if (isCutEnd) return stringInfo.SubstringByTextElements(0, length);                  //先頭を残す
        else return stringInfo.SubstringByTextElements(originalLength - length, length);     //末尾を残す
    }

    /// <summary>
    /// (Clone)と (1) (2)...を20まで除去する
    /// </summary>
    /// <param name="cloneStr"></param>
    /// <returns></returns>
    public static string RemoveCloneString(string cloneStr)
    {
        cloneStr = cloneStr.Replace("(Clone)", "");

        for (int i = 0; i < 20; i++)
        {
            cloneStr = cloneStr.Replace(" (" + i + ")", "");
        }
        return cloneStr;
    }
    /// <summary>
    /// (
    /// </summary>
    /// <param name="cloneStr"></param>
    /// <returns></returns>
    public static string RemoveInstanceString(string cloneStr)
    {
        cloneStr = cloneStr.Replace(" (Instance)", "");

        return cloneStr;
    }
    #endregion

    #region Texture
    public static Sprite TextureToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
    #endregion

    #region AsyncUtil
    public static IEnumerator ObserveProcessDoneAfterActionCoroutine(DoneFlagClass doneFlag, Action action)
    {
        //処理完了フラグが立つまで待機
        while (!doneFlag.isDone && !doneFlag.isCancel)
        {
            yield return null;
        }

        //処理完了フラグが立ったことを確認したら実行
        if (!doneFlag.isCancel) action();
    }
    public class DoneFlagClass
    {
        public bool isDone = false;
        public bool isCancel = false;
    }
    #endregion

    #region DateTime
    /// <summary>
    /// DateTime -> DateTimeOffsetに変換する
    /// new DateTimeOffset()はDateTimeの値のままOffsetを適用するが、本関数はOffset加算した上で返却する差異がある点に注意。前者はUTC時間で差が出て、後者は差が出ない
    /// 
    /// DateTime.Kind=UTC: 問題なく処理可能
    /// DateTime.Kind=Local: new DateTimeOffset()による変換だとDateTimeのKindがLocalの場合に例外が発生する。本関数では安全な処理を行う
    /// DateTime.Kind=Unspecified: Localの場合と同じ動作を行うため、値が正しい保証は取れない。非推奨
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateTimeOffset ConvertDateTimeToDateTimeOffset(in DateTime dateTime, in TimeZoneInfo timeZoneInfo)
    {
        //DateTimeのDateTimeKindがLocalの場合、DateTimeOffset生成時に例外が発生するケースがあるためUnspecifiedに変換する
        //https://qiita.com/Shaula/items/c3278c5192543b6b4096
        //DateTime unspecifiedDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);

        ////引数のDateTimeをタイムゾーンを元にDateTimeOffsetに変換
        //DateTimeOffset dateTimeOffset = new DateTimeOffset(unspecifiedDateTime, timeZoneInfo.BaseUtcOffset);

        //return dateTimeOffset;


        //http://neue.cc/2016/12/07_546.html
        //https://stackoverflow.com/questions/36255821/datetimeoffset-error-utc-offset-of-local-datetime-does-not-match-the-offset-arg
        DateTime dateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(dateTime);         //UTCに統一する
        DateTime fixedDateTimeUtc = dateTimeUtc + timeZoneInfo.BaseUtcOffset;   //TimeZoneのオフセットを加算する

        return new DateTimeOffset(fixedDateTimeUtc.Ticks, timeZoneInfo.BaseUtcOffset);  //オフセットが加算された時刻と、オフセット情報を付与する＝UTC時間は同一のままの値を返却する
    }

    /// <summary>
    /// 引数のDateTimeOffsetとタイムゾーンから
    /// OSのタイムゾーン設定のDateTimeOffsetに変換する
    /// </summary>
    /// <param name="dateTimeOffset"></param>
    /// <returns></returns>
    public static DateTimeOffset ConvertToLocalTimeZone(in DateTimeOffset dateTimeOffset)
    {
        //システムのタイムゾーンを取得
        TimeZoneInfo systemTimeZoneInfo = TimeZoneInfo.Local;

        //引数のDateTimeOffsetをOSのタイムゾーンに変換
        DateTimeOffset convertedDateTimeOffset = TimeZoneInfo.ConvertTime(dateTimeOffset, systemTimeZoneInfo);      //dateTimeOffset.LocalDateTimeでも同一

        return convertedDateTimeOffset;
    }
    public static DateTime ConvertTodayYMD(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
    }
    #endregion

    #region System
    public static void MemoryFullGarbageCollection()
    {
#if UNITY_EDITOR
        long beforeGCMemory = GC.GetTotalMemory(false);
#endif
        Resources.UnloadUnusedAssets();       //MaterialなどUnityAPIから明示的な参照を持っていないTextureをDisposeする模様　MaterialPropertyBlockに割り当てたTextureは削除対象となってしまう模様
        GC.Collect();
#if UNITY_EDITOR
        Debug.Log("FullGC Before= " + (beforeGCMemory / 1000000L) + "MB  After" + (GC.GetTotalMemory(false) / 1000000L) + "MB");
#endif
    }
    #endregion

    #region Serialize
    /// <summary>
    /// 1byte 読み込み
    /// </summary>
    /// <param name="_binary"></param>
    /// <param name="_offset"></param>
    /// <param name="_isLittleEndian"></param>
    /// <returns></returns>
    public static byte ReadByte(in byte[] _binary, ref int _offset, bool _isLittleEndian = false)
    {
        byte ret = 0;
        if (_offset + sizeof(byte) > _binary.Length)
        {
            Debug.LogError("Out of Range");
            return ret;
        }
        // 1byte だからbit 単位でR/Wしなければエンディアンはデフォルト
        ret = _binary[_offset++];
        return ret;
    }

    /// <summary>
    /// Binaryからint をパース
    /// </summary>
    /// <param name="_binary">対象のbyte 配列</param>
    /// <param name="_offset">読み出し開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    /// <returns></returns>
    public static int ReadIntFromBinary(in byte[] _binary, ref int _offset, bool _isLittleEndian = false)
    {
        int ret = 0;
        if (_offset + sizeof(int) > _binary.Length)
        {
            Debug.LogError("Out of Range");
            return ret;
        }
        if (_isLittleEndian)
        {
            ret |= _binary[_offset++];
            ret |= _binary[_offset++] << 8;
            ret |= _binary[_offset++] << 16;
            ret |= _binary[_offset++] << 24;
        }
        else
        {
            ret |= _binary[_offset++] << 24;
            ret |= _binary[_offset++] << 16;
            ret |= _binary[_offset++] << 8;
            ret |= _binary[_offset++];
        }
        return ret;
    }

    /// <summary>
    /// Binaryから short をパース
    /// </summary>
    /// <param name="_binary">対象のbyte 配列</param>
    /// <param name="_offset">読み出し開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    /// <returns></returns>
    public static short ReadShortFromBinary(in byte[] _binary, ref int _offset, bool _isLittleEndian = false)
    {
        short ret = 0;
        if (_offset + sizeof(short) > _binary.Length)
        {
            Debug.LogError("Out of Range");
            return ret;
        }
        if (_isLittleEndian)
        {
            ret |= (short)(_binary[_offset++]);
            ret |= (short)(_binary[_offset++] << 8);
        }
        else
        {
            ret |= (short)(_binary[_offset++] << 8);
            ret |= (short)(_binary[_offset++]);
        }
        return ret;
    }
    /// <summary>
    /// Binaryから float をパース
    /// </summary>
    /// <param name="_binary">対象のbyte 配列</param>
    /// <param name="_offset">読み出し開始位置</param>
    /// <param name="_workerList">作業用のリスト。GCを発生させないために繰り返し使うリスト。実行のたびに中身はクリアされることに注意。また、スレッド間で同時に実行すると意図しない結果を生むため実行してはならない</param>
    /// <param name="_workerFloatArray">作業用の配列。GCを発生させないために繰り返し使う配列。要素はfloatのバイト数と等しい必要がある。new byte[sizeof(float)]で生成可能。また、スレッド間で同時に実行すると意図しない結果を生むため実行してはならない。条件を満たさない場合はnewされる可能性があるため、refで渡される</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    /// <returns></returns>
    public static float ReadFloatFromBinary(in byte[] _binary, ref int _offset, List<byte> _workerList, ref byte[] _workerFloatArray, bool _isLittleEndian = false)
    {
        float ret = 0;
        int length = sizeof(float);
        if (_offset + length > _binary.Length)
        {
            Debug.LogError("Out of Range");
            return ret;
        }

        //処理用の配列の要素数チェック
        if (_workerFloatArray.Length != length)
        {
            Debug.LogError("一時処理用の配列の要素数が指定数: " + length.ToString() + " と異なっていたため生成しました");
            _workerFloatArray = new byte[length];
        }

        //バイナリから読み込み開始位置からfloatのバイト数分読み出して格納
        for (int i = 0; i < _workerFloatArray.Length; i++)
        {
            _workerFloatArray[i] = _binary[_offset + i];
        }

        if (!_isLittleEndian)
        {
            //リトルエンディアンならビット反転する
            ArrayUtil.FastReverse(_workerFloatArray, _workerList);
        }

        //バイナリからfloatへ変換
        ret = BitConverter.ToSingle(_workerFloatArray, 0);

        //読み出した分をオフセットに加算する
        _offset += length;

        return ret;
    }
    /// <summary>
    /// Binaryから Vecotr3 をパース
    /// </summary>
    /// <param name="_binary"></param>
    /// <param name="_offset"></param>
    /// <param name="_workerList"></param>
    /// <param name="_workerFloatArray"></param>
    /// <param name="_isLittleEndian"></param>
    /// <returns></returns>
    public static Vector3 ReadVec3FromBinary(in byte[] _binary, ref int _offset, List<byte> _workerList, ref byte[] _workerFloatArray, bool _isLittleEndian = false)
    {
        Vector3 ret = Vector3.zero;
        ret.x = ReadFloatFromBinary(_binary, ref _offset, _workerList, ref _workerFloatArray);
        ret.y = ReadFloatFromBinary(_binary, ref _offset, _workerList, ref _workerFloatArray);
        ret.z = ReadFloatFromBinary(_binary, ref _offset, _workerList, ref _workerFloatArray);
        return ret;
    }

    /// <summary>
    /// Binaryから Quaternion をパース
    /// </summary>
    /// <param name="_binary"></param>
    /// <param name="_offset"></param>
    /// <param name="_workerList"></param>
    /// <param name="_workerFloatArray"></param>
    /// <param name="_isLittleEndian"></param>
    /// <returns></returns>
    public static Quaternion ReadQuaternionFromBinary(in byte[] _binary, ref int _offset, List<byte> _workerList, ref byte[] _workerFloatArray, bool _isLittleEndian = false)
    {
        Quaternion ret = Quaternion.identity;
        ret.x = ReadFloatFromBinary(_binary, ref _offset, _workerList, ref _workerFloatArray);
        ret.y = ReadFloatFromBinary(_binary, ref _offset, _workerList, ref _workerFloatArray);
        ret.z = ReadFloatFromBinary(_binary, ref _offset, _workerList, ref _workerFloatArray);
        ret.w = ReadFloatFromBinary(_binary, ref _offset, _workerList, ref _workerFloatArray);
        return ret;
    }

    /// <summary>
    /// short をbyte 配列に変換
    /// </summary>
    /// <param name="_value"></param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    /// <returns></returns>
    public static byte[] Short2Bytes(short _value, bool _isLittleEndian = false)
    {
        byte[] bytes = new byte[2];

        if (_isLittleEndian)
        {
            bytes[0] = (byte)(_value);
            bytes[1] = (byte)(_value >> 8);
        }
        else
        {
            bytes[0] = (byte)(_value >> 8);
            bytes[1] = (byte)(_value);

        }
        return bytes;
    }

    /// <summary>
    /// int をbyte 配列に変換
    /// </summary>
    /// <param name="_value"></param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    /// <returns></returns>
    public static byte[] Int2Bytes(int _value, bool _isLittleEndian = false)
    {
        byte[] bytes = new byte[4];

        if (_isLittleEndian)
        {
            bytes[0] = (byte)(_value);
            bytes[1] = (byte)(_value >> 8);
            bytes[2] = (byte)(_value >> 16);
            bytes[3] = (byte)(_value >> 24);
        }
        else
        {
            bytes[0] = (byte)(_value >> 24);
            bytes[1] = (byte)(_value >> 16);
            bytes[2] = (byte)(_value >> 8);
            bytes[3] = (byte)(_value);
        }
        return bytes;
    }

    /// <summary>
    /// int をbyte 配列に変換
    /// </summary>
    /// <param name="_value"></param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    /// <returns></returns>
    public static byte[] Float2Bytes(float _value, bool _isLittleEndian = false)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        if (!_isLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return bytes;
    }
    /// <summary>
    /// 対象のBinary のoffset 位置から byte の値を書き込む
    /// </summary>
    /// <param name="_value">書き込む値</param>
    /// <param name="_retArray">対象のbyte 配列</param>
    /// <param name="_offset">Write開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    public static void WriteByte(byte _value, ref byte[] _retArray, ref int _offset, bool _isLittleEndian = false)
    {
        if (_offset + sizeof(byte) > _retArray.Length)
        {
            Debug.LogError("Out of Range");
            return;
        }
        _retArray[_offset++] = _value;

    }

    /// <summary>
    /// 対象のBinary のoffset 位置から int の値を書き込む
    /// </summary>
    /// <param name="_value">書き込む値</param>
    /// <param name="_retArray">対象のbyte 配列</param>
    /// <param name="_offset">Write開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    public static void WriteIntToBinary(int _value, ref byte[] _retArray, ref int _offset, bool _isLittleEndian = false)
    {
        var intBinary = Int2Bytes(_value, _isLittleEndian);
        if (_offset + intBinary.Length > _retArray.Length)
        {
            Debug.LogError("Out of Range");
            return;
        }
        for (int i = 0; i < intBinary.Length; i++)
        {
            _retArray[_offset++] = intBinary[i];
        }
    }

    /// <summary>
    /// 対象のBinary のoffset 位置から short の値を書き込む
    /// </summary>
    /// <param name="_value">書き込む値</param>
    /// <param name="_retArray">対象のbyte 配列</param>
    /// <param name="_offset">Write開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    public static void WriteShortToBinary(short _value, ref byte[] _retArray, ref int _offset, bool _isLittleEndian = false)
    {
        var shortBinary = Short2Bytes(_value);
        if (_offset + shortBinary.Length > _retArray.Length)
        {
            Debug.LogError("Out of Range");
            return;
        }
        for (int i = 0; i < shortBinary.Length; i++)
        {
            _retArray[_offset++] = shortBinary[i];
        }
    }


    /// <summary>
    /// 対象のBinary のoffset 位置から short の値を書き込む
    /// </summary>
    /// <param name="_value">書き込む値</param>
    /// <param name="_retArray">対象のbyte 配列</param>
    /// <param name="_offset">Write開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    public static void WriteFloatToBinary(float _value, ref byte[] _retArray, ref int _offset, bool _isLittleEndian = false)
    {
        var floatBinary = Float2Bytes(_value);
        if (_offset + floatBinary.Length > _retArray.Length)
        {
            Debug.LogError("Out of Range");
            return;
        }
        for (int i = 0; i < floatBinary.Length; i++)
        {
            _retArray[_offset++] = floatBinary[i];
        }
    }


    /// <summary>
    /// 対象のBinary のoffset 位置から short の値を書き込む
    /// </summary>
    /// <param name="_value">書き込む値</param>
    /// <param name="_retArray">対象のbyte 配列</param>
    /// <param name="_offset">Write開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    public static void WriteVec3ToBinary(Vector3 _value, ref byte[] _retArray, ref int _offset, bool _isLittleEndian = false)
    {
        WriteFloatToBinary(_value.x, ref _retArray, ref _offset, _isLittleEndian);
        WriteFloatToBinary(_value.y, ref _retArray, ref _offset, _isLittleEndian);
        WriteFloatToBinary(_value.z, ref _retArray, ref _offset, _isLittleEndian);
    }

    /// <summary>
    /// 対象のBinary のoffset 位置から short の値を書き込む
    /// </summary>
    /// <param name="_value">書き込む値</param>
    /// <param name="_retArray">対象のbyte 配列</param>
    /// <param name="_offset">Write開始位置</param>
    /// <param name="_isLittleEndian">LittleEndianかどうか</param>
    public static void WriteQuaternionToBinary(Quaternion _value, ref byte[] _retArray, ref int _offset, bool _isLittleEndian = false)
    {
        WriteFloatToBinary(_value.x, ref _retArray, ref _offset, _isLittleEndian);
        WriteFloatToBinary(_value.y, ref _retArray, ref _offset, _isLittleEndian);
        WriteFloatToBinary(_value.z, ref _retArray, ref _offset, _isLittleEndian);
        WriteFloatToBinary(_value.w, ref _retArray, ref _offset, _isLittleEndian);
    }
    #endregion //) Serialize
}
