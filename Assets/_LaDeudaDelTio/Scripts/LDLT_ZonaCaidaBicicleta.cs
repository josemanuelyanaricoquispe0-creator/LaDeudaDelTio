using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Colocar en un objeto con Box Collider 2D configurado como Is Trigger.
// Ubicar el objeto debajo de un hueco de la carretera.
public class LDLT_ZonaCaidaBicicleta : MonoBehaviour
{
    [Header("Personaje de la persecucion")]
    [SerializeField] private Transform jugador;
    [Tooltip("Arrastra aqui el componente que controla el pedaleo y el salto (opcional).")]
    [SerializeField] private MonoBehaviour controladorMovimiento;

    private bool derrotado;
    private bool reintentarSolicitado;
    private Texture2D pixel;

    private void Awake()
    {
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (derrotado || jugador == null)
            return;

        // Reconoce el collider del jugador y los colliders de sus hijos.
        if (otro.transform != jugador && !otro.transform.IsChildOf(jugador))
            return;

        derrotado = true;

        if (controladorMovimiento != null)
            controladorMovimiento.enabled = false;

        Rigidbody2D cuerpo = jugador.GetComponent<Rigidbody2D>();
        if (cuerpo == null)
            cuerpo = jugador.GetComponentInChildren<Rigidbody2D>();
        if (cuerpo != null)
            cuerpo.simulated = false;
    }

    private void Update()
    {
        if (!derrotado)
            return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            reintentarSolicitado = true;

        if (reintentarSolicitado)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnGUI()
    {
        if (!derrotado || pixel == null)
            return;

        float escala = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.6f, 1.35f);
        Pintar(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.015f, 0.012f, 0.025f, 0.81f));

        float ancho = Mathf.Min(Screen.width - 20f, 550f * escala);
        float alto = Mathf.Min(Screen.height - 20f, 267f * escala);
        Rect caja = new Rect((Screen.width - ancho) / 2f, (Screen.height - alto) / 2f, ancho, alto);

        Pintar(new Rect(caja.x + 7f, caja.y + 8f, caja.width, caja.height), new Color(0f, 0f, 0f, 0.65f));
        Pintar(caja, new Color(0.06f, 0.065f, 0.09f, 0.98f));
        Color borde = new Color(0.91f, 0.61f, 0.28f, 1f);
        float grosor = Mathf.Max(2f, 3f * escala);
        Pintar(new Rect(caja.x, caja.y, caja.width, grosor), borde);
        Pintar(new Rect(caja.x, caja.yMax - grosor, caja.width, grosor), borde);
        Pintar(new Rect(caja.x, caja.y, grosor, caja.height), borde);
        Pintar(new Rect(caja.xMax - grosor, caja.y, grosor, caja.height), borde);

        Etiqueta(new Rect(caja.x + 10f, caja.y + 18f * escala, ancho - 20f, 38f * escala),
            "CAISTE AL ABISMO", 32, escala, FontStyle.Bold,
            new Color(1f, 0.43f, 0.38f), TextAnchor.MiddleCenter);

        Etiqueta(new Rect(caja.x + 12f, caja.y + 72f * escala, ancho - 24f, 35f * escala),
            "La oscuridad te ha vencido...", 18, escala, FontStyle.Normal,
            new Color(0.98f, 0.93f, 0.82f), TextAnchor.MiddleCenter);

        Etiqueta(new Rect(caja.x + 12f, caja.y + 116f * escala, ancho - 24f, 30f * escala),
            "Salta los huecos con ESPACIO", 16, escala, FontStyle.Normal,
            new Color(0.83f, 0.85f, 0.88f), TextAnchor.MiddleCenter);

        Rect boton = new Rect(caja.x + ancho * 0.16f, caja.yMax - 85f * escala,
            ancho * 0.68f, 56f * escala);
        Pintar(boton, new Color(0.35f, 0.12f, 0.12f, 1f));
        Pintar(new Rect(boton.x, boton.y, boton.width, grosor), borde);

        GUIStyle estiloBoton = new GUIStyle(GUI.skin.button);
        estiloBoton.fontSize = Mathf.RoundToInt(20f * escala);
        estiloBoton.fontStyle = FontStyle.Bold;
        estiloBoton.alignment = TextAnchor.MiddleCenter;
        estiloBoton.normal.textColor = Color.white;
        estiloBoton.hover.textColor = Color.white;
        estiloBoton.active.textColor = Color.white;
        // Ya dibujamos nuestro fondo, dejamos transparente el estilo del boton.
        estiloBoton.normal.background = null;
        estiloBoton.hover.background = null;
        estiloBoton.active.background = null;

        if (GUI.Button(boton, "[R] VOLVER A INTENTAR", estiloBoton))
            reintentarSolicitado = true;
    }

    private void Pintar(Rect rect, Color color)
    {
        Color antes = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = antes;
    }

    private static void Etiqueta(Rect rect, string mensaje, int tamano, float escala,
        FontStyle grosor, Color color, TextAnchor alineacion)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = Mathf.RoundToInt(tamano * escala);
        estilo.fontStyle = grosor;
        estilo.alignment = alineacion;
        estilo.normal.textColor = color;
        estilo.wordWrap = true;
        GUI.Label(rect, mensaje, estilo);
    }

    private void OnDestroy()
    {
        if (pixel != null)
            Destroy(pixel);
    }
}
