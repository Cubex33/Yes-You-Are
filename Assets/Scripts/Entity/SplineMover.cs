using UnityEngine;
using UnityEngine.Splines;

public class SplineMover : MonoBehaviour
{
    public SplineContainer splineContainer;
    public float speed = 5f;
    private float t = 0f; 

    void Update()
    {
        if (splineContainer == null) return;
        t += speed * Time.deltaTime / splineContainer.CalculateLength();
        if (t > 1f) t = 0f; 
        Vector3 position = splineContainer.EvaluatePosition(t);
        Vector3 direction = splineContainer.EvaluateTangent(t);
        transform.position = position;
        transform.forward = direction;
    }
}