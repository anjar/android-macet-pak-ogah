using System;
using System.Collections.Generic;
using UnityEngine;
namespace Macet
{
    [Serializable] public class VehicleSpawnData
    {
        public string id;
        public EntityType entityType;
        public float speed = 2.8f;
        public Color color = Color.yellow;
        public string[] route;
    }
    [Serializable] public class RoadSegment { public Vector3 from; public Vector3 to; public float width = 2.4f; }
    [CreateAssetMenu(menuName = "Macet/Level")]
    public sealed class LevelData : ScriptableObject
    {
        public int levelNumber;
        public LevelDifficulty difficulty;
        public string instruction;
        public int expectedMoves;
        public float expectedDuration = 30;
        public string[] mechanics;
        public string[] intendedSolution;
        public RoadNode[] nodes;
        public RoadSegment[] roads;
        public VehicleSpawnData[] vehicles;
        public Route Resolve(VehicleSpawnData spawn)
        {
            var points = new Vector3[spawn.route.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var node = Array.Find(nodes, n => n.id == spawn.route[i]);
                if (node == null) throw new InvalidOperationException("Missing route node: " + spawn.route[i]);
                points[i] = node.position;
            }
            return new Route(points);
        }
        public List<string> ValidateData()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            if (nodes == null || vehicles == null || roads == null) { errors.Add("Missing map or spawns."); return errors; }
            foreach (var n in nodes) if (string.IsNullOrEmpty(n.id) || !ids.Add(n.id)) errors.Add("Node IDs must be unique.");
            ids.Clear();
            foreach (var v in vehicles)
            {
                if (string.IsNullOrEmpty(v.id) || !ids.Add(v.id)) errors.Add("Entity IDs must be unique.");
                if (v.speed <= 0 || v.route == null || v.route.Length < 2) { errors.Add("Invalid speed or route."); continue; }
                foreach (var id in v.route) if (Array.Find(nodes, n => n.id == id) == null) errors.Add("Missing node " + id);
                var exit = Array.Find(nodes, n => n.id == v.route[v.route.Length - 1]);
                if (exit != null && !exit.isExit) errors.Add("Route must end at exit.");
            }
            if (expectedMoves != vehicles.Length) errors.Add("Expected moves must match entity count.");
            var solution = new HashSet<string>(intendedSolution ?? Array.Empty<string>());
            if (solution.Count != vehicles.Length || !solution.SetEquals(ids)) errors.Add("Intended solution must contain every entity once.");
            return errors;
        }
    }
}
