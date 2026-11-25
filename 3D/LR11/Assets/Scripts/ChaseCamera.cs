using UnityEngine;

// Камера летить позаду літака і плавно доганяє його.
public class ChaseCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 16f;
    public float height = 5f;
    public float smooth = 4f;

    void Start()
    {
        if (target != null) transform.position = Wanted();
    }

    Vector3 Wanted()
    {
        Vector3 back = target.forward; back.y = 0f; back.Normalize();
        return target.position - back * distance + Vector3.up * height;
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.Lerp(transform.position, Wanted(), smooth * Time.deltaTime);
        transform.LookAt(target.position + target.forward * 6f);
    }
}
