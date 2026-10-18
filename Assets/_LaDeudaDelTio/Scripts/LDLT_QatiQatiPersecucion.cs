using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Colocar SOLO en LDLT_QatiQati, dentro de 03_PersecucionBicicleta.
// No modifica prefabs originales, no requiere collider ni Rigidbody2D en el enemigo.
public class LDLT_QatiQatiPersecucion : MonoBehaviour
{
    [Header("Referencias de la escena")]
    [Tooltip("Arrastra el objeto LDLT_AbueloBicicleta desde Hierarchy.")]
    public Transform abuelo;
    [Tooltip("Opcional: arrastra el componente LDLT_BicicletaMovimiento del abuelo.")]
    public MonoBehaviour controladorBicicleta;
    [Tooltip("Opcional: arrastra META_PERSECUCION para detener la persecucion al ganar.")]
    public Transform metaPersecucion;

    [Header("Distancias y velocidades")]
    [Min(1f)] public float distanciaSegura = 5f;
    [Min(0.2f)] public float distanciaCaptura = 0.8f;
    [Min(0.1f)] public float velocidadAcercamiento = 1.5f;
    [Min(0.1f)] public float velocidadRecuperacion = 1.4f;
    [Min(0.1f)] public float suavidadSeguimiento = 12f;
    [Min(0f)] public float alturaSobreAbuelo = 1.35f;
    [Min(0f)] public float oscilacionVertical = 0.2f;
    [Min(0f)] public float velocidadMinimaParaPedalear = 0.1f;

    [Header("Animaciones (nombres de ESTADOS del Animator)")]
    public string estadoPerseguir = "Perseguir";
    public string estadoAtaque = "Ataque";
    [Min(0f)] public float segundosDeJumpscare = 1.2f;

    private Animator animador;
    private Rigidbody2D cuerpoAbuelo;
    private float separacion;
    private float ultimaX;
    private float tiempoAtaque;
    private bool atrapado;
    private bool mostrarDerrota;
    private Texture2D pixel;

    private void Awake()
    {
        animador = GetComponent<Animator>();
        if (animador == null) animador = GetComponentInChildren<Animator>();

        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    private void Start()
    {
        if (abuelo == null)
        {
            Debug.LogError("Qati Qati: asigna LDLT_AbueloBicicleta al campo Abuelo.", this);
            enabled = false;
            return;
        }

        cuerpoAbuelo = abuelo.GetComponent<Rigidbody2D>();
        if (cuerpoAbuelo == null) cuerpoAbuelo = abuelo.GetComponentInChildren<Rigidbody2D>();

        // Respeta la distancia aproximada que colocaste a mano en la escena.
        float distanciaEnEscena = abuelo.position.x - transform.position.x;
        separacion = distanciaEnEscena > distanciaCaptura
            ? Mathf.Clamp(distanciaEnEscena, distanciaCaptura + 0.5f, distanciaSegura + 2f)
            : distanciaSegura;
        ultimaX = abuelo.position.x;

        Reproducir(estadoPerseguir);
    }

    private void Update()
    {
        if (abuelo == null) return;

        if (atrapado)
        {
            tiempoAtaque += Time.unscaledDeltaTime;
            if (tiempoAtaque >= segundosDeJumpscare) mostrarDerrota = true;

            if (mostrarDerrota && Keyboard.current != null &&
                Keyboard.current.rKey.wasPressedThisFrame)
                Reintentar();
            return;
        }

        // La zona de caida y la meta desactivan la simulacion del abuelo.
        // Con esto NO mostramos dos pantallas de derrota/victoria simultaneas.
        if (cuerpoAbuelo != null && !cuerpoAbuelo.simulated)
        {
            enabled = false;
            return;
        }

        if (metaPersecucion != null && abuelo.position.x >= metaPersecucion.position.x)
        {
            enabled = false;
            return;
        }

        float dt = Time.deltaTime;
        float velocidadRealX = dt > 0f ? (abuelo.position.x - ultimaX) / dt : 0f;
        ultimaX = abuelo.position.x;

        // Manten D para escapar; si D esta presionada pero el abuelo esta
        // atascado y no avanza, el Qati Qati sigue acercandose.
        bool teclaD = Keyboard.current != null && Keyboard.current.dKey.isPressed;
        bool pedaleandoYAvanzando = teclaD && velocidadRealX > velocidadMinimaParaPedalear;

        float distanciaObjetivo = pedaleandoYAvanzando ? distanciaSegura : 0f;
        float rapidez = pedaleandoYAvanzando ? velocidadRecuperacion : velocidadAcercamiento;
        separacion = Mathf.MoveTowards(separacion, distanciaObjetivo, rapidez * dt);

        float flotacion = Mathf.Sin(Time.time * 3f) * oscilacionVertical;
        Vector3 destino = new Vector3(
            abuelo.position.x - separacion,
            abuelo.position.y + alturaSobreAbuelo + flotacion,
            transform.position.z);
        float factor = 1f - Mathf.Exp(-suavidadSeguimiento * dt);
        transform.position = Vector3.Lerp(transform.position, destino, factor);

        if (separacion <= distanciaCaptura)
            Atrapar();
    }

    private void Atrapar()
    {
        if (atrapado) return;
        atrapado = true;
        tiempoAtaque = 0f;

        // Mostrar el jumpscare cerca del abuelo.
        transform.position = new Vector3(
            abuelo.position.x - distanciaCaptura,
            abuelo.position.y + alturaSobreAbuelo,
            transform.position.z);
        Reproducir(estadoAtaque);

        // Buscar controlador en el abuelo, sin tocar otros scripts.
        if (controladorBicicleta == null)
        {
            foreach (MonoBehaviour componente in abuelo.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (componente != null && componente.GetType().Name == "LDLT_BicicletaMovimiento")
                {
                    controladorBicicleta = componente;
                    break;
                }
            }
        }
        if (controladorBicicleta != null) controladorBicicleta.enabled = false;

        if (cuerpoAbuelo != null)
        {
            cuerpoAbuelo.linearVelocity = Vector2.zero;
            cuerpoAbuelo.angularVelocity = 0f;
            cuerpoAbuelo.simulated = false;
        }
    }

    private void Reproducir(string nombreEstado)
    {
        if (animador == null || string.IsNullOrEmpty(nombreEstado)) return;

        int estado = Animator.StringToHash("Base Layer." + nombreEstado);
        if (animador.HasState(0, estado))
            animador.Play(estado, 0, 0f);
        else
            Debug.LogWarning("Qati Qati: no existe el estado '" + nombreEstado +
                "' en el Animator. Revisa el nombre en la ventana Animator.", this);
    }

    private void Reintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnGUI()
    {
        if (pixel == null) return;
        GUI.depth = -50;

        if (!atrapado && separacion <= distanciaSegura * 0.42f)
        {
            Rect aviso = new Rect((Screen.width - 490f) * 0.5f, 28f, 490f, 50f);
            Pintar(aviso, new Color(0.13f, 0.015f, 0.025f, 0.89f));
            Texto(aviso, "¡PEDALEA! EL QATI QATI SE ACERCA", 20,
                new Color(1f, 0.81f, 0.65f), FontStyle.Bold);
        }

        if (!mostrarDerrota) return;

        Pintar(new Rect(0f, 0f, Screen.width, Screen.height),
            new Color(0.015f, 0.005f, 0.015f, 0.88f));

        float ancho = Mathf.Min(590f, Screen.width - 24f);
        float alto = Mathf.Min(285f, Screen.height - 24f);
        Rect panel = new Rect((Screen.width - ancho) * 0.5f,
            (Screen.height - alto) * 0.5f, ancho, alto);
        Pintar(new Rect(panel.x + 8f, panel.y + 8f, panel.width, panel.height), Color.black);
        Pintar(panel, new Color(0.07f, 0.06f, 0.085f, 0.98f));
        Color dorado = new Color(0.96f, 0.66f, 0.29f);
        Pintar(new Rect(panel.x, panel.y, panel.width, 3f), dorado);
        Pintar(new Rect(panel.x, panel.yMax - 3f, panel.width, 3f), dorado);
        Pintar(new Rect(panel.x, panel.y, 3f, panel.height), dorado);
        Pintar(new Rect(panel.xMax - 3f, panel.y, 3f, panel.height), dorado);

        Texto(new Rect(panel.x + 12f, panel.y + 28f, ancho - 24f, 58f),
            "¡EL QATI QATI TE ATRAPÓ!", 29, new Color(1f, 0.44f, 0.4f), FontStyle.Bold);
        Texto(new Rect(panel.x + 12f, panel.y + 101f, ancho - 24f, 34f),
            "La cabeza voladora alcanzó al abuelo...", 18, Color.white, FontStyle.Normal);
        Texto(new Rect(panel.x + 12f, panel.y + 145f, ancho - 24f, 26f),
            "No dejes de pedalear para escapar.", 16,
            new Color(0.85f, 0.86f, 0.89f), FontStyle.Normal);

        Rect boton = new Rect(panel.x + ancho * 0.20f, panel.yMax - 77f,
            ancho * 0.60f, 53f);
        Pintar(boton, new Color(0.40f, 0.11f, 0.13f));
        GUIStyle estilo = new GUIStyle(GUI.skin.button);
        estilo.fontSize = 19;
        estilo.fontStyle = FontStyle.Bold;
        estilo.normal.textColor = Color.white;
        estilo.hover.textColor = Color.white;
        estilo.active.textColor = Color.white;
        estilo.normal.background = null;
        estilo.hover.background = null;
        estilo.active.background = null;
        if (GUI.Button(boton, "[R] VOLVER A INTENTAR", estilo)) Reintentar();
    }

    private void Pintar(Rect area, Color color)
    {
        Color anterior = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(area, pixel);
        GUI.color = anterior;
    }

    private static void Texto(Rect area, string mensaje, int tamano,
        Color color, FontStyle estiloFuente)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = tamano;
        estilo.fontStyle = estiloFuente;
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.normal.textColor = color;
        estilo.wordWrap = true;
        GUI.Label(area, mensaje, estilo);
    }

    private void OnDestroy()
    {
        if (pixel != null) Destroy(pixel);
    }
}
