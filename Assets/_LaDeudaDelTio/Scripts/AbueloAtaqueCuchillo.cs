using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Complemento del Player de SpriteCook. No modifica el controlador original.
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(200)]
public class AbueloAtaqueCuchillo : MonoBehaviour
{
    [Header("Prueba del cuchillo")]
    [SerializeField] private bool cuchilloDesbloqueado = true;
    [SerializeField] private float duracionSiNoHayClip = 0.7f;

    private Animator animator;
    private SpriteCookPlayerMovement movimiento;
    private SpriteCookAnimationController animacionesSpriteCook;
    private Rigidbody2D cuerpo;
    private bool atacando;
    private float finAtaque;
    private const string EstadoAtaque = "Base Layer.Attack";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movimiento = GetComponent<SpriteCookPlayerMovement>();
        animacionesSpriteCook = GetComponent<SpriteCookAnimationController>();
        cuerpo = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!atacando)
        {
            if (cuchilloDesbloqueado && PulsaronJ())
            {
                IniciarAtaque();
            }
            return;
        }

        if (Time.time >= finAtaque)
        {
            TerminarAtaque();
        }
    }

    private void IniciarAtaque()
    {
        if (animator.runtimeAnimatorController == null ||
            !animator.HasState(0, Animator.StringToHash(EstadoAtaque)))
        {
            Debug.LogWarning("No se encontró el estado Attack en el Animator del Abuelo. Revisa Controller = Character y la animación Attack.", this);
            return;
        }

        atacando = true;

        // El controlador de movimiento de SpriteCook cambia a Idle/Walk/Run
        // en cada Update. Se suspende SOLO durante la cuchillada.
        if (movimiento != null)
        {
            movimiento.enabled = false;
        }

        if (cuerpo != null)
        {
            Vector2 velocidad = cuerpo.linearVelocity;
            velocidad.x = 0f;
            cuerpo.linearVelocity = velocidad;
        }

        animator.Play(EstadoAtaque, 0, 0f);
        finAtaque = Time.time + DuracionRealDelClip();
    }

    private float DuracionRealDelClip()
    {
        var controller = animator.runtimeAnimatorController;
        if (controller != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip != null && clip.name.EndsWith("_attack", StringComparison.OrdinalIgnoreCase))
                {
                    return Mathf.Max(0.15f, clip.length);
                }
            }
        }
        return Mathf.Max(0.15f, duracionSiNoHayClip);
    }

    private void TerminarAtaque()
    {
        atacando = false;

        // Forzamos que SpriteCook actualice su estado interno y vuelva a Idle;
        // después su controlador normal recupera Walk/Run/Jump cuando proceda.
        if (animacionesSpriteCook != null)
        {
            animacionesSpriteCook.SetWalk();
            animacionesSpriteCook.SetIdle();
        }
        else
        {
            animator.Play("Base Layer.Idle", 0, 0f);
        }

        if (movimiento != null)
        {
            movimiento.enabled = true;
        }
    }

    private void OnDisable()
    {
        // Si el componente se desactiva durante una prueba, devolver el control.
        if (atacando)
        {
            TerminarAtaque();
        }
    }

    private static bool PulsaronJ()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.jKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.J);
#else
        return false;
#endif
    }
}
