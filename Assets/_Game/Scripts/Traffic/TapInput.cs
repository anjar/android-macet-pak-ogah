using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace Macet
{
    public sealed class TapInput : MonoBehaviour
    {
        public Camera view;
        public TrafficManager traffic;
        void Update()
        {
            if (!traffic.IsPlaying) return;
            Vector2 position;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                position = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                position = Mouse.current.position.ReadValue();
            else return;
            // UI is tested at the actual pointer position, including first touch frame.
            var data = new PointerEventData(EventSystem.current) { position = position };
            uiHits.Clear(); EventSystem.current.RaycastAll(data, uiHits);
            if (uiHits.Count > 0) return;
            if (Physics.Raycast(view.ScreenPointToRay(position), out var hit, 200f))
            {
                var entity = hit.collider.GetComponentInParent<TrafficEntity>();
                if (entity != null) traffic.Tap(entity);
            }
        }
        readonly System.Collections.Generic.List<RaycastResult> uiHits = new System.Collections.Generic.List<RaycastResult>(16);
    }
}
