using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// CONTROL DE LA BATALLA FINAL DEL TIO
// SOLO AFECTA A LA ESCENA DONDE SE COLOQUE.
public class LDLT_ControlJefeFinal : MonoBehaviour
{
    [Header("PERSONAJES")]
    public Transform abuelo;
    public Animator diablo;
    public Animator altar;

    [Header("ENEMIGOS")]
    public GameObject prefabKariKari;
    public Transform spawnerIzquierdo;
    public Transform spawnerDerecho;

    [Header("INVOCACIONES")]
    public float segundosEntreInvocaciones = 5f;
    public int enemigosPorLado = 3;
    public int maximoEnemigosVivos = 12;
    public float separacionEnemigos = 0.6f;

    [Header("VIDA DEL ALTAR")]
    public int vidaMaximaAltar = 30;
    public int danoPorCuchillazo = 2;
    public float distanciaGolpe = 1.2f;

    [Header("FINAL")]
    public float esperaAntesDeOscurecer = 1.5f;
    public float duracionOscurecimiento = 4f;
    public string escenaFinal = "09_Cinematica_RescateFinal";

    private AbueloAtaqueCuchillo cuchillo;
    private SpriteRenderer imagenAltar;

    private readonly List<GameObject> enemigos =
        new List<GameObject>();

    private int vidaActual;
    private float tiempoInvocacion;
    private float oscuridad;

    private bool atacabaAntes;
    private bool batallaTerminada;

    private Coroutine animacionAltar;
    private Coroutine animacionDiablo;

    private void Start()
    {
        vidaActual = vidaMaximaAltar;

        if (abuelo != null)
        {
            cuchillo = abuelo.GetComponent<AbueloAtaqueCuchillo>();
        }

        if (altar != null)
        {
            imagenAltar = altar.GetComponentInChildren<SpriteRenderer>();
        }

        if (cuchillo == null)
        {
            Debug.LogWarning(
                "JEFE FINAL: falta conectar el Player con su cuchillo."
            );
        }

        Reproducir(diablo, "Reposo");
        Reproducir(altar, "Reposo");
    }

    private void Update()
    {
        if (batallaTerminada)
            return;

        RevisarCuchillo();
        ControlarInvocaciones();
    }

    // REVISAMOS CADA CUCHILLAZO DEL ABUELO
    private void RevisarCuchillo()
    {
        if (cuchillo == null || abuelo == null || altar == null)
            return;

        bool atacandoAhora = cuchillo.IsAttacking;

        if (atacandoAhora && !atacabaAntes)
        {
            ComprobarGolpeAltar();
        }

        atacabaAntes = atacandoAhora;
    }

    // EL ALTAR RECIBE 2 DE DANO POR GOLPE
    private void ComprobarGolpeAltar()
    {
        if (imagenAltar == null)
            return;

        Vector3 puntoCercano =
            imagenAltar.bounds.ClosestPoint(abuelo.position);

        float distancia = Vector2.Distance(
            abuelo.position,
            puntoCercano
        );

        if (distancia > distanciaGolpe)
            return;

        vidaActual -= danoPorCuchillazo;

        vidaActual = Mathf.Max(0, vidaActual);

        Debug.Log(
            "ALTAR DEL TIO - VIDA: " +
            vidaActual + "/" + vidaMaximaAltar
        );

        if (vidaActual <= 0)
        {
            DestruirAltar();
        }
        else
        {
            if (animacionAltar != null)
                StopCoroutine(animacionAltar);

            Reproducir(altar, "Danio");

            animacionAltar = StartCoroutine(
                VolverAReposo(altar, "Danio", 0.35f)
            );
        }
    }

    // EL DIABLO INVOCA CADA 5 SEGUNDOS
    private void ControlarInvocaciones()
    {
        tiempoInvocacion += Time.deltaTime;

        if (tiempoInvocacion < segundosEntreInvocaciones)
            return;

        tiempoInvocacion = 0f;

        if (animacionDiablo != null)
            StopCoroutine(animacionDiablo);

        Reproducir(diablo, "Invocar");

        animacionDiablo = StartCoroutine(
            VolverAReposo(diablo, "Invocar", 1f)
        );

        GenerarEnemigos();
    }

    private void GenerarEnemigos()
    {
        if (prefabKariKari == null)
            return;

        enemigos.RemoveAll(
            e => e == null || !e.activeInHierarchy
        );

        int disponibles =
            maximoEnemigosVivos - enemigos.Count;

        if (disponibles <= 0)
            return;

        GenerarDesdeLado(
            spawnerIzquierdo,
            1,
            ref disponibles
        );

        GenerarDesdeLado(
            spawnerDerecho,
            -1,
            ref disponibles
        );
    }

    private void GenerarDesdeLado(
        Transform punto,
        int direccion,
        ref int disponibles)
    {
        if (punto == null)
            return;

        for (int i = 0; i < enemigosPorLado; i++)
        {
            if (disponibles <= 0)
                break;

            Vector3 posicion = punto.position;

            posicion.x +=
                direccion * i * separacionEnemigos;

            GameObject enemigo = Instantiate(
                prefabKariKari,
                posicion,
                Quaternion.identity
            );

            enemigos.Add(enemigo);

            disponibles--;
        }
    }

    // REPRODUCIMOS UN ESTADO DEL ANIMATOR
    private void Reproducir(Animator animador, string estado)
    {
        if (animador == null)
            return;

        int hash = Animator.StringToHash(
            "Base Layer." + estado
        );

        if (animador.HasState(0, hash))
        {
            animador.Play(hash, 0, 0f);
        }
        else
        {
            Debug.LogWarning(
                "No existe el estado " + estado +
                " en " + animador.name
            );
        }
    }

    // REGRESAMOS A REPOSO DESPUES DE LA ANIMACION
    private IEnumerator VolverAReposo(
        Animator animador,
        string estadoActual,
        float tiempoMinimo)
    {
        float espera = tiempoMinimo;

        if (animador != null &&
            animador.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in
                     animador.runtimeAnimatorController.animationClips)
            {
                if (clip != null &&
                    clip.name.IndexOf(
                        estadoActual,
                        System.StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    espera = Mathf.Max(
                        tiempoMinimo,
                        clip.length
                    );

                    break;
                }
            }
        }

        yield return new WaitForSeconds(espera);

        if (!batallaTerminada)
        {
            Reproducir(animador, "Reposo");
        }
    }

    // EL ALTAR SE DESTRUYE Y EL DIABLO MUERE
    private void DestruirAltar()
    {
        batallaTerminada = true;

        if (animacionAltar != null)
            StopCoroutine(animacionAltar);

        if (animacionDiablo != null)
            StopCoroutine(animacionDiablo);

        Reproducir(altar, "Destruccion");
        Reproducir(diablo, "Muerte");

        // DETENEMOS A LOS KARI KARI RESTANTES
        foreach (GameObject enemigo in enemigos)
        {
            if (enemigo != null)
                Destroy(enemigo);
        }

        // BLOQUEAMOS EL MOVIMIENTO DURANTE EL FINAL
        if (abuelo != null)
        {
            PlayerControlLock bloqueo =
                abuelo.GetComponent<PlayerControlLock>();

            if (bloqueo != null)
                bloqueo.Acquire(this);
        }

        StartCoroutine(FinalDeLaBatalla());
    }

    // OSCURECIMIENTO PROGRESIVO DE LA PANTALLA
    private IEnumerator FinalDeLaBatalla()
    {
        yield return new WaitForSecondsRealtime(
            esperaAntesDeOscurecer
        );

        float tiempo = 0f;

        while (tiempo < duracionOscurecimiento)
        {
            tiempo += Time.unscaledDeltaTime;

            oscuridad = Mathf.Clamp01(
                tiempo / duracionOscurecimiento
            );

            yield return null;
        }

        oscuridad = 1f;

        yield return new WaitForSecondsRealtime(0.5f);

        if (Application.CanStreamedLevelBeLoaded(escenaFinal))
        {
            SceneManager.LoadScene(escenaFinal);
        }
        else
        {
            Debug.LogError(
                "No se encontro la escena: " + escenaFinal +
                ". Revisar Build Profiles > Scene List."
            );
        }
    }

    // CAPA NEGRA QUE CUBRE LA PANTALLA POCO A POCO
    private void OnGUI()
    {
        if (oscuridad <= 0f)
            return;

        GUI.depth = -10000;

        Color colorAnterior = GUI.color;

        GUI.color = new Color(
            0f, 0f, 0f, oscuridad
        );

        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture
        );

        GUI.color = colorAnterior;
    }
}