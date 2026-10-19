using System.Collections;
using UnityEngine;

// Kari Kari: persigue, ataca y muere de un cuchillazo.
// No empuja al Player ni a otros Kari Kari: mantiene las colisiones con el suelo.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class LDLT_KariKariEnemigo : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform abuelo;

    [Header("Persecucion")]
    [Min(0f)] public float velocidad = 3.6f;
    [Min(0.2f)] public float distanciaAtaque = 1.35f;

    [Header("Garras")]
    [Min(0.25f)] public float segundosEntreAtaques = 1.5f;
    [Min(0f)] public float retrasoGolpe = 0.23f;

    [Header("Estados exactos del Animator")]
    public string estadoCorrer = "Correr";
    public string estadoAtacar = "Atacar";

    [Header("Efecto al recibir el cuchillazo")]
    [Min(0.05f)] public float duracionDesaparicion = 0.22f;

    private Rigidbody2D cuerpo;
    private Animator animador;
    private SpriteRenderer dibujo;
    private LDLT_VidaAbuelo vidaAbuelo;
    private LDLT_SpawnerKariKari generador;
    private float proximoAtaque;
    private float momentoGolpe;
    private float finAnimacionAtaque;
    private bool golpePendiente;
    private bool atacando;
    private bool eliminado;
    private string ultimoEstado = "";
    private bool avisoAnimator;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        animador = GetComponent<Animator>();
        dibujo = GetComponentInChildren<SpriteRenderer>();
        cuerpo.gravityScale = Mathf.Max(1f, cuerpo.gravityScale);
        cuerpo.constraints |= RigidbodyConstraints2D.FreezeRotation;
        cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Configurar(Transform jugador, LDLT_SpawnerKariKari spawner)
    {
        abuelo = jugador;
        generador = spawner;
        if (abuelo != null)
            vidaAbuelo = abuelo.GetComponent<LDLT_VidaAbuelo>();

        // Evita que la fisica del Kari Kari arrastre o empuje al abuelo.
        IgnorarColisionesConAbuelo();
        IgnorarColisionesConOtrosKariKari();
    }

    private void Start()
    {
        if (abuelo == null)
        {
            LDLT_VidaAbuelo vida = FindFirstObjectByType<LDLT_VidaAbuelo>();
            if (vida != null) abuelo = vida.transform;
        }

        if (abuelo != null && vidaAbuelo == null)
            vidaAbuelo = abuelo.GetComponent<LDLT_VidaAbuelo>();

        // Tambien sirve si se coloca un Kari Kari manualmente para probar.
        IgnorarColisionesConAbuelo();
        IgnorarColisionesConOtrosKariKari();
        Reproducir(estadoCorrer);
    }

    private void IgnorarColisionesConAbuelo()
    {
        if (abuelo == null) return;
        Collider2D[] collidersKari = GetComponentsInChildren<Collider2D>();
        Collider2D[] collidersPlayer = abuelo.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D kari in collidersKari)
        {
            if (kari == null) continue;
            foreach (Collider2D player in collidersPlayer)
            {
                if (player != null)
                    Physics2D.IgnoreCollision(kari, player, true);
            }
        }
    }

    private void IgnorarColisionesConOtrosKariKari()
    {
        Collider2D[] propios = GetComponentsInChildren<Collider2D>();
        LDLT_KariKariEnemigo[] otros =
            FindObjectsByType<LDLT_KariKariEnemigo>(FindObjectsSortMode.None);

        foreach (LDLT_KariKariEnemigo otro in otros)
        {
            if (otro == null || otro == this) continue;
            Collider2D[] ajenos = otro.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D a in propios)
            {
                if (a == null) continue;
                foreach (Collider2D b in ajenos)
                {
                    if (b != null) Physics2D.IgnoreCollision(a, b, true);
                }
            }
        }
    }

    private void Update()
    {
        if (eliminado || abuelo == null ||
            (vidaAbuelo != null && vidaAbuelo.EstaDerrotado))
            return;

        float distancia = Vector2.Distance(transform.position, abuelo.position);

        if (atacando)
        {
            if (golpePendiente && Time.time >= momentoGolpe)
            {
                golpePendiente = false;
                if (distancia <= distanciaAtaque + 0.5f && vidaAbuelo != null)
                    vidaAbuelo.RecibirDanio(1);
            }

            if (Time.time >= finAnimacionAtaque)
            {
                atacando = false;
                Reproducir(estadoCorrer);
            }
            return;
        }

        if (distancia <= distanciaAtaque && Time.time >= proximoAtaque)
        {
            atacando = true;
            golpePendiente = true;
            momentoGolpe = Time.time + retrasoGolpe;
            finAnimacionAtaque = Time.time + Mathf.Max(0.42f, retrasoGolpe + 0.15f);
            proximoAtaque = Time.time + segundosEntreAtaques;
            Reproducir(estadoAtacar);
        }
        else
        {
            Reproducir(estadoCorrer);
        }

        if (dibujo != null)
            dibujo.flipX = abuelo.position.x < transform.position.x;
    }

    private void FixedUpdate()
    {
        if (cuerpo == null || eliminado) return;

        Vector2 velocidadActual = cuerpo.linearVelocity;
        if (abuelo == null ||
            (vidaAbuelo != null && vidaAbuelo.EstaDerrotado) || atacando)
        {
            velocidadActual.x = 0f;
        }
        else
        {
            float distanciaX = abuelo.position.x - transform.position.x;
            velocidadActual.x = Mathf.Abs(distanciaX) > distanciaAtaque * 0.82f
                ? Mathf.Sign(distanciaX) * velocidad
                : 0f;
        }
        cuerpo.linearVelocity = velocidadActual;
    }

    private void Reproducir(string estado)
    {
        if (animador == null || string.IsNullOrEmpty(estado) || ultimoEstado == estado)
            return;

        int hash = Animator.StringToHash("Base Layer." + estado);
        if (animador.HasState(0, hash))
        {
            animador.Play(hash, 0, 0f);
            ultimoEstado = estado;
        }
        else if (!avisoAnimator)
        {
            Debug.LogWarning(
                "Kari Kari: el Animator no tiene el estado '" + estado + "'.",
                this);
            avisoAnimator = true;
        }
    }

    // Un cuchillazo basta. Notificamos primero al spawner para contar la baja.
    public void RecibirCuchillazo()
    {
        if (eliminado) return;
        eliminado = true;

        if (generador != null)
            generador.NotificarDerrota(this);

        if (cuerpo != null)
        {
            cuerpo.linearVelocity = Vector2.zero;
            cuerpo.simulated = false;
        }
        if (animador != null) animador.enabled = false;

        // La criatura deja de hacer dano desde el instante del impacto.
        StartCoroutine(Desaparecer());
    }

    private IEnumerator Desaparecer()
    {
        float duracion = Mathf.Max(0.05f, duracionDesaparicion);
        float tiempo = 0f;
        Vector3 escalaInicial = transform.localScale;
        Color colorInicial = dibujo != null ? dibujo.color : Color.white;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracion);

            if (dibujo != null)
            {
                Color color = Color.Lerp(colorInicial,
                    new Color(1f, 0.18f, 0.12f, 0f), progreso);
                dibujo.color = color;
            }
            transform.localScale = escalaInicial * (1f - 0.16f * progreso);
            yield return null;
        }
        Destroy(gameObject);
    }
}
