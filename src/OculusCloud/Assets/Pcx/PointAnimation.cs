using UnityEngine;
using System.Collections.Generic;
using Pcx;

[ExecuteInEditMode]
public class PointAnimation : MonoBehaviour
{
    [SerializeField] PointCloudData _sourceData;
    [SerializeField] ComputeShader _computeShader;

    [SerializeField] float _param1;
    [SerializeField] float _param2;
    [SerializeField] float _param3;
    [SerializeField] float _param4;

    ComputeBuffer pointBuffer;
    ComputeBuffer timesBuffer;
    ComputeBuffer velocitiesBuffer;

    float HOVER_SPEED = 0.01f;

    struct Point {
        public Vector3 position;
        public uint color;
    }

    void OnDisable()
    {
        if (pointBuffer != null) {
            pointBuffer.Release();
            pointBuffer = null;
        }
        if (velocitiesBuffer != null) {
            velocitiesBuffer.Release();
            velocitiesBuffer = null;
        }
        if (timesBuffer != null) {
            timesBuffer.Release();
            timesBuffer = null;
        }
    }

    void Update()
    {
        if (_sourceData == null) return;

        var sourceBuffer = _sourceData.computeBuffer;

        if (pointBuffer == null || pointBuffer.count != sourceBuffer.count)
        {
            if (pointBuffer != null) {
                pointBuffer.Release();
                timesBuffer.Release();
            }
            pointBuffer = new ComputeBuffer(sourceBuffer.count, PointCloudData.elementSize);
            velocitiesBuffer = new ComputeBuffer(sourceBuffer.count, sizeof(float) * 3);
            timesBuffer = new ComputeBuffer(sourceBuffer.count, sizeof(float), ComputeBufferType.Default);

            int count = sourceBuffer.count;
            Point[] startingPositions = new Point[count];
            Vector3[] velocities = new Vector3[count];
            float[] times = new float[count];
            for (int i = 0; i < count; ) {
                // Generate random starting positions
                startingPositions[i] = new Point {
                    position = new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(0, 150)),
                    color = 255
                };

                // Initialize velocity
                velocities[i] = new Vector3(Random.Range(-HOVER_SPEED, HOVER_SPEED), Random.Range(-HOVER_SPEED, HOVER_SPEED), Random.Range(-HOVER_SPEED, HOVER_SPEED));

                // Generate random completion times
                for (int j = 0; j < 50 && i < sourceBuffer.count; j++, i++) {
                    float randomTime = Random.Range(1.0f, 10.0f);
                    times[i] = randomTime;
                }
            }

            pointBuffer.SetData(startingPositions);
            velocitiesBuffer.SetData(velocities);
            timesBuffer.SetData(times);
        }

        var time = Application.isPlaying ? Time.time : 0;
        var kernel = _computeShader.FindKernel("Main");

        _computeShader.SetFloat("Param1", _param1);
        _computeShader.SetFloat("Param2", _param2);
        _computeShader.SetFloat("Param3", _param3);
        _computeShader.SetFloat("Param4", _param4);
        _computeShader.SetFloat("Time", time);
        _computeShader.SetInt("PointCount", sourceBuffer.count);

        _computeShader.SetBuffer(kernel, "Velocities", velocitiesBuffer);
        _computeShader.SetBuffer(kernel, "Times", timesBuffer);

        _computeShader.SetBuffer(kernel, "SourceBuffer", sourceBuffer);
        _computeShader.SetBuffer(kernel, "OutputBuffer", pointBuffer);

        _computeShader.Dispatch(kernel, sourceBuffer.count / 128, 1, 1);

        GetComponent<PointCloudRenderer>().sourceBuffer = pointBuffer;
    }
}
