using System.Collections.Generic;
using UnityEngine;
namespace Macet
{
    public sealed class LevelLoader : MonoBehaviour
    {
        public Vehicle carPrefab;
        public Material roadMaterial;
        public Material routeMaterial;
        Transform levelRoot;
        public List<TrafficEntity> Load(LevelData data, Camera camera)
        {
            var errors = data.ValidateData();
            if (errors.Count > 0) throw new System.InvalidOperationException(string.Join("; ", errors));
            if (levelRoot != null) { levelRoot.gameObject.SetActive(false); Destroy(levelRoot.gameObject); }
            levelRoot = new GameObject("Level " + data.levelNumber).transform;
            var bounds = new Bounds(data.nodes[0].position, Vector3.one);
            foreach (var node in data.nodes) bounds.Encapsulate(node.position);
            foreach (var road in data.roads)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Road"; go.transform.SetParent(levelRoot);
                go.transform.position = (road.from + road.to) * 0.5f + Vector3.down * 0.56f;
                go.transform.rotation = Quaternion.LookRotation(road.to - road.from);
                go.transform.localScale = new Vector3(road.width, 0.12f, Vector3.Distance(road.from, road.to));
                go.GetComponent<Collider>().enabled = false;
                go.GetComponent<Renderer>().sharedMaterial = roadMaterial;
            }
            var entities = new List<TrafficEntity>();
            foreach (var spawn in data.vehicles)
            {
                var route = data.Resolve(spawn);
                var car = Instantiate(carPrefab, levelRoot);
                car.name = spawn.id; car.Initialize(spawn, route); entities.Add(car);
                var line = new GameObject("Route " + spawn.id).AddComponent<LineRenderer>();
                line.transform.SetParent(levelRoot);
                line.sharedMaterial = routeMaterial; line.startColor = line.endColor = spawn.color;
                line.startWidth = line.endWidth = 0.045f; line.positionCount = route.Points.Length;
                for (int i = 0; i < route.Points.Length; i++) line.SetPosition(i, route.Points[i] + Vector3.down * 0.43f);
            }
            camera.transform.rotation = Quaternion.Euler(60, 0, 0);
            camera.transform.position = bounds.center - camera.transform.forward * 24;
            camera.orthographic = true;
            // Fit all projected corners with space reserved for HUD and bottom instruction.
            float horizontal = 0, vertical = 0;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var corner = new Vector3(bounds.extents.x * x, 0, bounds.extents.z * z);
                horizontal = Mathf.Max(horizontal, Mathf.Abs(Vector3.Dot(corner, camera.transform.right)));
                vertical = Mathf.Max(vertical, Mathf.Abs(Vector3.Dot(corner, camera.transform.up)));
            }
            camera.orthographicSize = Mathf.Max((vertical + 1.5f) / 0.65f, (horizontal + 1.5f) / camera.aspect);
            Physics.SyncTransforms(); return entities;
        }
    }
}
