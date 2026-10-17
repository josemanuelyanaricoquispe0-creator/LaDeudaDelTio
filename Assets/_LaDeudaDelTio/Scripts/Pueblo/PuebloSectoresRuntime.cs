using System.Collections;
using UnityEngine;

// Diez fondos dentro de UNA sola escena. Solo un sector queda visible a la vez.
// Se cambia de sector con un fundido corto: oculta cortes de edificios y lunas repetidas.
[DisallowMultipleComponent]
public class PuebloSectoresRuntime : MonoBehaviour
{
    public GameObject[] sectores;
    public Transform jugador;
    public Camera camara;
    public float anchoSector = 21.72f;
    public float limitePaso = 9.45f;
    public float reaparecerX = 8.55f;
    public float alturaCamara = 0f;
    public float duracionFundido = 0.18f;

    private int sectorActual;
    private bool cambiando;
    private float negro;
    private float velocidadCamara;
    private SpriteCookPlayerMovement movimiento;
    private Rigidbody2D cuerpo;

    private void Awake()
    {
        if (jugador != null)
        {
            movimiento = jugador.GetComponent<SpriteCookPlayerMovement>();
            cuerpo = jugador.GetComponent<Rigidbody2D>();
        }
        sectorActual = 0;
        for (int i = 0; i < sectores.Length; i++)
            if (sectores[i] != null) sectores[i].SetActive(i == sectorActual);
    }

    private void Start()
    {
        ColocarCamaraInmediata();
    }

    private void Update()
    {
        if (jugador == null || sectores == null || sectores.Length == 0 || cambiando) return;

        float x = jugador.position.x;
        float vx = cuerpo != null ? cuerpo.linearVelocity.x : 0f;

        if (x >= limitePaso && vx > 0.03f && sectorActual < sectores.Length - 1)
        {
            StartCoroutine(CambiarSector(sectorActual + 1, -reaparecerX));
        }
        else if (x <= -limitePaso && vx < -0.03f && sectorActual > 0)
        {
            StartCoroutine(CambiarSector(sectorActual - 1, reaparecerX));
        }

        // El jugador nunca puede salir por el extremo del pueblo.
        if (sectorActual == 0 && jugador.position.x < -limitePaso ||
            sectorActual == sectores.Length - 1 && jugador.position.x > limitePaso)
        {
            Vector3 p = jugador.position;
            p.x = Mathf.Clamp(p.x, -limitePaso, limitePaso);
            jugador.position = p;
            if (cuerpo != null) cuerpo.linearVelocity = new Vector2(0f, cuerpo.linearVelocity.y);
        }
    }

    private IEnumerator CambiarSector(int nuevo, float nuevaX)
    {
        cambiando = true;
        if (movimiento != null) movimiento.enabled = false;
        if (cuerpo != null) cuerpo.linearVelocity = new Vector2(0f, cuerpo.linearVelocity.y);

        yield return Fundir(0f, 1f);
        if (sectorActual >= 0 && sectorActual < sectores.Length && sectores[sectorActual] != null)
            sectores[sectorActual].SetActive(false);
        sectorActual = nuevo;
        if (sectores[sectorActual] != null) sectores[sectorActual].SetActive(true);
        Vector3 p = jugador.position;
        p.x = nuevaX;
        jugador.position = p;
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
        ColocarCamaraInmediata();
        yield return Fundir(1f, 0f);

        if (movimiento != null) movimiento.enabled = true;
        cambiando = false;
    }

    private IEnumerator Fundir(float desde, float hasta)
    {
        float t = 0f;
        float duracion = Mathf.Max(0.02f, duracionFundido);
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            negro = Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / duracion));
            yield return null;
        }
        negro = hasta;
    }

    private void LateUpdate()
    {
        if (camara == null || jugador == null) return;
        float medioAncho = camara.orthographicSize * camara.aspect;
        float maximo = Mathf.Max(0f, anchoSector / 2f - medioAncho);
        float objetivoX = Mathf.Clamp(jugador.position.x, -maximo, maximo);
        float x = Mathf.SmoothDamp(camara.transform.position.x, objetivoX, ref velocidadCamara, 0.16f);
        camara.transform.position = new Vector3(x, alturaCamara, -10f);
    }

    private void ColocarCamaraInmediata()
    {
        if (camara == null || jugador == null) return;
        float medioAncho = camara.orthographicSize * camara.aspect;
        float maximo = Mathf.Max(0f, anchoSector / 2f - medioAncho);
        camara.transform.position = new Vector3(Mathf.Clamp(jugador.position.x, -maximo, maximo), alturaCamara, -10f);
        velocidadCamara = 0f;
    }

    private void OnGUI()
    {
        if (negro <= 0.001f) return;
        Color antes = GUI.color;
        int profundidadAnterior = GUI.depth;
        GUI.depth = -1000;
        GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(negro));
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = antes;
        GUI.depth = profundidadAnterior;
    }
}
