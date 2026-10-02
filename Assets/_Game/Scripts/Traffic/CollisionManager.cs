using UnityEngine;
namespace Macet
{
    public sealed class CollisionManager : MonoBehaviour
    {
        // Non-allocating overlap + sweep catches initial contact and tunnelling.
        readonly Collider[] overlaps = new Collider[64];
        readonly RaycastHit[] hits = new RaycastHit[64];
        public TrafficEntity Check(TrafficEntity entity, Vector3 next)
        {
            var box = entity.CollisionBounds;
            var center = entity.transform.TransformPoint(box.center);
            var half = Vector3.Scale(box.size * 0.5f, entity.transform.lossyScale);
            int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, entity.transform.rotation, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var other = overlaps[i].GetComponentInParent<TrafficEntity>();
                if (other != null && other != entity && other.State != EntityState.Exited) return other;
            }
            var delta = next - entity.transform.position;
            if (delta.sqrMagnitude < 0.0000001f) return null;
            count = Physics.BoxCastNonAlloc(center, half, delta.normalized, hits, entity.transform.rotation,
                delta.magnitude, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var other = hits[i].collider.GetComponentInParent<TrafficEntity>();
                if (other != null && other != entity && other.State != EntityState.Exited) return other;
            }
            return null;
        }
    }
}
