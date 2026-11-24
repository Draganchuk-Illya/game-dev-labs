using UnityEngine;

// Безкінечний фон з паралаксом. Малюнок фону повторюється кожні period клітинок,
// тому шар можна непомітно пересувати за камерою на цілу кількість періодів.
public class LoopBackground : MonoBehaviour
{
    [SerializeField] private float period = 12f;     // ширина одного повтору фону в клітинках
    [SerializeField] private float parallax = 0.5f;  // 0 - фон стоїть на місці, 1 - їде разом з камерою

    private Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        Apply(cam.position.x);
    }

    public void Apply(float cameraX)
    {
        float shift = cameraX * parallax;                              // зсув від паралаксу
        float wrap = Mathf.Floor((cameraX - shift) / period) * period; // цілі періоди, які камера вже проїхала
        transform.position = new Vector3(shift + wrap, transform.position.y, transform.position.z);
    }

    public void Setup(float newPeriod, float newParallax)
    {
        period = newPeriod;
        parallax = newParallax;
    }
}
