using System.Collections.Generic;
using UnityEngine;

// Керування головним героєм: рух, стрибок, розворот у напрямку руху.
// Замінює граф Visual Scripting PlayerController.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumping = 16f;
    [SerializeField] private float groundDistance = 0.05f;

    private Rigidbody2D rb;
    private Animator animator;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // рух вліво і вправо
        float horizontal = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

        // стрибок дозволений тільки з землі
        bool grounded = IsGrounded();
        if (Input.GetButtonDown("Jump") && grounded)
        {
            rb.AddForce(new Vector2(0, jumping), ForceMode2D.Impulse);
        }

        // розворот: коли герой стоїть (horizontal = 0), масштаб не змінюю,
        // тому він лишається повернутим у бік останнього руху
        if (horizontal > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontal < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("Grounded", grounded);
        }
    }

    // Кидаю колайдер героя вниз на малу відстань. Якщо він у щось впирається, герой на землі.
    private bool IsGrounded()
    {
        return rb.Cast(Vector2.down, hits, groundDistance) > 0;
    }
}
