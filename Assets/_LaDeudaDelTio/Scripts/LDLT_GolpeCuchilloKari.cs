using UnityEngine;

// Complemento de AbueloAtaqueCuchillo. NO lo reemplaza ni modifica.
// Detecta UN impacto al comenzar cada cuchillada (tecla J).
[DisallowMultipleComponent]
[RequireComponent(typeof(AbueloAtaqueCuchillo))]
public class LDLT_GolpeCuchilloKari : MonoBehaviour
{
    [Header("Area de golpe (se dibuja en Scene)")]
    [Min(0.1f)] public float distanciaAdelante = 1.05f;
    public float altura = 0.4f;
    [Min(0.1f)] public float radio = 0.95f;
    [Min(0f)] public float demoraGolpe = 0.055f;

    private AbueloAtaqueCuchillo ataque;
    private SpriteRenderer sprite;
    private bool ataqueAnterior;
    private bool golpeRegistrado;
    private float inicioAtaque;

    private void Awake()
    {
        ataque = GetComponent<AbueloAtaqueCuchillo>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (ataque == null) return;
        bool activo = ataque.IsAttacking;
        if (activo && !ataqueAnterior)
        {
            inicioAtaque = Time.time;
            golpeRegistrado = false;
        }
        if (activo && !golpeRegistrado && Time.time - inicioAtaque >= demoraGolpe)
        {
            golpeRegistrado = true;
            DanoConCuchillo();
        }
        ataqueAnterior = activo;
    }

    private Vector2 CentroGolpe()
    {
        float direccion = (sprite != null && sprite.flipX) ? -1f : 1f;
        return (Vector2)transform.position + new Vector2(distanciaAdelante * direccion, altura);
    }

    private void DanoConCuchillo()
    {
        Collider2D[] detectados = Physics2D.OverlapCircleAll(CentroGolpe(), radio);
        foreach (Collider2D collider in detectados)
        {
            if (collider == null) continue;
            LDLT_KariKariEnemigo enemigo = collider.GetComponentInParent<LDLT_KariKariEnemigo>();
            if (enemigo != null) enemigo.RecibirCuchillazo();
        }
    }

    private void OnDrawGizmosSelected()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float direccion = sr != null && sr.flipX ? -1f : 1f;
        Vector3 centro = transform.position + new Vector3(distanciaAdelante * direccion, altura, 0f);
        Gizmos.color = new Color(1f, 0.7f, 0.12f, 0.85f);
        Gizmos.DrawWireSphere(centro, radio);
    }
}
