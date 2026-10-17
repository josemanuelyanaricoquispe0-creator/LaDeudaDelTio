using UnityEngine;

// Proyectil simple provisional; se reemplazara por arte y efectos finales.

public class YatiriProyectil : MonoBehaviour
{
    private static Sprite spriteCompartido;
    private Rigidbody2D cuerpo;
    private Transform creador;
    private float velocidad;
    private int danio;

    public static Sprite CrearSprite()
    {
        if (spriteCompartido != null) return spriteCompartido;
        const int ancho = 32;
        Texture2D textura = new Texture2D(ancho, ancho, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < ancho; y++)
        for (int x = 0; x < ancho; x++)
        {
            float dx = x - 15.5f;
            float dy = y - 15.5f;
            float intensidad = Mathf.Clamp01((16f - Mathf.Sqrt(dx * dx + dy * dy)) / 5f);
            textura.SetPixel(x, y, new Color(0.6f, 0.95f, 1f, intensidad));
        }
        textura.Apply();
        spriteCompartido = Sprite.Create(textura, new Rect(0, 0, ancho, ancho), new Vector2(0.5f, 0.5f), 32f);
        return spriteCompartido;
    }

    public void Inicializar(float velocidadHorizontal, int dano, Transform dueño)
    {
        cuerpo = GetComponent<Rigidbody2D>();
        velocidad = velocidadHorizontal;
        danio = dano;
        creador = dueño;
        Destroy(gameObject, 2.5f);
    }

    private void FixedUpdate()
    {
        if (cuerpo != null) cuerpo.linearVelocity = new Vector2(velocidad, 0f);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro == null || otro.isTrigger || (creador != null && otro.transform.IsChildOf(creador))) return;
        YatiriObjetivo objetivo = otro.GetComponentInParent<YatiriObjetivo>();
        if (objetivo != null)
        {
            objetivo.RecibirImpacto(danio);
            Destroy(gameObject);
        }
        else if (otro.gameObject.GetComponent<YatiriJugador>() == null)
        {
            // No atraviesa paredes ni suelo.
            Destroy(gameObject);
        }
    }
}
