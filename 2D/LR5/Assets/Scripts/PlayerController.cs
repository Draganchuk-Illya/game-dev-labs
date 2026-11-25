using UnityEngine;
using UnityEngine.InputSystem;

// Керування героїнею Ruby: рух через фізику і система здоров'я.
public class PlayerController : MonoBehaviour
{
    // рух
    public InputAction MoveAction;
    public float speed = 3.0f;
    Rigidbody2D rigidbody2d;
    Vector2 move;

    // здоров'я
    public int maxHealth = 5;
    int currentHealth;
    public int health { get { return currentHealth; } }

    // тимчасова невразливість після отримання шкоди
    public float timeInvincible = 2.0f;
    bool isInvincible;
    float damageCooldown;

    void Start()
    {
        MoveAction.Enable();
        rigidbody2d = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    void Update()
    {
        move = MoveAction.ReadValue<Vector2>();

        // відлік часу невразливості
        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;
            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }
    }

    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2d.position + move * speed * Time.deltaTime;
        rigidbody2d.MovePosition(position);
    }

    // amount > 0 лікує, amount < 0 завдає шкоди
    public void ChangeHealth(int amount)
    {
        if (amount < 0)
        {
            // під час невразливості шкода не проходить
            if (isInvincible)
            {
                return;
            }
            isInvincible = true;
            damageCooldown = timeInvincible;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UIHandler.instance.SetHealthValue(currentHealth / (float)maxHealth);
    }
}
