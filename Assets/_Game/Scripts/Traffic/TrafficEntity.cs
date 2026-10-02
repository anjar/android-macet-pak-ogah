using UnityEngine;
namespace Macet
{
    [RequireComponent(typeof(BoxCollider))]
    public abstract class TrafficEntity : MonoBehaviour
    {
        public string Id { get; private set; }
        public EntityType EntityType { get; private set; }
        public float Speed { get; private set; }
        public EntityState State { get; private set; }
        public Route Route { get; private set; }
        public int CurrentWaypoint { get; private set; }
        public BoxCollider CollisionBounds { get; private set; }
        public Vector3 Size => CollisionBounds.size;
        public void Initialize(VehicleSpawnData data, Route route)
        {
            Id = data.id; EntityType = data.entityType; Speed = data.speed;
            Route = route; CurrentWaypoint = 1; State = EntityState.Waiting;
            CollisionBounds = GetComponent<BoxCollider>();
            transform.position = route.Points[0];
            Face(route.Points[1] - transform.position);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", data.color);
            GetComponentInChildren<Renderer>().SetPropertyBlock(block);
        }
        public bool StartMoving()
        {
            if (State != EntityState.Waiting) return false;
            State = EntityState.Moving; return true;
        }
        public Vector3 NextPosition(float delta)
        {
            Face(Route.Points[CurrentWaypoint] - transform.position);
            return Vector3.MoveTowards(transform.position, Route.Points[CurrentWaypoint], Speed * delta);
        }
        public bool Advance(Vector3 position)
        {
            Face(Route.Points[CurrentWaypoint] - transform.position);
            transform.position = position;
            if ((position - Route.Points[CurrentWaypoint]).sqrMagnitude > 0.000001f) return false;
            CurrentWaypoint++;
            if (CurrentWaypoint < Route.Points.Length) return false;
            State = EntityState.Exited; gameObject.SetActive(false); return true;
        }
        public void Crash() { State = EntityState.Crashed; }
        void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.000001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
