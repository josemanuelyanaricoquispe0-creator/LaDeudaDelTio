using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Controlador propio del juego. No modifica los scripts originales de SpriteCook.
[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class YatiriJugador : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaminar = 3f;
    public float velocidadCorrer = 6f;
    public float fuerzaSalto = 9.5f;
    public float velocidadAgachado = 1.3f;
    public LayerMask capasSuelo = ~0;
    public float distanciaSuelo = 0.08f;

    [Header("Acciones")]
    public bool cuchilloDesbloqueado = false;
    public float tiempoEntreAtaques = 0.25f;
    public float alcanceCuchillo = 2.3f;
    public int danioMagia = 1;
    public int danioCuchillo = 1;
    public float velocidadProyectil = 11f;
    [Min(1)] public int vidaMaxima = 5;

    private Animator anim;
    private SpriteRenderer dibujo;
    private Rigidbody2D cuerpo;
    private CapsuleCollider2D colision;
    private Vector2 tamanoOriginal;
    private Vector2 offsetOriginal;
    private float ejeHorizontal;
    private bool correr;
    private bool quiereAgacharse;
    private bool agachado;
    private bool saltarPendiente;
    private bool mirandoIzquierda;
    private bool muerto;
    private int vidaActual;
    private float accionHasta;
    private float siguienteAtaque;
    private float cambioPosturaHasta;
    private string accionActual = "";
    private string estadoActual = "";

    private void Awake()
    {
        anim = GetComponent<Animator>();
        dibujo = GetComponent<SpriteRenderer>();
        cuerpo = GetComponent<Rigidbody2D>();
        colision = GetComponent<CapsuleCollider2D>();
        cuerpo.freezeRotation = true;
        tamanoOriginal = colision.size;
        offsetOriginal = colision.offset;
        vidaActual = Mathf.Max(1, vidaMaxima);
    }

    private void Update()
    {
        if (muerto) return;
        LeerTeclado();

        if (ejeHorizontal < -0.01f) mirandoIzquierda = true;
        else if (ejeHorizontal > 0.01f) mirandoIzquierda = false;
        dibujo.flipX = mirandoIzquierda;

        if (Time.time >= accionHasta) accionActual = "";
        bool accionOcupada = accionActual.Length > 0;

        // La accion prevalece sobre la animacion normal de movimiento.
        if (!accionOcupada)
        {
            if (quiereAgacharse || (agachado && !PuedeLevantarse()))
            {
                if (!agachado)
                {
                    agachado = true;
                    AjustarColision(true);
                    cambioPosturaHasta = Time.time + DuracionEstado("AgacharseBajar", 0.5f);
                    Reproducir("AgacharseBajar");
                }
                else if (Time.time >= cambioPosturaHasta)
                {
                    Reproducir("AgacharseQuieto");
                }
            }
            else if (agachado)
            {
                agachado = false;
                AjustarColision(false);
                cambioPosturaHasta = Time.time + DuracionEstado("AgacharseLevantar", 0.4f);
                Reproducir("AgacharseLevantar");
            }
        }

        if (accionOcupada) return;
        if (agachado) return;
        if (Time.time < cambioPosturaHasta) return;

        if (!EstaEnSuelo())
            Reproducir(cuerpo.linearVelocity.y > 0.05f ? "Saltar" : "Caer");
        else if (Mathf.Abs(ejeHorizontal) < 0.01f)
            Reproducir("Reposo");
        else
            Reproducir(correr ? "Correr" : "Caminar");
    }

    private void FixedUpdate()
    {
        if (muerto)
        {
            cuerpo.linearVelocity = new Vector2(0f, cuerpo.linearVelocity.y);
            return;
        }

        float velocidad = agachado ? velocidadAgachado : (correr ? velocidadCorrer : velocidadCaminar);
        if (accionActual.Length > 0 || Time.time < cambioPosturaHasta) velocidad = 0f;
        cuerpo.linearVelocity = new Vector2(ejeHorizontal * velocidad, cuerpo.linearVelocity.y);

        if (saltarPendiente && EstaEnSuelo() && !agachado && accionActual.Length == 0)
            cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, fuerzaSalto);
        saltarPendiente = false;
    }

    private void LeerTeclado()
    {
        bool magia = false;
        bool cuchillo = false;
        bool recibido = false;
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            recibido = true;
            ejeHorizontal = (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed ? 1f : 0f)
                            - (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed ? 1f : 0f);
            correr = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
            quiereAgacharse = teclado.leftCtrlKey.isPressed || teclado.rightCtrlKey.isPressed
                               || teclado.sKey.isPressed || teclado.downArrowKey.isPressed;
            if (teclado.spaceKey.wasPressedThisFrame) saltarPendiente = true;
            magia = teclado.kKey.wasPressedThisFrame;
            cuchillo = teclado.jKey.wasPressedThisFrame;
#if UNITY_EDITOR
            if (teclado.bKey.wasPressedThisFrame) RecibirDanio(1);
            if (teclado.nKey.wasPressedThisFrame) Morir();
#endif
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (!recibido)
        {
            ejeHorizontal = Input.GetAxisRaw("Horizontal");
            correr = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            quiereAgacharse = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                               || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            if (Input.GetKeyDown(KeyCode.Space)) saltarPendiente = true;
            magia = Input.GetKeyDown(KeyCode.K);
            cuchillo = Input.GetKeyDown(KeyCode.J);
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.B)) RecibirDanio(1);
            if (Input.GetKeyDown(KeyCode.N)) Morir();
#endif
        }
#endif
        if (magia) IntentarAtaque(false);
        if (cuchillo) IntentarAtaque(true);
    }

    private void IntentarAtaque(bool usarCuchillo)
    {
        if (muerto || agachado || quiereAgacharse || accionActual.Length > 0 || Time.time < siguienteAtaque) return;
        if (usarCuchillo && !cuchilloDesbloqueado) return; // Se desbloquea con el amigo del cerro.

        string nombre = usarCuchillo ? "Cuchillo" : "Magia";
        accionActual = nombre;
        accionHasta = Time.time + DuracionEstado(nombre, 0.55f);
        siguienteAtaque = accionHasta + tiempoEntreAtaques;
        Reproducir(nombre);
        if (usarCuchillo) GolpearConCuchillo();
        else LanzarMagia();
    }

    private void GolpearConCuchillo()
    {
        Vector2 centro = (Vector2)transform.position + new Vector2(mirandoIzquierda ? -1.65f : 1.65f, 2.1f);
        foreach (Collider2D c in Physics2D.OverlapCircleAll(centro, alcanceCuchillo))
        {
            if (!c || c.transform.IsChildOf(transform)) continue;
            YatiriObjetivo objetivo = c.GetComponentInParent<YatiriObjetivo>();
            if (objetivo != null) objetivo.RecibirImpacto(danioCuchillo);
        }
    }

    private void LanzarMagia()
    {
        GameObject bolita = new GameObject("Hechizo_Espiritual_Temporal");
        bolita.transform.position = transform.position + new Vector3(mirandoIzquierda ? -1.8f : 1.8f, 2.5f, 0f);
        SpriteRenderer sr = bolita.AddComponent<SpriteRenderer>();
        sr.sprite = YatiriProyectil.CrearSprite();
        sr.color = new Color(0.55f, 0.93f, 1f, 1f);
        sr.sortingOrder = dibujo.sortingOrder + 2;
        bolita.transform.localScale = Vector3.one * 0.7f;
        CircleCollider2D col = bolita.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        Rigidbody2D rb = bolita.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        YatiriProyectil proyectil = bolita.AddComponent<YatiriProyectil>();
        proyectil.Inicializar(mirandoIzquierda ? -velocidadProyectil : velocidadProyectil, danioMagia, transform);
    }

    private float DuracionEstado(string estado, float respaldo)
    {
        if (anim.runtimeAnimatorController == null) return respaldo;
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name == estado) return Mathf.Max(0.12f, clip.length);
        return respaldo;
    }

    private void Reproducir(string estado)
    {
        if (anim.runtimeAnimatorController == null || estado == estadoActual) return;
        estadoActual = estado;
        anim.Play(estado, 0, 0f);
    }

    private void AjustarColision(bool bajo)
    {
        colision.size = bajo ? new Vector2(tamanoOriginal.x, tamanoOriginal.y * 0.55f) : tamanoOriginal;
        // Mantiene los pies en el mismo punto al reducir el collider.
        colision.offset = bajo
            ? new Vector2(offsetOriginal.x, offsetOriginal.y - tamanoOriginal.y * 0.225f)
            : offsetOriginal;
    }

    private bool PuedeLevantarse()
    {
        Vector2 centro = (Vector2)transform.position + offsetOriginal;
        Collider2D[] hits = Physics2D.OverlapCapsuleAll(centro, tamanoOriginal * 0.92f,
            CapsuleDirection2D.Vertical, 0f, ~0);
        foreach (Collider2D hit in hits)
        {
            if (hit && !hit.isTrigger && !hit.transform.IsChildOf(transform)) return false;
        }
        return true;
    }

    private bool EstaEnSuelo()
    {
        if (colision.IsTouchingLayers(capasSuelo)) return true;
        Bounds b = colision.bounds;
        Vector2 centro = new Vector2(b.center.x, b.min.y - distanciaSuelo - 0.05f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(centro, new Vector2(b.size.x * 0.8f, 0.12f), 0f, capasSuelo);
        foreach (Collider2D hit in hits)
            if (hit && !hit.isTrigger && !hit.transform.IsChildOf(transform)) return true;
        return false;
    }

    public void DesbloquearCuchillo() { cuchilloDesbloqueado = true; }

    public void RecibirDanio(int cantidad)
    {
        if (muerto || cantidad <= 0) return;
        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        if (vidaActual <= 0) { Morir(); return; }
        accionActual = "Herido";
        accionHasta = Time.time + DuracionEstado("Herido", 0.55f);
        Reproducir("Herido");
    }

    public void Morir()
    {
        if (muerto) return;
        muerto = true;
        accionActual = "Muerte";
        Reproducir("Muerte");
    }
}

