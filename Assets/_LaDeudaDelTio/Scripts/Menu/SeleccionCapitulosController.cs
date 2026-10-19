
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SeleccionCapitulosController : MonoBehaviour
{
    [Header("Fondos de capítulos")]
    public Image fondo;
    public Sprite fondo1;
    public Sprite fondo2;
    public Sprite fondo3;

    [Header("Botón Iniciar")]
    public TMP_Text textoIniciar;

    // Capítulo seleccionado actualmente
    private int capituloSeleccionado = 1;

    void Start()
    {
        Seleccionar(1);
    }

    // Recibe el número desde el botón
    public void Seleccionar(int numero)
    {
        capituloSeleccionado = numero;

        Debug.Log("Capítulo seleccionado: " + numero);

        switch (numero)
        {
            case 1:
                fondo.sprite = fondo1;
                textoIniciar.text = "INICIAR";
                break;

            case 2:
                fondo.sprite = fondo2;
                textoIniciar.text = "PRÓXIMAMENTE";
                break;

            case 3:
                fondo.sprite = fondo3;
                textoIniciar.text = "PRÓXIMAMENTE";
                break;
        }
    }

    public void Iniciar()
    {
        if (capituloSeleccionado == 1)
        {
            SceneManager.LoadScene("00_Cinematica_Introduccion");
        }
        else
        {
            Debug.Log("Capítulo próximamente disponible");
        }
    }

    public void Volver()
    {
        SceneManager.LoadScene("MenuPrincipal");
    }
}
