using UnityEngine;

// Камера від третьої особи: тримається позаду і вище цілі, Tab перемикає ціль.
public class FollowCamera : MonoBehaviour
{
    public Transform[] targets;
    public Vector3 offset = new Vector3(0f, 2.1f, -3.8f);
    public float smooth = 6f;
    int current;

    void LateUpdate()
    {
        if (targets == null || targets.Length == 0) return;
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            current = (current + 1) % targets.Length;
            for (int i = 0; i < targets.Length; i++)
            {
                var c = targets[i].GetComponent<CharacterAnimator>();
                if (c != null) c.readInput = i == current;
            }
        }
        Transform t = targets[current];
        Vector3 wanted = t.position + offset;
        transform.position = Vector3.Lerp(transform.position, wanted, smooth * Time.deltaTime);
        transform.LookAt(t.position + Vector3.up * 1.2f);
    }
}
