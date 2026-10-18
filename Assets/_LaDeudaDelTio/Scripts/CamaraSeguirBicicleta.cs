using UnityEngine;

// Camara exclusiva para 03_PersecucionBicicleta.
// No modifica los controles ni las animaciones del Abuelo Chaman.
[RequireComponent(typeof(Camera))]
public class CamaraSeguirBicicleta : MonoBehaviour
{
    [Header("Abuelo en bicicleta")]
    public Transform objetivo;

    [Header("Encuadre")]
    [Tooltip("X positivo muestra mas camino delante de la bicicleta.")]
    public float adelantoHorizontal = 2.5f;
    [Tooltip("Ajusta la altura del encuadre con respecto a la bicicleta.")]
    public float altura = 1.5f;
    [Tooltip("Profundidad de la camara 2D; normalmente -10.")]
    public float profundidad = -10f;

    [Header("Movimiento suave")]
    [Min(0.01f)] public float suavizado = 0.18f;

    private Vector3 velocidad;

    private Vector3 CalcularPosicion()
    {
        Vector3 posicion = objetivo.position;
        return new Vector3(posicion.x + adelantoHorizontal,
                           posicion.y + altura,
                           profundidad);
    }

    private void Start()
    {
        if (objetivo != null)
            transform.position = CalcularPosicion();
        else
            Debug.LogWarning("CamaraSeguirBicicleta: asigna LDLT_AbueloBicicleta en el campo Objetivo.", this);
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            CalcularPosicion(),
            ref velocidad,
            suavizado);
    }
}
