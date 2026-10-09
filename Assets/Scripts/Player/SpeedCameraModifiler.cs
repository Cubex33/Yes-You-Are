using Unity.Cinemachine;
using UnityEngine;

public class SpeedCameraModifiler : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera cam;
    public Lucy.Core.Movement movement;

    [Header("FOV")]
    public float baseFov = 60f;
    public float maxFov = 75f;

    [Header("Speed Tuning")]
    public float speedThreshold = 5.2f;   // ниже этого значения FOV не меняется
    public float fullFovSpeed = 8f;        // на этой скорости достигается maxFov

    [Header("Smoothing")]
    public float increaseSmooth = 10f;
    public float decreaseSmooth = 6f;

    private float currentFov;

    private void Awake()
    {
        currentFov = baseFov;
        ApplyFov(currentFov);
    }

    private void LateUpdate()
    {
        if (cam == null || movement == null) return;

        float speed = movement.currentHorizontalVelocity.magnitude;

        float targetFov = baseFov;

        if (speed > speedThreshold)
        {
            float t = Mathf.InverseLerp(speedThreshold, fullFovSpeed, speed);
            t = Mathf.SmoothStep(0f, 1f, t);
            targetFov = Mathf.Lerp(baseFov, maxFov, t);
        }

        float smooth = targetFov > currentFov ? increaseSmooth : decreaseSmooth;
        currentFov = Mathf.Lerp(currentFov, targetFov, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        ApplyFov(currentFov);
    }

    private void ApplyFov(float fov)
    {
        var lens = cam.Lens;
        lens.FieldOfView = fov;
        cam.Lens = lens;
    }
}