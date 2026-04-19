using UnityEngine;
using UnityEngine.EventSystems;

public class NeuronView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public NeuronNode Node { get; set; }
    public GameSceneController Controller { get; set; }

    bool dragging;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dragging) return;
        if (Node is InputNode && CursorManager.Instance != null)
        {
            CursorManager.Instance.SetPassive();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dragging) return;
        if (CursorManager.Instance != null) CursorManager.Instance.SetIdle();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        if (CursorManager.Instance != null) CursorManager.Instance.SetActive();
        if (Controller != null) Controller.OnNeuronDragStart(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Controller != null) Controller.OnNeuronDrag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        if (Controller != null) Controller.OnNeuronDragEnd(this, eventData);
        if (CursorManager.Instance != null) CursorManager.Instance.SetIdle();
    }
}
