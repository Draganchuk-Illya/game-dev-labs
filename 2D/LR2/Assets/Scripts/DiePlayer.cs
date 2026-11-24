using UnityEngine;
using UnityEngine.SceneManagement;

// Якщо герой упав нижче заданої висоти, сцена запускається заново.
// Замінює граф Visual Scripting DeiPlayer.
public class DiePlayer : MonoBehaviour
{
    [SerializeField] private float dieBelowValue = -10f;

    private void Update()
    {
        if (transform.position.y < dieBelowValue)
        {
            SceneManager.LoadScene(0);
        }
    }
}
