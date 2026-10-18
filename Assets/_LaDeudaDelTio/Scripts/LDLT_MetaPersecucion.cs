using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Punto de llegada de la persecucion. Colocar en un objeto con BoxCollider2D (Is Trigger).
// No modifica el Player original del pueblo.
[RequireComponent(typeof(BoxCollider2D))]
public class LDLT_MetaPersecucion : MonoBehaviour
{
    [Header("Llegada de la bicicleta")]
    public Transform jugador;
    [Tooltip("Opcional: si queda vacio, se busca LDLT_BicicletaMovimiento automaticamente.")]
    public MonoBehaviour controladorMovimiento;

    [Header("Pantalla de victoria")]
    [Min(0.25f)] public float duracionFundido = 1.8f;
    [Min(0f)] public float pausaAntesDelMensaje = 0.3f;

    [Header("Continuacion futura (opcional)")]
    [Tooltip("Dejalo vacio por ahora. Cuando exista la siguiente escena, escribe su nombre exacto y agregala a Build Profiles > Scene List.")]
    public string escenaSiguiente = "";

    private Texture2D pixelBlanco;
    private float tiempoVictoria;
    private bool victoria;
    private bool cargando;

    private void Awake()
    {
        pixelBlanco = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixelBlanco.SetPixel(0, 0, Color.white);
        pixelBlanco.Apply();
        pixelBlanco.filterMode = FilterMode.Point;

        BoxCollider2D zona = GetComponent<BoxCollider2D>();
        if (!zona.isTrigger)
            Debug.LogWarning("LDLT_MetaPersecucion: activa 'Is Trigger' en el Box Collider 2D.", this);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (victoria || jugador == null) return;

        bool esJugador = otro.transform == jugador || otro.transform.IsChildOf(jugador);
        if (!esJugador && otro.attachedRigidbody != null)
        {
            Transform cuerpo = otro.attachedRigidbody.transform;
            esJugador = cuerpo == jugador || cuerpo.IsChildOf(jugador);
        }

        if (!esJugador) return;
        GanarPersecucion();
    }

    private void GanarPersecucion()
    {
        victoria = true;
        tiempoVictoria = 0f;

        // Detener el control solo en esta escena, sin destruir ni modificar prefabs.
        if (controladorMovimiento == null)
        {
            foreach (MonoBehaviour componente in jugador.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (componente != null && componente.GetType().Name == "LDLT_BicicletaMovimiento")
                {
                    controladorMovimiento = componente;
                    break;
                }
            }
        }
        if (controladorMovimiento != null)
            controladorMovimiento.enabled = false;

        Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
        if (rb == null) rb = jugador.GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        Animator animador = jugador.GetComponent<Animator>();
        if (animador == null) animador = jugador.GetComponentInChildren<Animator>();
        if (animador != null) animador.speed = 0f;

        Debug.Log("La Deuda del Tio: ¡El abuelo sobrevivio a la persecucion!");
    }

    private void Update()
    {
        if (!victoria) return;

        tiempoVictoria += Time.unscaledDeltaTime;
        bool yaAparecioMensaje = tiempoVictoria > duracionFundido + pausaAntesDelMensaje + 0.6f;
        bool escenaLista = !string.IsNullOrWhiteSpace(escenaSiguiente) &&
                           Application.CanStreamedLevelBeLoaded(escenaSiguiente);

        if (yaAparecioMensaje && escenaLista && !cargando && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            cargando = true;
            SceneManager.LoadScene(escenaSiguiente);
        }
    }

    private void OnGUI()
    {
        if (!victoria || pixelBlanco == null) return;
        GUI.depth = -100;

        float fundido = Mathf.Clamp01(tiempoVictoria / Mathf.Max(0.25f, duracionFundido));
        DibujarRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, fundido));

        float entrada = Mathf.Clamp01((tiempoVictoria - duracionFundido - pausaAntesDelMensaje) / 0.75f);
        entrada = entrada * entrada * (3f - 2f * entrada); // Aparicion suave.
        if (entrada <= 0.01f) return;

        float escala = Mathf.Clamp(Mathf.Min(Screen.width / 1600f, Screen.height / 900f), 0.65f, 1.35f);
        float ancho = Mathf.Min(Screen.width - 20f, 860f * escala);
        float alto = Mathf.Min(Screen.height - 20f, 370f * escala);
        float x = (Screen.width - ancho) * 0.5f;
        float y = (Screen.height - alto) * 0.5f + (1f - entrada) * 24f * escala;
        Rect panel = new Rect(x, y, ancho, alto);

        Color oro = new Color(0.98f, 0.72f, 0.34f, entrada);
        Color blanco = new Color(1f, 0.96f, 0.86f, entrada);
        Color secundario = new Color(0.85f, 0.83f, 0.78f, entrada);

        DibujarRect(new Rect(x + 9f * escala, y + 11f * escala, ancho, alto), new Color(0, 0, 0, 0.7f * entrada));
        DibujarRect(panel, new Color(0.055f, 0.060f, 0.080f, 0.96f * entrada));
        Borde(panel, 3f * escala, oro);
        Borde(new Rect(x + 8f * escala, y + 8f * escala, ancho - 16f * escala, alto - 16f * escala),
              1f * escala, new Color(0.55f, 0.31f, 0.17f, entrada));

        DibujarRect(new Rect(x + ancho * 0.28f, y + 68f * escala, ancho * 0.44f, 2f * escala), oro);
        Texto(new Rect(x, y + 26f * escala, ancho, 32f * escala),
              "LA DEUDA DEL TÍO  ·  PERSECUCIÓN SUPERADA", 17, escala, FontStyle.Bold, oro);
        Texto(new Rect(x + 15f, y + 96f * escala, ancho - 30f, 70f * escala),
              "¡LOGRASTE ESCAPAR!", 44, escala, FontStyle.Bold, blanco);
        Texto(new Rect(x + 12f, y + 185f * escala, ancho - 24f, 45f * escala),
              "El abuelo sobrevivió a la cabeza voladora.", 22, escala, FontStyle.Normal, secundario);
        DibujarRect(new Rect(x + ancho * 0.23f, y + 253f * escala, ancho * 0.54f, 1f * escala),
                    new Color(0.78f, 0.52f, 0.26f, entrada));

        bool escenaLista = !string.IsNullOrWhiteSpace(escenaSiguiente) &&
                           Application.CanStreamedLevelBeLoaded(escenaSiguiente);
        string pie = escenaLista ? "[ ENTER ]  CONTINUAR LA HISTORIA" : "LA HISTORIA CONTINUARÁ...";
        Texto(new Rect(x + 10f, y + 282f * escala, ancho - 20f, 45f * escala),
              pie, 20, escala, FontStyle.Bold, oro);
    }

    private void DibujarRect(Rect rect, Color color)
    {
        Color antes = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixelBlanco);
        GUI.color = antes;
    }

    private void Borde(Rect rect, float grosor, Color color)
    {
        DibujarRect(new Rect(rect.x, rect.y, rect.width, grosor), color);
        DibujarRect(new Rect(rect.x, rect.yMax - grosor, rect.width, grosor), color);
        DibujarRect(new Rect(rect.x, rect.y, grosor, rect.height), color);
        DibujarRect(new Rect(rect.xMax - grosor, rect.y, grosor, rect.height), color);
    }

    private void Texto(Rect rect, string contenido, int tamano, float escala, FontStyle estiloFuente, Color color)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = Mathf.Max(12, Mathf.RoundToInt(tamano * escala));
        estilo.fontStyle = estiloFuente;
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.clipping = TextClipping.Clip;
        estilo.wordWrap = false;

        // Sombra bajo las letras para legibilidad.
        estilo.normal.textColor = new Color(0f, 0f, 0f, color.a);
        GUI.Label(new Rect(rect.x + 2f * escala, rect.y + 3f * escala, rect.width, rect.height), contenido, estilo);
        estilo.normal.textColor = color;
        GUI.Label(rect, contenido, estilo);
    }

    private void OnDestroy()
    {
        if (pixelBlanco != null) Destroy(pixelBlanco);
    }
}
