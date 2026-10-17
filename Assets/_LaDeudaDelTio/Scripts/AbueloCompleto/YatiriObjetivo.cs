using UnityEngine;

// Pon este componente en futuros enemigos para que reciban los ataques.
public class YatiriObjetivo : MonoBehaviour
{
    [Min(1)] public int vida = 3;

    public void RecibirImpacto(int danio)
    {
        vida -= Mathf.Max(0, danio);
        if (vida <= 0) Destroy(gameObject);
    }
}
