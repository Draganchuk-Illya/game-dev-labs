using UnityEngine;

// Стоїть на кожній машині і рахує її просування по трасі: які вейпоінти пройдено і яке зараз коло.
public class RaceCar : MonoBehaviour
{
    public string driverName = "Суперник";
    public bool isPlayer;
    public float waypointRadius = 16f;      // на такій відстані вейпоінт вважається пройденим

    public int Passed { get; private set; }         // скільки вейпоінтів пройдено від старту
    public int NextWaypoint { get; private set; }   // індекс вейпоінта, до якого машина їде зараз
    public bool Finished { get; private set; }
    public float FinishTime { get; private set; }

    Transform[] waypoints;
    int totalLaps;
    Rigidbody body;
    float stuckTime;
    public int Respawns { get; private set; }

    public void Setup(Transform[] points, int laps)
    {
        waypoints = points;
        totalLaps = laps;
        body = GetComponent<Rigidbody>();
        Passed = 0;
        NextWaypoint = 0;                   // вейпоінт 0 стоїть на лінії старту
    }

    // Номер поточного кола, починаючи з 1. До перетину лінії старту це коло 1.
    public int Lap
    {
        get { return Mathf.Clamp((Passed - 1) / waypoints.Length + 1, 1, totalLaps); }
    }

    // Чим більше значення, тим далі машина по дистанції. За ним визначається місце в гонці.
    public float Progress
    {
        get
        {
            Vector3 to = waypoints[NextWaypoint].position - transform.position;
            to.y = 0f;
            return Passed - to.magnitude / 1000f;
        }
    }

    // Якщо машина довго стоїть (вперлась у бар'єр або перекинулась), повертаю її на останній пройдений вейпоінт
    public void CheckStuck(float deltaTime)
    {
        if (Finished || waypoints == null) return;
        stuckTime = body.linearVelocity.magnitude < 1.5f ? stuckTime + deltaTime : 0f;
        if (stuckTime > 3f || (isPlayer && Input.GetKeyDown(KeyCode.Backspace))) Respawn();
    }

    public void Respawn()
    {
        Transform point = waypoints[(NextWaypoint - 1 + waypoints.Length) % waypoints.Length];
        transform.SetPositionAndRotation(point.position + Vector3.up * 0.6f, point.rotation);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        stuckTime = 0f;
        Respawns++;
    }

    public void Tick(float raceTime)
    {
        if (Finished || waypoints == null) return;

        Vector3 to = waypoints[NextWaypoint].position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > waypointRadius * waypointRadius) return;

        Passed++;
        NextWaypoint = (NextWaypoint + 1) % waypoints.Length;

        // лінію старту перетнуто (totalLaps + 1) разів: перший раз на старті, далі після кожного кола
        if (Passed - 1 >= totalLaps * waypoints.Length)
        {
            Finished = true;
            FinishTime = raceTime;
        }
    }
}
