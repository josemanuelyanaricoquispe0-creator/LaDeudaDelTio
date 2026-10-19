using System.Collections.Generic;
using UnityEngine;

// Generador de Kari Kari para La Deuda del Tio.
// Los enemigos aparecen progresivamente fuera de la camara.

[DisallowMultipleComponent]
public class LDLT_SpawnerKariKari : MonoBehaviour
{
    [Header("Referencias")]
    public LDLT_KariKariEnemigo prefabKariKari;
    public Transform abuelo;
    public Camera camaraJuego;

    [Header("Cantidad de enemigos")]
    [Min(1)] public int totalAparecer = 15;
    [Min(1)] public int maximoSimultaneos = 2;
    [Min(0.5f)] public float segundosEntreApariciones = 3f;

    [Header("Progreso del jugador")]
    [Tooltip("-1 = izquierda, 1 = derecha")]
    public int direccionAvance = -1;

    [Min(0.5f)]
    public float metrosEntreApariciones = 3.5f;

    [Header("Aparicion fuera de camara")]
    [Min(2f)] public float distanciaMinima = 6f;
    [Min(3f)] public float distanciaMaxima = 35f;

    [Min(0.5f)]
    public float margenFueraCamara = 2f;

    [Header("Deteccion del suelo")]
    public LayerMask capasSuelo = ~0;

    [Min(1f)] public float alturaRaycast = 6f;
    [Min(1f)] public float longitudRaycast = 20f;

    [Min(0f)]
    public float margenEncimaSuelo = 0.05f;

    public int TotalGenerados { get; private set; }
    public int TotalDerrotados { get; private set; }

    public int EnemigosActivos => enemigosActivos.Count;

    private readonly List<LDLT_KariKariEnemigo> enemigosActivos =
        new List<LDLT_KariKariEnemigo>();

    private LDLT_VidaAbuelo vidaAbuelo;

    private float siguienteAparicion;
    private float ultimaPosicionAvanzada;
    private float metrosAvanzados;

    private bool reemplazoPendiente;
    private bool avisoCamara;
    private bool avisoSuelo;

    private void Start()
    {
        if (camaraJuego == null)
            camaraJuego = Camera.main;

        if (abuelo != null)
        {
            vidaAbuelo = abuelo.GetComponent<LDLT_VidaAbuelo>();
            ultimaPosicionAvanzada = abuelo.position.x;
        }

        siguienteAparicion = Time.time + 1f;
    }

    private void Update()
    {
        if (prefabKariKari == null || abuelo == null)
            return;

        if (vidaAbuelo != null && vidaAbuelo.EstaDerrotado)
            return;

        enemigosActivos.RemoveAll(enemigo => enemigo == null);

        RegistrarAvance();

        if (TotalGenerados >= totalAparecer)
            return;

        if (enemigosActivos.Count >= maximoSimultaneos)
            return;

        if (Time.time < siguienteAparicion)
            return;

        // Aparecen cuando el jugador avanza
        // o cuando debe reemplazarse un enemigo derrotado.
        if (metrosAvanzados < metrosEntreApariciones &&
            !reemplazoPendiente)
        {
            return;
        }

        if (camaraJuego == null)
        {
            if (!avisoCamara)
            {
                Debug.LogWarning(
                    "Spawner Kari Kari: asigna la Main Camera.",
                    this
                );

                avisoCamara = true;
            }

            return;
        }

        if (BuscarPuntoAparicion(out Vector3 posicion))
        {
            GenerarEnemigo(posicion);

            metrosAvanzados = 0f;
            reemplazoPendiente = false;

            siguienteAparicion =
                Time.time + segundosEntreApariciones;
        }
        else
        {
            siguienteAparicion = Time.time + 1f;

            if (!avisoSuelo)
            {
                Debug.LogWarning(
                    "Spawner Kari Kari: no encontro un suelo " +
                    "valido fuera de camara. Revisa la direccion, " +
                    "el alcance y las capas de suelo.",
                    this
                );

                avisoSuelo = true;
            }
        }
    }

    private void RegistrarAvance()
    {
        int direccion = direccionAvance < 0 ? -1 : 1;

        float avance =
            (abuelo.position.x - ultimaPosicionAvanzada) *
            direccion;

        if (avance > 0f)
        {
            metrosAvanzados += avance;
            ultimaPosicionAvanzada = abuelo.position.x;
        }
    }

    private bool BuscarPuntoAparicion(out Vector3 posicion)
    {
        posicion = Vector3.zero;

        int direccion = direccionAvance < 0 ? -1 : 1;

        float minimo = Mathf.Max(2f, distanciaMinima);
        float maximo = Mathf.Max(minimo + 1f, distanciaMaxima);

        for (int intento = 0; intento < 25; intento++)
        {
            float distancia = Random.Range(minimo, maximo);

            float x =
                abuelo.position.x + direccion * distancia;

            // Nunca generar un enemigo dentro de la camara.
            if (!EstaFueraDeCamara(x, direccion))
                continue;

            Vector2 origen = new Vector2(
                x,
                abuelo.position.y + alturaRaycast
            );

            RaycastHit2D[] impactos = Physics2D.RaycastAll(
                origen,
                Vector2.down,
                longitudRaycast,
                capasSuelo
            );

            foreach (RaycastHit2D impacto in impactos)
            {
                if (impacto.collider == null)
                    continue;

                if (impacto.collider.isTrigger)
                    continue;

                if (impacto.normal.y < 0.5f)
                    continue;

                // Evitamos generar sobre objetos moviles.
                if (impacto.rigidbody != null &&
                    impacto.rigidbody.bodyType != RigidbodyType2D.Static)
                {
                    continue;
                }

                posicion = new Vector3(
                    x,
                    impacto.point.y,
                    0f
                );

                return true;
            }
        }

        return false;
    }

    private bool EstaFueraDeCamara(float x, int direccion)
    {
        if (camaraJuego == null)
            return false;

        // Convertimos los extremos de la camara
        // a coordenadas del mundo.
        float profundidad =
            Mathf.Abs(camaraJuego.transform.position.z);

        Vector3 izquierda = camaraJuego.ViewportToWorldPoint(
            new Vector3(0f, 0.5f, profundidad)
        );

        Vector3 derecha = camaraJuego.ViewportToWorldPoint(
            new Vector3(1f, 0.5f, profundidad)
        );

        float bordeIzquierdo = Mathf.Min(
            izquierda.x,
            derecha.x
        );

        float bordeDerecho = Mathf.Max(
            izquierda.x,
            derecha.x
        );

        // Consideramos el ancho del sprite
        // para que no aparezca parcialmente en pantalla.
        float mitadAnchoEnemigo = 1f;

        SpriteRenderer spriteEnemigo =
            prefabKariKari.GetComponent<SpriteRenderer>();

        if (spriteEnemigo != null &&
            spriteEnemigo.sprite != null)
        {
            mitadAnchoEnemigo =
                spriteEnemigo.sprite.bounds.extents.x *
                Mathf.Abs(prefabKariKari.transform.localScale.x);
        }

        float margenTotal =
            mitadAnchoEnemigo + margenFueraCamara;

        if (direccion > 0)
        {
            return x > bordeDerecho + margenTotal;
        }

        return x < bordeIzquierdo - margenTotal;
    }

    private void GenerarEnemigo(Vector3 posicion)
    {
        LDLT_KariKariEnemigo enemigo = Instantiate(
            prefabKariKari,
            posicion,
            Quaternion.identity
        );

        enemigo.Configurar(abuelo, this);

        // Colocar los pies encima del piso.
        Collider2D colision = enemigo.GetComponent<Collider2D>();

        if (colision != null)
        {
            float correccion =
                posicion.y -
                colision.bounds.min.y +
                margenEncimaSuelo;

            enemigo.transform.position +=
                new Vector3(0f, correccion, 0f);
        }

        enemigosActivos.Add(enemigo);

        TotalGenerados++;
    }

    public void NotificarDerrota(LDLT_KariKariEnemigo enemigo)
    {
        if (enemigosActivos.Remove(enemigo))
        {
            TotalDerrotados++;

            // Permitimos que aparezca otro,
            // respetando el tiempo y el limite de enemigos.
            reemplazoPendiente = true;

            siguienteAparicion = Mathf.Max(
                siguienteAparicion,
                Time.time + segundosEntreApariciones
            );
        }
    }
}