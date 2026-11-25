using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Vehicles.Car;

// Керує гонкою: відлік перед стартом, кола, місця, час, фініш і перезапуск.
public class RaceManager : MonoBehaviour
{
    public WaypointCircuit circuit;
    public RaceCar[] cars;
    public int totalLaps = 3;

    public Text infoText;           // коло, місце, час, швидкість
    public Text centerText;         // відлік і результат

    public bool RaceStarted { get; private set; }
    public float RaceTime { get; private set; }

    RaceCar player;
    readonly List<RaceCar> finishOrder = new List<RaceCar>();

    void Start()
    {
        // кадри синхронізуються з монітором, щоб картинка не рвалась
        QualitySettings.vSyncCount = 1;
        foreach (var car in cars) car.Setup(circuit.Waypoints, totalLaps);
        player = cars.First(c => c.isPlayer);
        SetDrivers(false);
        Hold(true);
        StartCoroutine(Countdown());
    }

    // Вмикає або вимикає керування: гравця (PlayerDrive) і суперників (CarAIControl)
    void SetDrivers(bool on)
    {
        foreach (var car in cars)
        {
            var user = car.GetComponent<PlayerDrive>();
            if (user != null) user.enabled = on;
            var ai = car.GetComponent<CarAIControl>();
            if (ai != null) ai.enabled = on;
        }
    }

    // До старту машини заморожені, щоб не котились і не штовхали одна одну
    void Hold(bool on)
    {
        foreach (var car in cars)
        {
            var body = car.GetComponent<Rigidbody>();
            body.constraints = on ? RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY
                                  : RigidbodyConstraints.None;
            if (!on) body.WakeUp();
        }
    }

    IEnumerator Countdown()
    {
        for (int i = 3; i >= 1; i--)
        {
            centerText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        centerText.text = "Старт!";
        RaceStarted = true;
        Hold(false);
        SetDrivers(true);
        yield return new WaitForSeconds(1f);
        if (!player.Finished) centerText.text = "";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (RaceStarted && !player.Finished) RaceTime += Time.deltaTime;

        foreach (var car in cars)
        {
            bool was = car.Finished;
            car.Tick(RaceTime);
            if (RaceStarted) car.CheckStuck(Time.deltaTime);
            if (!was && car.Finished)
            {
                finishOrder.Add(car);
                Debug.Log("FINISH: " + car.driverName + " місце " + finishOrder.Count + " час " + car.FinishTime.ToString("F1"));
                if (car.isPlayer) PlayerFinished();
            }
        }

        float speed = player.GetComponent<Rigidbody>().linearVelocity.magnitude * 3.6f;
        infoText.text = string.Format("Коло: {0} / {1}\nМісце: {2} / {3}\nЧас: {4}\nШвидкість: {5:0} км/год",
            player.Lap, totalLaps, Place(player), cars.Length, Format(RaceTime), speed);
    }

    // Місце машини: ті, хто фінішував, стоять у порядку фінішу, решта - за пройденою дистанцією
    public int Place(RaceCar car)
    {
        if (car.Finished) return finishOrder.IndexOf(car) + 1;
        return finishOrder.Count + 1 + cars.Count(c => !c.Finished && c != car && c.Progress > car.Progress);
    }

    void PlayerFinished()
    {
        var user = player.GetComponent<PlayerDrive>();
        if (user != null) user.enabled = false;
        StartCoroutine(BrakePlayer());
        centerText.text = string.Format("Фініш!\nМісце: {0} з {1}\nЧас: {2}\nR - почати заново",
            finishOrder.Count, cars.Length, Format(player.FinishTime));
    }

    IEnumerator BrakePlayer()
    {
        var controller = player.GetComponent<CarController>();
        while (true)
        {
            controller.Move(0f, 0f, -1f, 0f);
            yield return new WaitForFixedUpdate();
        }
    }

    static string Format(float t)
    {
        int minutes = (int)(t / 60f);
        return string.Format("{0:00}:{1:00.0}", minutes, t - minutes * 60);
    }
}
