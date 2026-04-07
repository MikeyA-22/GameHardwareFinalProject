using UnityEngine;
using UnityEngine.Animations.Rigging;


[ExecuteAlways]
public class ElbowHintDriver : MonoBehaviour
{
    public Transform shoulder;
    public Transform elbow;
    public Transform hintTarget;
    public float hintDistance = 0.4f;

    void LateUpdate()
    {
        Vector3 armDir = (elbow.position - shoulder.position).normalized;
        Vector3 reference = Vector3.up;

        // Avoid gimbal when arm points straight up or down
        if (Mathf.Abs(Vector3.Dot(armDir, Vector3.up)) > 0.9f)
            reference = Vector3.forward;

        Vector3 hintDir = Vector3.Cross(armDir, reference).normalized;
        hintTarget.position = elbow.position - hintDir * hintDistance;
    }
}