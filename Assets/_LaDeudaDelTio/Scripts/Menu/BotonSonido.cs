
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BotonSonido : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sonidos")]
    [SerializeField] private AudioClip sonidoHover;
    [SerializeField] private AudioClip sonidoClick;

    private Button boton;

    private void Awake()
    {
        boton = GetComponent<Button>();
    }

    // CUANDO EL MOUSE ENTRA AL BOTÓN
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!boton.IsInteractable()) return;

        if (audioSource != null && sonidoHover != null)
        {
            audioSource.PlayOneShot(sonidoHover);
        }
    }

    // CUANDO HACEMOS CLICK EN EL BOTÓN
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!boton.IsInteractable()) return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (audioSource != null && sonidoClick != null)
        {
            audioSource.PlayOneShot(sonidoClick);
        }
    }
}
