using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsCounter
{
    // �t���[�������J�E���g
    private int frameCount = 0;
    // �v���J�n����
    private float elapsedTime = 0f;
    // FPS�l
    private float fps = 0f;
    // �X�V�p�x�iFPS���v�Z����Ԋu�j
    public float updateInterval = 0.5f;

    public void OnUpdate()
    {
        // �o�ߎ��Ԃ��J�E���g
        elapsedTime += Time.deltaTime;
        frameCount++;

        // ���Ԋu��FPS���v�Z
        if (elapsedTime >= updateInterval)
        {
            // FPS���v�Z
            fps = frameCount / elapsedTime;

            // �J�E���^�����Z�b�g
            frameCount = 0;
            elapsedTime = 0f;
        }
    }

    // FPS���擾���郁�\�b�h
    public float GetFps()
    {
        return fps;
    }
}
