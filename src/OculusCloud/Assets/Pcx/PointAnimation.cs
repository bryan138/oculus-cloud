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

    ComputeBuffer _pointBuffer;
    ComputeBuffer timesBuffer;

    void OnDisable()
    {
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
            _pointBuffer = null;
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

        if (_pointBuffer == null || _pointBuffer.count != sourceBuffer.count)
        {
            if (_pointBuffer != null) {
                _pointBuffer.Release();
                timesBuffer.Release();
            }
            _pointBuffer = new ComputeBuffer(sourceBuffer.count, PointCloudData.elementSize);

            float[] times = new float[sourceBuffer.count];
            for (int i = 0; i < sourceBuffer.count;) {
                float randomTime = Random.Range(2.0f, _param1);
                for (int j = 0; j < 50 && i < sourceBuffer.count; j++, i ++) {
                    times[i] = randomTime;
                }
            }

            timesBuffer = new ComputeBuffer(sourceBuffer.count, sizeof(float), ComputeBufferType.Default);
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

        _computeShader.SetBuffer(kernel, "Times", timesBuffer);
        _computeShader.SetBuffer(kernel, "SourceBuffer", sourceBuffer);
        _computeShader.SetBuffer(kernel, "OutputBuffer", _pointBuffer);

        _computeShader.Dispatch(kernel, sourceBuffer.count / 128, 1, 1);

        GetComponent<PointCloudRenderer>().sourceBuffer = _pointBuffer;
    }
}
