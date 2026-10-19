using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Vida para la escena de hordas. No modifica el movimiento ni el ataque de SpriteCook.
[DisallowMultipleComponent]
public class LDLT_VidaAbuelo : MonoBehaviour
{
    [Header("Vida y regeneracion")]
    [Min(1)] public int corazonesMaximos = 5;
    [Min(0.1f)] public float segundosPorCorazon = 10f;
    [Min(0f)] public float invulnerabilidad = 1f;

    [Header("HUD")]
    public bool mostrarHUD = true;

    public int CorazonesActuales { get; private set; }
    public bool EstaDerrotado { get; private set; }

    private float siguienteDanioPermitido;
    private float tiempoRecuperacion;
    private Texture2D pixel;
    private bool pausaAplicada;

    private void Awake()
    {
        CorazonesActuales = Mathf.Max(1, corazonesMaximos);
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    private void Update()
    {
        if (EstaDerrotado)
        {
            if (TeclaR()) Reiniciar();
            return;
        }

        if (CorazonesActuales < corazonesMaximos)
        {
            tiempoRecuperacion += Time.deltaTime;
            if (tiempoRecuperacion >= segundosPorCorazon)
            {
                CorazonesActuales = Mathf.Min(corazonesMaximos, CorazonesActuales + 1);
                tiempoRecuperacion = 0f;
            }
        }
        else tiempoRecuperacion = 0f;
    }

    // Se llama desde el ataque del Kari Kari: quita un corazon con proteccion entre golpes.
    public bool RecibirDanio(int cantidad = 1)
    {
        if (EstaDerrotado || Time.time < siguienteDanioPermitido) return false;
        CorazonesActuales = Mathf.Max(0, CorazonesActuales - Mathf.Max(1, cantidad));
        siguienteDanioPermitido = Time.time + invulnerabilidad;
        tiempoRecuperacion = 0f;
        if (CorazonesActuales == 0)
        {
            EstaDerrotado = true;
            Time.timeScale = 0f;
            pausaAplicada = true;
        }
        return true;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        pausaAplicada = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static bool TeclaR()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) return Keyboard.current.rKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }

    private void Dibujar(Rect rect, Color color)
    {
        Color anterior = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = anterior;
    }

    private static void Etiqueta(Rect rect, string mensaje, int tamano, Color color, TextAnchor ancla = TextAnchor.MiddleLeft)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = tamano;
        estilo.fontStyle = FontStyle.Bold;
        estilo.normal.textColor = color;
        estilo.alignment = ancla;
        GUI.Label(rect, mensaje, estilo);
    }

    private void OnGUI()
    {
        if (pixel == null) return;
        float escala = Mathf.Clamp(Screen.width / 1280f, 0.72f, 1.4f);
        float ancho = 332f * escala;
        float alto = 105f * escala;
        if (mostrarHUD)
        {
            Rect p = new Rect(22f * escala, 18f * escala, ancho, alto);
            Dibujar(new Rect(p.x + 4, p.y + 5, p.width, p.height), new Color(0f, 0f, 0f, 0.55f));
            Dibujar(p, new Color(0.075f, 0.065f, 0.08f, 0.96f));
            Dibujar(new Rect(p.x, p.y, p.width, 3f * escala), new Color(0.91f, 0.66f, 0.31f));
            Etiqueta(new Rect(p.x + 16f * escala, p.y + 10f * escala, 290f * escala, 25f * escala),
                "VIDA DEL ABUELO", Mathf.RoundToInt(18 * escala), new Color(1f, 0.83f, 0.51f));
            // Corazones claros de leer sobre un fondo oscuro.
            for (int i = 0; i < corazonesMaximos && i < 12; i++)
            {
                float x = p.x + (15f + i * 46f) * escala;
                Etiqueta(new Rect(x, p.y + 39f * escala, 45f * escala, 42f * escala), "♥",
                    Mathf.RoundToInt(37f * escala), i < CorazonesActuales ?
                    new Color(1f, 0.26f, 0.34f) : new Color(0.26f, 0.24f, 0.29f), TextAnchor.MiddleCenter);
            }
            if (CorazonesActuales < corazonesMaximos && !EstaDerrotado)
            {
                float frac = Mathf.Clamp01(tiempoRecuperacion / Mathf.Max(0.1f, segundosPorCorazon));
                Rect barra = new Rect(p.x + 17f * escala, p.y + 90f * escala, p.width - 34f * escala, 5f * escala);
                Dibujar(barra, new Color(0.25f, 0.22f, 0.25f));
                Dibujar(new Rect(barra.x, barra.y, barra.width * frac, barra.height), new Color(0.55f, 0.95f, 0.58f));
            }
        }

        if (!EstaDerrotado) return;
        Dibujar(new Rect(0, 0, Screen.width, Screen.height), new Color(0.015f, 0f, 0.025f, 0.9f));
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        Rect fondo = new Rect(cx - 310f * escala, cy - 150f * escala, 620f * escala, 300f * escala);
        Dibujar(fondo, new Color(0.08f, 0.045f, 0.075f, 1f));
        Dibujar(new Rect(fondo.x, fondo.y, fondo.width, 4f * escala), new Color(0.95f, 0.62f, 0.32f));
        Etiqueta(new Rect(fondo.x, fondo.y + 35f * escala, fondo.width, 65f * escala),
            "EL KARI KARI TE DERROTO", Mathf.RoundToInt(29 * escala), Color.white, TextAnchor.MiddleCenter);
        Etiqueta(new Rect(fondo.x, fondo.y + 105f * escala, fondo.width, 45f * escala),
            "Los espiritus del pueblo no te dejaron escapar...", Mathf.RoundToInt(16 * escala),
            new Color(0.88f, 0.75f, 0.64f), TextAnchor.MiddleCenter);
        Rect boton = new Rect(cx - 145f * escala, cy + 47f * escala, 290f * escala, 57f * escala);
        if (GUI.Button(boton, "REINTENTAR  [R]")) Reiniciar();
    }

    private void OnDestroy()
    {
        if (pausaAplicada) Time.timeScale = 1f;
        if (pixel != null) Destroy(pixel);
    }
}
