using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Aeroplane;

// Показує на екрані час польоту, швидкість і висоту літака.
public class FlightTimer : MonoBehaviour
{
    public Text label;                      // текст у лівому верхньому куті
    public AeroplaneController plane;

    public float FlightTime { get; private set; }
    bool running = true;

    void Update()
    {
        if (running) FlightTime += Time.deltaTime;

        int minutes = (int)(FlightTime / 60f);
        float seconds = FlightTime - minutes * 60;
        label.text = string.Format("Час польоту: {0:00}:{1:00.0}\nШвидкість: {2:0} км/год\nВисота: {3:0} м",
            minutes, seconds, plane.ForwardSpeed * 3.6f, plane.transform.position.y);
    }

    // після аварії час зупиняється
    public void Stop()
    {
        running = false;
    }
}
