using System;
using UnityEngine;
namespace Macet
{
    [Serializable] public class RoadNode
    {
        public string id;
        public Vector3 position;
        public bool isExit;
    }
    public sealed class Route
    {
        public Vector3[] Points { get; }
        public Route(Vector3[] points)
        {
            if (points == null || points.Length < 2) throw new ArgumentException("Route needs at least two points.");
            Points = (Vector3[])points.Clone();
        }
    }
}
