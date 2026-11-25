using UnityEngine;
using UnityEngine.InputSystem;

// Керування героїнею Ruby: рух у чотирьох напрямках.
public class PlayerController : MonoBehaviour
{
    // дія введення: клавіші WASD, стрілки або лівий стік геймпада (налаштовується в Inspector)
    public InputAction MoveAction;

    // швидкість у клітинках за секунду
    public float speed = 3.0f;

    void Start()
    {
        // дію потрібно ввімкнути, інакше вона не зчитує клавіші
        MoveAction.Enable();
    }

    void Update()
    {
        Vector2 move = MoveAction.ReadValue<Vector2>();

        // множення на Time.deltaTime робить швидкість однаковою при будь-якій частоті кадрів
        Vector2 position = (Vector2)transform.position + move * speed * Time.deltaTime;
        transform.position = position;
    }
}
