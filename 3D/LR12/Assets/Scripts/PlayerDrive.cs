using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

// Керування машиною гравця замість стандартного CarUserControl:
// кермо повертається плавно, а на великій швидкості кут повороту зменшується.
[RequireComponent(typeof(CarController))]
public class PlayerDrive : MonoBehaviour
{
    public float steerSpeed = 2.2f;         // як швидко кермо йде до потрібного положення (за секунду)
    public float returnSpeed = 4f;          // як швидко кермо повертається в центр
    public float highSpeed = 35f;           // швидкість (м/с), на якій кермо найменш чутливе
    public float minSteerAtSpeed = 0.35f;   // частка повного кута на великій швидкості

    CarController car;
    Rigidbody body;
    float steer;

    void Awake()
    {
        car = GetComponent<CarController>();
        body = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxis("Vertical");
        float handbrake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // чим швидше їде машина, тим менший максимальний кут керма
        float limit = Mathf.Lerp(1f, minSteerAtSpeed, body.linearVelocity.magnitude / highSpeed);
        float target = h * limit;
        float rate = Mathf.Abs(target) > Mathf.Abs(steer) ? steerSpeed : returnSpeed;
        steer = Mathf.MoveTowards(steer, target, rate * Time.fixedDeltaTime);

        car.Move(steer, v, v, handbrake);
    }
}
