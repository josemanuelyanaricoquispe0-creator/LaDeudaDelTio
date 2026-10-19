
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalController : MonoBehaviour
{
    [Header("Panel de créditos")]
    [SerializeField] private GameObject panelCreditos;

    [Header("Nombre de la escena")]
    [SerializeField] private string escenaCapitulos = "SeleccionCapitulos";

    private void Start()
    {
        // Los créditos comienzan ocultos.
        if (panelCreditos != null)
        {
            panelCreditos.SetActive(false);
        }
    }

    // BOTÓN START
    public void Jugar()
    {
        SceneManager.LoadScene(escenaCapitulos);
    }

    // BOTÓN CREDITS
    public void MostrarCreditos()
    {
        if (panelCreditos != null)
        {
            panelCreditos.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Falta asignar PanelCreditos.");
        }
    }

    // BOTÓN VOLVER DE CRÉDITOS
    public void CerrarCreditos()
    {
        if (panelCreditos != null)
        {
            panelCreditos.SetActive(false);
        }
    }

    // BOTÓN EXIT
    public void Salir()
    {
        Debug.Log("Cerrando Secretos de la Pachamama...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
