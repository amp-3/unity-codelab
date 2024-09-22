using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ArrayUtil
{
    /// <summary>
    /// Array.Reverse()の高速処理版
    /// 
    /// GCを発生させない代わりに、スレッド間で同時にアクセスしないことが保証される一時処理用のリストを必要とする
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array">要素の並びを逆順に入れ替えたい配列。本処理実行後、要素が置き換わっていることに注意</param>
    /// <param name="workerList">作業用のリスト。GCを発生させないために繰り返し使うリスト。中身はクリアされることに注意。また、スレッド間で同時に実行すると意図しない結果を生むため実行してはならない</param>
    public static void FastReverse<T>(T[] array, List<T> workerList)
    {
        if (array.IsNullOrEmpty()) return;

        if (workerList == null) workerList = new List<T>();
        workerList.Clear();

        //要素を逆順に作業用リストに詰め込む
        for (int i = array.Length - 1; i >= 0; i--)
        {
            workerList.Add(array[i]);
        }

        //逆順にした要素を元の配列に格納する
        for (int i = 0; i < workerList.Count; i++)
        {
            array[i] = workerList[i];
        }

        //処理終わりにClearしておく
        workerList.Clear();
    }
}
