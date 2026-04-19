using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class ButtonCursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        var sel = GetComponent<Selectable>();
        if (sel == null || !sel.interactable) return;
        if (CursorManager.Instance != null) CursorManager.Instance.SetClick();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CursorManager.Instance != null) CursorManager.Instance.SetIdle();
    }
}
