using System;
using System.Collections.Generic;
using UnityEngine;
namespace Macet
{
    public sealed class TrafficManager : MonoBehaviour
    {
        public event Action AllExited;
        public event Action<TrafficEntity, TrafficEntity> Collided;
        public readonly List<TrafficEntity> Entities = new List<TrafficEntity>(30);
        public int Remaining { get; private set; }
        public bool IsPlaying { get; set; }
        CollisionManager collisions;
        public void Configure(CollisionManager collisionManager) { collisions = collisionManager; }
        public void SetEntities(IEnumerable<TrafficEntity> entities)
        {
            Entities.Clear(); Entities.AddRange(entities); Remaining = Entities.Count;
        }
        public bool Tap(TrafficEntity entity) => IsPlaying && Entities.Contains(entity) && entity.StartMoving();
        void FixedUpdate() { Tick(Time.fixedDeltaTime); }
        public void Tick(float delta)
        {
            if (!IsPlaying) return;
            Physics.SyncTransforms();
            foreach (var entity in Entities)
            {
                if (entity.State != EntityState.Moving) continue;
                var next = entity.NextPosition(delta);
                var other = collisions.Check(entity, next);
                if (other != null)
                {
                    entity.Crash(); other.Crash(); IsPlaying = false; Collided?.Invoke(entity, other); return;
                }
                if (entity.Advance(next))
                {
                    Remaining--;
                    if (Remaining == 0) { IsPlaying = false; AllExited?.Invoke(); return; }
                }
                Physics.SyncTransforms();
            }
        }
    }
}
