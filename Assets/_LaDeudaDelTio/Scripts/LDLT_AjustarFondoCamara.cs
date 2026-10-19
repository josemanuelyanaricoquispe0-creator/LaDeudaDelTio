using UnityEngine;

// Ajusta el fondo automaticamente al tamaño de la camara.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class LDLT_AjustarFondoCamara : MonoBehaviour
{
    [Header("Camara del escenario")]
    public Camera camara;

    [Header("Configuracion")]
    public bool llenarPantalla = true;

    private SpriteRenderer spriteRenderer;

    private void LateUpdate()
    {
        if (camara == null)
            return;

        if (!camara.orthographic)
            return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            return;

        float altoCamara = camara.orthographicSize * 2f;
        float anchoCamara = altoCamara * camara.aspect;

        Vector2 tamañoImagen = spriteRenderer.sprite.bounds.size;

        float escalaX = anchoCamara / tamañoImagen.x;
        float escalaY = altoCamara / tamañoImagen.y;

        float escalaFinal;

        if (llenarPantalla)
        {
            escalaFinal = Mathf.Max(escalaX, escalaY);
        }
        else
        {
            escalaFinal = Mathf.Min(escalaX, escalaY);
        }

        Vector3 escalaActual = transform.localScale;

        if (Mathf.Abs(escalaActual.x - escalaFinal) > 0.001f ||
            Mathf.Abs(escalaActual.y - escalaFinal) > 0.001f)
        {
            transform.localScale = new Vector3(
                escalaFinal,
                escalaFinal,
                escalaActual.z
            );
        }
    }
}