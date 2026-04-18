using UnityEngine;
using UnityEngine.EventSystems;

public class NeuronView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public NeuronNode Node { get; set; }
    public GameSceneController Controller { get; set; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Controller != null) Controller.OnNeuronDragStart(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Controller != null) Controller.OnNeuronDrag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Controller != null) Controller.OnNeuronDragEnd(this, eventData);
    }
}
