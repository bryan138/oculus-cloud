using UnityEngine;
using System.Collections.Generic;
using Pcx;

[ExecuteInEditMode]
public class PointAnimation : MonoBehaviour
{
    [SerializeField] PointCloudData _sourceData;
    [SerializeField] ComputeShader _computeShader;
    
    [SerializeField] GameObject leftHand;
    [SerializeField] GameObject rightHand;
    [SerializeField] GameObject leftHandSphere;
    [SerializeField] GameObject rightHandSphere;

    ComputeBuffer pointBuffer;
    ComputeBuffer velocitiesBuffer;
    ComputeBuffer timesBuffer;
    ComputeBuffer handsBuffer;

    float HOVER_SPEED = 0.01f;
    float BLACK_VOID = 4.0f;
    float STARRY_NIGHT = 1.0f;
    float HAND_SPHERE_STEP = 0.015f;
    bool SHOW_SPHERES = false;

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
        if (handsBuffer != null) {
            handsBuffer.Release();
            handsBuffer = null;
        }
    }

    void Update() {
        if (_sourceData == null) return;

        var sourceBuffer = _sourceData.computeBuffer;

        float scale = gameObject.transform.localScale.x;
        float scaleFactor = 1.0f / scale;
        float explodingRange = scaleFactor * 15;

        if (pointBuffer == null || pointBuffer.count != sourceBuffer.count) {
            if (pointBuffer != null) {
                pointBuffer.Release();
                velocitiesBuffer.Release();
                timesBuffer.Release();
                handsBuffer.Release();
            }
            pointBuffer = new ComputeBuffer(sourceBuffer.count, PointCloudData.elementSize);
            velocitiesBuffer = new ComputeBuffer(sourceBuffer.count, sizeof(float) * 3);
            timesBuffer = new ComputeBuffer(sourceBuffer.count, sizeof(float));
            handsBuffer = new ComputeBuffer(10, sizeof(float));

            int count = sourceBuffer.count;
            Point[] startingPositions = new Point[count];
            Vector3[] velocities = new Vector3[count];
            float[] times = new float[count];
            for (int i = 0; i < count;) {
                // Generate random starting positions
                startingPositions[i] = new Point {
                    position = new Vector3(Random.Range(-explodingRange, explodingRange), Random.Range(-explodingRange, explodingRange), Random.Range(0, explodingRange * 2)),
                    color = 255
                };

                // Initialize velocity
                velocities[i] = new Vector3(Random.Range(-HOVER_SPEED, HOVER_SPEED), Random.Range(-HOVER_SPEED, HOVER_SPEED), Random.Range(-HOVER_SPEED, HOVER_SPEED));

                // Generate random completion times
                for (int j = 0; j < 50 && i < sourceBuffer.count; j++, i++) {
                    float randomTime = Random.Range(0.0f, STARRY_NIGHT);
                    times[i] = randomTime;
                }
            }

            pointBuffer.SetData(startingPositions);
            velocitiesBuffer.SetData(velocities);
            timesBuffer.SetData(times);
        }

        // Get oculus touch input data
        float leftTrigger = OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger);
        float rightTrigger = OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);
        float leftForce = leftTrigger * 0.2f * scaleFactor;
        float rightForce = rightTrigger * 0.2f * scaleFactor;

        float leftHandRadius = processHandSphere(leftHandSphere, true);
        float rightHandRadius = processHandSphere(rightHandSphere, false);

        // Compose hand data buffer
        Vector3 leftHandPosition = transform.InverseTransformPoint(leftHand.transform.position);
        Vector3 rightHandPosition = transform.InverseTransformPoint(rightHand.transform.position);
        float[] hands = { leftHandPosition.x, leftHandPosition.y, leftHandPosition.z, leftHandRadius, leftForce, rightHandPosition.x, rightHandPosition.y, rightHandPosition.z, rightHandRadius, rightForce };
        handsBuffer.SetData(hands);

        var time = Application.isPlaying ? Time.time : 0;
        var kernel = _computeShader.FindKernel("Main");

        _computeShader.SetFloat("BLACK_VOID", BLACK_VOID);
        _computeShader.SetFloat("STARRY_NIGHT", STARRY_NIGHT);
        _computeShader.SetFloat("time", time);
        _computeShader.SetFloat("scale", scale);

        _computeShader.SetBuffer(kernel, "Velocities", velocitiesBuffer);
        _computeShader.SetBuffer(kernel, "Times", timesBuffer);
        _computeShader.SetBuffer(kernel, "Hands", handsBuffer);

        _computeShader.SetBuffer(kernel, "SourceBuffer", sourceBuffer);
        _computeShader.SetBuffer(kernel, "OutputBuffer", pointBuffer);

        _computeShader.Dispatch(kernel, sourceBuffer.count / 128, 1, 1);

        GetComponent<PointCloudRenderer>().sourceBuffer = pointBuffer;
    }

    float processHandSphere(GameObject hand, bool left) {
        float radius = hand.transform.localScale.x;
        bool valueChanged = false;

        if (OVRInput.Get(left ? OVRInput.RawButton.Y : OVRInput.RawButton.B)) {
            valueChanged = true;
            radius += HAND_SPHERE_STEP;
        } else if (OVRInput.Get(left ? OVRInput.RawButton.X : OVRInput.RawButton.A)) {
            valueChanged = true;
            radius = Mathf.Max(0.1f, radius - HAND_SPHERE_STEP);
        }

        if (valueChanged) {
            hand.SetActive(SHOW_SPHERES);
            hand.transform.localScale = new Vector3(radius, radius, radius);
        } else {
            hand.SetActive(false);
        }

        return radius / 2.0f;
    }
}
