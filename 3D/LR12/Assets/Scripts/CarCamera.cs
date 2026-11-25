using UnityEngine;

// Камера позаду машини гравця: плавно доганяє її і плавно повертається разом з нею.
public class CarCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 7.5f;
    public float height = 3f;
    public float followTime = 0.12f;        // час згладжування позиції
    public float turnSpeed = 3.5f;          // як швидко камера заходить за машину після повороту

    Vector3 velocity;
    Vector3 back;                           // згладжений напрямок "назад від машини"

    void Start()
    {
        if (target == null) return;
        back = Flat(target.forward);
        transform.position = Wanted();
        transform.LookAt(LookPoint());
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }
    Vector3 Wanted() { return target.position - back * distance + Vector3.up * height; }
    Vector3 LookPoint() { return target.position + back * 5f + Vector3.up * 0.8f; }

    void LateUpdate()
    {
        if (target == null) return;
        // напрямок камери повертається повільніше за машину, тому різкі рухи керма не смикають картинку
        back = Vector3.Slerp(back, Flat(target.forward), turnSpeed * Time.deltaTime);
        transform.position = Vector3.SmoothDamp(transform.position, Wanted(), ref velocity, followTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(LookPoint() - transform.position), 10f * Time.deltaTime);
    }
}
