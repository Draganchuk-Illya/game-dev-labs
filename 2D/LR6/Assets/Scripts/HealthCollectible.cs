using UnityEngine;

// Предмет, що відновлює одну одиницю здоров'я.
public class HealthCollectible : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController controller = other.GetComponent<PlayerController>();

        // предмет зникає тільки тоді, коли здоров'я неповне
        if (controller != null && controller.health < controller.maxHealth)
        {
            controller.ChangeHealth(1);
            Destroy(gameObject);
        }
    }
}
