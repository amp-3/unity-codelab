using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsCounter
{
    // フレーム数をカウント
    private int frameCount = 0;
    // 計測開始時間
    private float elapsedTime = 0f;
    // FPS値
    private float fps = 0f;
    // 更新頻度（FPSを計算する間隔）
    public float updateInterval = 0.5f;

    public void OnUpdate()
    {
        // 経過時間をカウント
        elapsedTime += Time.deltaTime;
        frameCount++;

        // 一定間隔でFPSを計算
        if (elapsedTime >= updateInterval)
        {
            // FPSを計算
            fps = frameCount / elapsedTime;

            // カウンタをリセット
            frameCount = 0;
            elapsedTime = 0f;
        }
    }

    // FPSを取得するメソッド
    public float GetFps()
    {
        return fps;
    }
}
