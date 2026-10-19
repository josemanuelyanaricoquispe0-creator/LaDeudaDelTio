using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Colocar un Empty frente a la ENTRADA de la mina, no sobre el Spawner.
// Solo permite pulsar Y cuando se derrotaron los 15 Kari Kari.
[DisallowMultipleComponent]
public class LDLT_EntradaBarrioMineros : MonoBehaviour
{
    [Header("Conectar desde Hierarchy")]
    public LDLT_SpawnerKariKari spawner;
    public Transform abuelo;

    [Header("Entrada a la siguiente escena")]
    public string escenaDestino = "06_BarrioDeMineros";
    [Min(0.5f)] public float radioActivacion = 2.5f;

    [Header("Interfaz")]
    public bool mostrarContador = true;
    public bool mostrarMensajeCercaDeMina = true;

    private LDLT_VidaAbuelo vidaAbuelo;
    private Texture2D pixel;
    private bool avisoEscena;

    private bool EstaCerca => abuelo != null &&
        Vector2.Distance((Vector2)abuelo.position, (Vector2)transform.position) <= radioActivacion;

    private bool HordaCompleta => spawner != null &&
        spawner.TotalDerrotados >= Mathf.Max(1, spawner.totalAparecer);

    private void Awake()
    {
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    private void Start()
    {
        if (abuelo != null)
            vidaAbuelo = abuelo.GetComponent<LDLT_VidaAbuelo>();
    }

    private void Update()
    {
        if (abuelo == null || spawner == null) return;
        if (vidaAbuelo != null && vidaAbuelo.EstaDerrotado) return;
        if (!EstaCerca || !HordaCompleta || !PulsaronY()) return;

        if (!Application.CanStreamedLevelBeLoaded(escenaDestino))
        {
            if (!avisoEscena)
            {
                Debug.LogError(
                    "Entrada a mineros: agrega la escena '" + escenaDestino +
                    "' en File > Build Profiles > Scene List (con la casilla activada).",
                    this);
                avisoEscena = true;
            }
            return;
        }

        SceneManager.LoadScene(escenaDestino);
    }

    private static bool PulsaronY()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.yKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Y);
#else
        return false;
#endif
    }

    private void Fondo(Rect area, Color color)
    {
        if (pixel == null) return;
        Color anterior = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(area, pixel);
        GUI.color = anterior;
    }

    private static void Texto(Rect area, string valor, int tamano, Color color,
        TextAnchor alineacion = TextAnchor.MiddleCenter)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = tamano;
        estilo.fontStyle = FontStyle.Bold;
        estilo.normal.textColor = color;
        estilo.alignment = alineacion;
        GUI.Label(area, valor, estilo);
    }

    private void OnGUI()
    {
        if (spawner == null || pixel == null) return;

        float escala = Mathf.Clamp(Screen.width / 1280f, 0.7f, 1.3f);
        int total = Mathf.Max(1, spawner.totalAparecer);
        int derrotados = Mathf.Clamp(spawner.TotalDerrotados, 0, total);
        bool completo = HordaCompleta;

        // Contador compacto, separado de los corazones del abuelo.
        if (mostrarContador)
        {
            float ancho = 270f * escala;
            Rect panel = new Rect(Screen.width - ancho - 16f * escala,
                17f * escala, ancho, 75f * escala);
            Fondo(panel, new Color(0.055f, 0.045f, 0.06f, 0.9f));
            Fondo(new Rect(panel.x, panel.y, panel.width, 3f * escala),
                completo ? new Color(0.43f, 0.93f, 0.55f) :
                new Color(0.9f, 0.66f, 0.31f));
            Texto(new Rect(panel.x, panel.y + 6f * escala, panel.width, 24f * escala),
                "HORDA DEL PUEBLO", Mathf.RoundToInt(16 * escala),
                new Color(1f, 0.82f, 0.54f));
            Texto(new Rect(panel.x, panel.y + 32f * escala, panel.width, 34f * escala),
                derrotados + " / " + total + "  KARI KARI",
                Mathf.RoundToInt(20 * escala),
                completo ? new Color(0.48f, 1f, 0.6f) : Color.white);
        }

        if (!mostrarMensajeCercaDeMina || !EstaCerca ||
            (vidaAbuelo != null && vidaAbuelo.EstaDerrotado))
            return;

        float anchoMensaje = Mathf.Min(Screen.width - 20f,
            620f * escala);
        Rect mensaje = new Rect((Screen.width - anchoMensaje) / 2f,
            Screen.height - 98f * escala, anchoMensaje, 70f * escala);

        Fondo(mensaje, new Color(0.045f, 0.035f, 0.05f, 0.94f));
        Fondo(new Rect(mensaje.x, mensaje.y, mensaje.width, 3f * escala),
            completo ? new Color(0.43f, 0.93f, 0.55f) :
            new Color(0.95f, 0.36f, 0.3f));

        if (completo)
        {
            Texto(new Rect(mensaje.x, mensaje.y + 6f * escala,
                    mensaje.width, 52f * escala),
                "[Y]  ENTRAR AL BARRIO DE MINEROS",
                Mathf.RoundToInt(20 * escala), Color.white);
        }
        else
        {
            int faltan = Mathf.Max(0, total - derrotados);
            Texto(new Rect(mensaje.x, mensaje.y + 5f * escala,
                    mensaje.width, 55f * escala),
                "MINA BLOQUEADA  -  FALTAN " + faltan + " KARI KARI",
                Mathf.RoundToInt(18 * escala),
                new Color(1f, 0.76f, 0.61f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.72f, 0.22f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radioActivacion);
    }

    private void OnDestroy()
    {
        if (pixel != null) Destroy(pixel);
    }
}
