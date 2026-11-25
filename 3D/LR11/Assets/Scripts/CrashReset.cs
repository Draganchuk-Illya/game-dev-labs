using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Аварія літака: зіткнення зі скелею чи землею, падіння у воду або виліт за межі локації.
// Після аварії сцена завантажується заново.
[RequireComponent(typeof(Rigidbody))]
public class CrashReset : MonoBehaviour
{
    public float startSpeed = 45f;          // початкова швидкість, літак стартує в повітрі
    public float waterLevel = 84f;          // нижче цієї висоти літак вважається затопленим
    public float areaSize = 2400f;          // розмір локації, за її межами політ закінчується
    public float reloadDelay = 2f;          // пауза перед перезапуском сцени
    public Text message;                    // напис про аварію в центрі екрана
    public FlightTimer timer;
    public GameObject explosionPrefab;

    Rigidbody body;
    bool crashed;

    void Start()
    {
        body = GetComponent<Rigidbody>();
        body.linearVelocity = transform.forward * startSpeed;
        if (message != null) message.text = "";
    }

    void Update()
    {
        if (crashed) return;
        Vector3 p = transform.position;
        if (p.y < waterLevel) Crash("Літак упав у воду");
        else if (p.x < 0f || p.z < 0f || p.x > areaSize || p.z > areaSize) Crash("Літак вилетів за межі локації");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!crashed) Crash("Аварія: зіткнення (" + collision.gameObject.name + ")");
    }

    void Crash(string reason)
    {
        crashed = true;
        Debug.Log("CRASH: " + reason + " на " + (timer != null ? timer.FlightTime.ToString("F1") : "?") + " с польоту");
        if (timer != null) timer.Stop();
        if (message != null) message.text = reason + "\nПерезапуск...";
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Invoke(nameof(Reload), reloadDelay);
    }

    void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
