using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Punto de entrada a la cinematica del cerro.
// No altera los controles, las animaciones ni el prefab del Player.
public class EntradaCerro : MonoBehaviour
{
    [Header("Interaccion con el cerro")]
    public Transform jugador;
    [Min(0.5f)] public float distancia = 2.5f;
    public string escenaCinematica = "02_Cinematica_Cerro";

    [Header("Textos de la notificacion")]
    public string titulo = "SUBIR AL CERRO";
    public string subtitulo = "Un viejo amigo te espera arriba...";

    private Texture2D pixelBlanco;
    private float aparicion;
    private bool cambiandoEscena;

    // Valores que solo se utilizan para dibujar la notificacion.
    private float escalaUI;
    private float panelX;
    private float panelY;
    private float opacidad;

    private void Awake()
    {
        pixelBlanco = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixelBlanco.name = "EntradaCerro_PixelUI";
        pixelBlanco.filterMode = FilterMode.Point;
        pixelBlanco.SetPixel(0, 0, Color.white);
        pixelBlanco.Apply();
    }

    private void Update()
    {
        bool cerca = jugador != null &&
                     Vector2.Distance(jugador.position, transform.position) <= distancia;

        // Aparicion/desaparicion suave, incluso si el juego usa pausa.
        aparicion = Mathf.MoveTowards(aparicion, cerca ? 1f : 0f,
                                      Time.unscaledDeltaTime * 4.5f);

        // Compatible con el nuevo Input System de Unity 6.
        if (!cerca || cambiandoEscena || Keyboard.current == null ||
            !Keyboard.current.oKey.wasPressedThisFrame)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(escenaCinematica) &&
            Application.CanStreamedLevelBeLoaded(escenaCinematica))
        {
            cambiandoEscena = true;
            SceneManager.LoadScene(escenaCinematica);
        }
        else
        {
            Debug.LogWarning("EntradaCerro: agrega la escena '" + escenaCinematica +
                             "' en File > Build Profiles > Scene List.");
        }
    }

    private void OnGUI()
    {
        if (pixelBlanco == null || aparicion <= 0.001f) return;
        if (Event.current.type != EventType.Repaint) return;

        // Prioridad frente a otros elementos IMGUI.
        GUI.depth = -80;

        // Se adapta a la resolucion de Game sin salirse de la pantalla.
        escalaUI = Mathf.Clamp(Mathf.Min(Screen.width / 1450f,
                                         Screen.height / 820f), 0.65f, 1.4f);
        escalaUI = Mathf.Min(escalaUI, Mathf.Max(0.25f, (Screen.width - 24f) / 615f));

        float ancho = 580f * escalaUI;
        float alto = 178f * escalaUI;
        float margen = Mathf.Max(10f, 24f * escalaUI);

        // Easing: aparece deslizando levemente desde la derecha.
        float t = aparicion * aparicion * (3f - 2f * aparicion);
        opacidad = t;
        panelX = Screen.width - ancho - margen + (1f - t) * 28f * escalaUI;
        panelY = margen + (1f - t) * 7f * escalaUI;

        // --- MARCO PRINCIPAL (NEGRO, ORO Y GRANATE) ---
        Dibujar(9, 12, 580, 178, 0f, 0f, 0f, 0.36f);                // sombra lejana
        Dibujar(4, 6, 580, 178, 0f, 0f, 0f, 0.48f);                 // sombra cercana
        Dibujar(0, 0, 580, 178, 0.64f, 0.42f, 0.21f, 1f);           // borde exterior
        Dibujar(2, 2, 576, 174, 0.035f, 0.049f, 0.066f, 0.98f);   // fondo nocturno
        Dibujar(6, 6, 568, 166, 0.075f, 0.086f, 0.093f, 0.91f);   // superficie interior
        Dibujar(6, 6, 6, 166, 0.46f, 0.13f, 0.17f, 0.98f);         // acento granate
        Dibujar(12, 6, 562, 3, 0.95f, 0.75f, 0.37f, 1f);           // brillo superior
        Dibujar(12, 170, 562, 2, 0.80f, 0.54f, 0.27f, 0.90f);      // brillo inferior

        // Remaches decorativos tipo marco antiguo.
        Dibujar(18, 17, 5, 5, 0.98f, 0.78f, 0.42f, 1f);
        Dibujar(558, 17, 5, 5, 0.98f, 0.78f, 0.42f, 1f);
        Dibujar(18, 155, 5, 5, 0.98f, 0.78f, 0.42f, 1f);
        Dibujar(558, 155, 5, 5, 0.98f, 0.78f, 0.42f, 1f);

        // --- TECLA GRANDE Y LEGIBLE ---
        float pulso = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * 3.2f);
        Dibujar(26, 37, 97, 93, 0.75f, 0.50f, 0.26f, pulso);
        Dibujar(29, 40, 91, 87, 0.13f, 0.11f, 0.12f, 1f);
        Dibujar(33, 44, 83, 79, 0.22f, 0.13f, 0.12f, 1f);
        Dibujar(39, 115, 71, 2, 0.97f, 0.74f, 0.38f, pulso);
        Texto(33, 36, 83, 21, "TECLA", 12, FontStyle.Bold, TextAnchor.MiddleCenter,
              new Color(0.98f, 0.81f, 0.56f, 1f));
        Texto(30, 53, 90, 66, "O", 55, FontStyle.Bold, TextAnchor.MiddleCenter,
              new Color(1f, 0.97f, 0.83f, 1f));

        // Separador central.
        Dibujar(135, 22, 2, 112, 0.68f, 0.48f, 0.30f, 0.50f);

        // --- TITULOS EN GRANDE, CON CONTRASTE Y SOMBRA ---
        Texto(153, 19, 397, 25, "EVENTO DE HISTORIA", 16,
              FontStyle.Bold, TextAnchor.MiddleLeft,
              new Color(1f, 0.76f, 0.38f, 1f));
        Texto(151, 44, 408, 50, titulo.ToUpperInvariant(), 32,
              FontStyle.Bold, TextAnchor.MiddleLeft,
              new Color(1f, 0.98f, 0.88f, 1f));
        Dibujar(153, 94, 245, 2, 0.92f, 0.68f, 0.36f, 0.62f);
        Texto(153, 100, 404, 27, subtitulo, 18,
              FontStyle.Normal, TextAnchor.MiddleLeft,
              new Color(0.94f, 0.94f, 0.90f, 1f));

        // --- BARRA INFERIOR CON INSTRUCCION CLARA ---
        Dibujar(13, 138, 554, 29, 0.075f, 0.045f, 0.053f, 0.97f);
        Dibujar(13, 138, 554, 1, 0.72f, 0.48f, 0.29f, 0.57f);
        Dibujar(153, 149, 7, 7, 0.99f, 0.77f, 0.36f, pulso);
        Texto(170, 139, 376, 27, "PRESIONA O PARA CONTINUAR", 16,
              FontStyle.Bold, TextAnchor.MiddleLeft,
              new Color(1f, 0.89f, 0.64f, 1f));
    }

    private Rect RectUI(float x, float y, float ancho, float alto)
    {
        return new Rect(panelX + x * escalaUI,
                        panelY + y * escalaUI,
                        ancho * escalaUI, alto * escalaUI);
    }

    private void Dibujar(float x, float y, float ancho, float alto,
                         float rojo, float verde, float azul, float alfa)
    {
        Color anterior = GUI.color;
        GUI.color = new Color(rojo, verde, azul, Mathf.Clamp01(alfa * opacidad));
        GUI.DrawTexture(RectUI(x, y, ancho, alto), pixelBlanco);
        GUI.color = anterior;
    }

    private void Texto(float x, float y, float ancho, float alto, string contenido,
                       int tamano, FontStyle grosor, TextAnchor alineacion, Color color)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = Mathf.Max(10, Mathf.RoundToInt(tamano * escalaUI));
        estilo.fontStyle = grosor;
        estilo.alignment = alineacion;
        estilo.wordWrap = false;
        estilo.clipping = TextClipping.Clip;
        estilo.padding = new RectOffset(0, 0, 0, 0);

        Color anterior = GUI.color;
        GUI.color = Color.white;

        Rect posicion = RectUI(x, y, ancho, alto);
        float borde = Mathf.Max(1f, escalaUI);
        Color sombra = new Color(0f, 0f, 0f, 0.85f * opacidad);
        estilo.normal.textColor = sombra;

        // Contorno oscuro para que las letras se lean aun sobre paisajes claros.
        GUI.Label(new Rect(posicion.x - borde, posicion.y, posicion.width, posicion.height), contenido, estilo);
        GUI.Label(new Rect(posicion.x + borde, posicion.y, posicion.width, posicion.height), contenido, estilo);
        GUI.Label(new Rect(posicion.x, posicion.y - borde, posicion.width, posicion.height), contenido, estilo);
        GUI.Label(new Rect(posicion.x, posicion.y + borde, posicion.width, posicion.height), contenido, estilo);

        estilo.normal.textColor = new Color(color.r, color.g, color.b,
                                           color.a * opacidad);
        GUI.Label(posicion, contenido, estilo);
        GUI.color = anterior;
    }

    private void OnDestroy()
    {
        if (pixelBlanco != null) Destroy(pixelBlanco);
    }
}
