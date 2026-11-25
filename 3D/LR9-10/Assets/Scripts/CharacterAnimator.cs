using UnityEngine;

// Керує персонажем і передає параметри в Animator Controller.
// WASD - рух, пробіл - стрибок, ліва кнопка миші або F - удар, H - отримати удар, K - впасти, R - піднятись.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class CharacterAnimator : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    public float turnSpeed = 540f;
    public bool readInput = true;      // у сцені з двома персонажами керується тільки вибраний

    Animator animator;
    CharacterController body;
    float verticalSpeed;
    bool dead;

    void Start()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 input = Vector3.zero;
        if (readInput && !dead)
        {
            input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (Input.GetButtonDown("Jump")) animator.SetTrigger("Jump");
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F)) animator.SetTrigger("Attack");
            if (Input.GetKeyDown(KeyCode.H)) animator.SetTrigger("Hit");
            if (Input.GetKeyDown(KeyCode.K)) SetDead(true);
        }
        if (readInput && dead && Input.GetKeyDown(KeyCode.R)) SetDead(false);

        Move(input.normalized);
    }

    // Рух і поворот у напрямку руху. Параметр Speed плавно змінюється, щоб Blend Tree не перемикався ривком.
    public void Move(Vector3 direction)
    {
        bool moving = direction.sqrMagnitude > 0.01f && !dead;
        if (moving)
        {
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
        animator.SetFloat("Speed", moving ? 1f : 0f, 0.12f, Time.deltaTime);

        verticalSpeed = body.isGrounded ? -1f : verticalSpeed + Physics.gravity.y * Time.deltaTime;
        Vector3 velocity = (moving ? direction * moveSpeed : Vector3.zero) + Vector3.up * verticalSpeed;
        body.Move(velocity * Time.deltaTime);
    }

    public void SetDead(bool value)
    {
        dead = value;
        animator.SetBool("Dead", value);
    }
}
