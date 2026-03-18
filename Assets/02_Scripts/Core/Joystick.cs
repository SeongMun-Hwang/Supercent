using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float range = 100f;

    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out position))
        {
            position.x = -(position.x / background.sizeDelta.x);
            position.y = -(position.y / background.sizeDelta.y);

            InputDirection = new Vector2(position.x * 2, position.y * 2);
            InputDirection = (InputDirection.magnitude > 1.0f) ? InputDirection.normalized : InputDirection;

            handle.anchoredPosition = new Vector2(InputDirection.x * -(background.sizeDelta.x / 2), InputDirection.y * -(background.sizeDelta.y / 2));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputDirection = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
