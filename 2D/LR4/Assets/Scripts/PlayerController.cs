using UnityEngine;
using UnityEngine.InputSystem;

// Керування героїнею Ruby: рух через фізику, щоб вона не проходила крізь перешкоди.
public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public float speed = 3.0f;

    Rigidbody2D rigidbody2d;
    Vector2 move;

    void Start()
    {
        MoveAction.Enable();
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    // введення зчитую кожен кадр
    void Update()
    {
        move = MoveAction.ReadValue<Vector2>();
    }

    // FixedUpdate викликається з тією ж частотою, що й фізика, тому рухаю тіло тут
    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2d.position + move * speed * Time.deltaTime;
        rigidbody2d.MovePosition(position);
    }
}
