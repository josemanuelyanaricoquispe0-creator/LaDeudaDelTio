using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Cinematicas: imagen completa con el texto ya incluido en la propia ilustracion.
// ENTER avanza una imagen y, al terminar, cambia de escena o muestra FIN.
public class LDLT_CinematicaImagenes : MonoBehaviour
{
    [SerializeField] private Sprite[] paginas;
    [SerializeField] private Image imagenPantalla;
    [SerializeField] private CanvasGroup fundidoImagen;
    [SerializeField] private string siguienteEscena;
    [SerializeField] private bool esFinalDelJuego;
    [SerializeField] private GameObject panelFinal;
    [SerializeField] private CanvasGroup fundidoFinal;
    [SerializeField, Range(0.05f, 1f)] private float tiempoTransicion = 0.22f;

    private int paginaActual;
    private bool cambiando;
    private bool terminado;

    // El configurador del Editor rellena estos campos automaticamente.
    public void Preparar(Sprite[] sprites, string escenaSiguiente, bool esFinal,
        Image imagen, CanvasGroup grupoImagen, GameObject fin, CanvasGroup grupoFin)
    {
        paginas = sprites;
        siguienteEscena = escenaSiguiente;
        esFinalDelJuego = esFinal;
        imagenPantalla = imagen;
        fundidoImagen = grupoImagen;
        panelFinal = fin;
        fundidoFinal = grupoFin;
    }

    private void Start()
    {
        if (paginas == null || paginas.Length == 0 || imagenPantalla == null || fundidoImagen == null)
        {
            Debug.LogError("CINEMATICA: faltan imagenes o referencias. Ejecuta de nuevo el configurador.", this);
            enabled = false;
            return;
        }

        paginaActual = 0;
        imagenPantalla.sprite = paginas[0];
        imagenPantalla.preserveAspect = true;
        fundidoImagen.alpha = 1f;
        if (panelFinal != null) panelFinal.SetActive(false);
    }

    private void Update()
    {
        if (cambiando || terminado || !PulsaronEnter()) return;

        if (paginaActual < paginas.Length - 1)
            StartCoroutine(AvanzarPagina());
        else if (esFinalDelJuego)
            StartCoroutine(MostrarFinal());
        else
            StartCoroutine(IrASiguienteEscena());
    }

    private static bool PulsaronEnter()
    {
        bool pulsado = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        pulsado |= Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado != null)
            pulsado |= teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame;
#endif
        return pulsado;
    }

    private IEnumerator AvanzarPagina()
    {
        cambiando = true;
        yield return CambiarAlpha(fundidoImagen, 1f, 0f, tiempoTransicion);
        paginaActual++;
        imagenPantalla.sprite = paginas[paginaActual];
        yield return CambiarAlpha(fundidoImagen, 0f, 1f, tiempoTransicion);
        cambiando = false;
    }

    private IEnumerator IrASiguienteEscena()
    {
        cambiando = true;
        if (string.IsNullOrEmpty(siguienteEscena) || !Application.CanStreamedLevelBeLoaded(siguienteEscena))
        {
            Debug.LogError("CINEMATICA: la siguiente escena '" + siguienteEscena +
                "' no esta habilitada en Build Profiles > Scene List.", this);
            cambiando = false;
            yield break;
        }

        yield return CambiarAlpha(fundidoImagen, 1f, 0f, 0.55f);
        SceneManager.LoadScene(siguienteEscena);
    }

    private IEnumerator MostrarFinal()
    {
        cambiando = true;
        yield return CambiarAlpha(fundidoImagen, 1f, 0f, 0.7f);
        if (panelFinal != null && fundidoFinal != null)
        {
            panelFinal.SetActive(true);
            yield return CambiarAlpha(fundidoFinal, 0f, 1f, 1.2f);
        }
        terminado = true;
    }

    private static IEnumerator CambiarAlpha(CanvasGroup grupo, float desde, float hasta, float segundos)
    {
        float transcurrido = 0f;
        grupo.alpha = desde;
        while (transcurrido < segundos)
        {
            transcurrido += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(transcurrido / segundos));
            yield return null;
        }
        grupo.alpha = hasta;
    }
}
