using UnityEngine;

// Камера позаду машини гравця: плавно повертається разом з нею.
public class CarCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 7.5f;
    public float height = 3f;
    public float smooth = 5f;

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
        transform.LookAt(target.position + target.forward * 5f + Vector3.up * 0.8f);
    }
}
