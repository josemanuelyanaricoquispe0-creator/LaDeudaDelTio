using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraSeguirAbuelo : MonoBehaviour
{
    [Header("Personaje")]
    public Transform jugador;

    [Header("Movimiento de la camara")]
    [Min(0.01f)] public float suavizado = 0.15f;

    [Header("Limites horizontales del pueblo (mundo)")]
    public float limiteIzquierdo = -10.86f;
    public float limiteDerecho = 206.34f;

    private Camera camara;
    private float velocidadX;
    private float alturaFija;
    private float profundidadFija;

    private void Awake()
    {
        camara = GetComponent<Camera>();
        alturaFija = transform.position.y;
        profundidadFija = transform.position.z;
    }

    private void LateUpdate()
    {
        if (jugador == null) return;

        float mitadPantalla = camara.orthographicSize * camara.aspect;
        float xMinimo = limiteIzquierdo + mitadPantalla;
        float xMaximo = limiteDerecho - mitadPantalla;

        float destinoX = (xMinimo <= xMaximo)
            ? Mathf.Clamp(jugador.position.x, xMinimo, xMaximo)
            : (limiteIzquierdo + limiteDerecho) * 0.5f;

        float nuevaX = Mathf.SmoothDamp(
            transform.position.x,
            destinoX,
            ref velocidadX,
            suavizado);

        transform.position = new Vector3(nuevaX, alturaFija, profundidadFija);
    }
}
