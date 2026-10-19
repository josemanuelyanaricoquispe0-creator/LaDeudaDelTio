using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Controla el ataque rapido del cuchillo del Abuelo Chaman.
// Conserva el movimiento original de SpriteCook.
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(200)]
public class AbueloAtaqueCuchillo : MonoBehaviour
{
    [Header("Configuracion del cuchillo")]
    [SerializeField] private bool cuchilloDesbloqueado = true;

    [SerializeField] private float duracionSiNoHayClip = 0.7f;

    [Header("Velocidad del cuchillo")]
    [Range(1f, 3f)]
    [SerializeField] private float velocidadAtaque = 1.6f;

    private Animator animator;
    private SpriteCookPlayerMovement movimiento;
    private SpriteCookAnimationController animacionesSpriteCook;
    private Rigidbody2D cuerpo;
    private PlayerControlLock controlLock;

    private bool movimientoEstabaHabilitado;
    private bool atacando;

    public bool IsAttacking => atacando;

    private float finAtaque;
    private float velocidadOriginalAnimator = 1f;

    private const string EstadoAtaque = "Base Layer.Attack";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movimiento = GetComponent<SpriteCookPlayerMovement>();
        animacionesSpriteCook = GetComponent<SpriteCookAnimationController>();
        cuerpo = GetComponent<Rigidbody2D>();
        controlLock = GetComponent<PlayerControlLock>();
    }

    private void Update()
    {
        if (!atacando)
        {
            if ((controlLock != null && controlLock.BlocksInput) ||
                (movimiento != null && !movimiento.isActiveAndEnabled))
                return;

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
            Debug.LogWarning(
                "No se encontro la animacion Attack del Abuelo.",
                this
            );
            return;
        }

        atacando = true;

        // Bloqueamos temporalmente el movimiento.
        if (controlLock != null)
        {
            controlLock.Acquire(this);
        }
        else if (movimiento != null)
        {
            movimientoEstabaHabilitado = movimiento.enabled;
            movimiento.enabled = false;
        }

        // Detenemos el desplazamiento horizontal.
        if (cuerpo != null)
        {
            Vector2 velocidad = cuerpo.linearVelocity;
            velocidad.x = 0f;
            cuerpo.linearVelocity = velocidad;
        }

        // Guardamos la velocidad original del Animator.
        velocidadOriginalAnimator = animator.speed;

        // Aumentamos la velocidad solamente durante el ataque.
        animator.speed = Mathf.Max(0.1f, velocidadOriginalAnimator)
                         * velocidadAtaque;

        // Reproducimos el cuchillazo desde el principio.
        animator.Play(EstadoAtaque, 0, 0f);

        // Calculamos la duracion segun la nueva velocidad.
        finAtaque = Time.time + DuracionRealDelClip();
    }

    private float DuracionRealDelClip()
    {
        var controller = animator.runtimeAnimatorController;

        if (controller != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip != null &&
                    clip.name.EndsWith(
                        "_attack",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Mathf.Max(
                        0.1f,
                        clip.length / Mathf.Max(0.1f, animator.speed)
                    );
                }
            }
        }

        return Mathf.Max(
            0.1f,
            duracionSiNoHayClip / Mathf.Max(0.1f, animator.speed)
        );
    }

    private void TerminarAtaque()
    {
        atacando = false;

        // Recuperamos la velocidad normal de las animaciones.
        if (animator != null)
        {
            animator.speed = velocidadOriginalAnimator;
        }

        // Devolvemos las animaciones normales de SpriteCook.
        if (animacionesSpriteCook != null)
        {
            animacionesSpriteCook.SetWalk();
            animacionesSpriteCook.SetIdle();
        }
        else if (animator != null)
        {
            animator.Play("Base Layer.Idle", 0, 0f);
        }

        // Devolvemos el control al jugador.
        if (controlLock != null)
        {
            controlLock.Release(this);
        }
        else if (movimiento != null)
        {
            movimiento.enabled = movimientoEstabaHabilitado;
        }
    }

    private void OnDisable()
    {
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