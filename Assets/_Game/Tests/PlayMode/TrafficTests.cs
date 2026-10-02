using System.Collections;
using System.Collections.Generic;
using Macet;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Macet.Tests
{
    public class TrafficTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        TrafficManager traffic;
        bool complete, failed;
        [SetUp] public void Setup()
        {
            complete = failed = false;
            var go = new GameObject("Test traffic"); objects.Add(go);
            traffic = go.AddComponent<TrafficManager>(); traffic.enabled = false;
            traffic.Configure(go.AddComponent<CollisionManager>());
            traffic.AllExited += () => complete = true;
            traffic.Collided += (a, b) => failed = true;
            traffic.IsPlaying = true;
        }
        Vehicle Car(string id, Vector3 start, Vector3 end, float speed = 2.8f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(go);
            go.GetComponent<BoxCollider>().size = new Vector3(0.85f, 0.8f, 1.4f);
            var car = go.AddComponent<Vehicle>();
            car.Initialize(new VehicleSpawnData { id = id, speed = speed }, new Route(new[] { start, end }));
            return car;
        }
        void UntilStopped(Vehicle car)
        {
            for (int i = 0; i < 1000 && car.State == EntityState.Moving && !failed; i++) traffic.Tick(0.02f);
            Assert.That(car.State, Is.EqualTo(EntityState.Exited));
        }
        [UnityTest] public IEnumerator SafeCrossingSequenceCompletes()
        {
            var a = Car("A", new Vector3(-2.5f, 0, 0), new Vector3(7, 0, 0));
            var b = Car("B", new Vector3(0, 0, 2.5f), new Vector3(0, 0, -7));
            traffic.SetEntities(new[] { a, b });
            Assert.That(traffic.Tap(a), Is.True); UntilStopped(a);
            Assert.That(traffic.Tap(b), Is.True); UntilStopped(b);
            Assert.That(complete, Is.True); Assert.That(failed, Is.False); Assert.That(traffic.Remaining, Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator SimultaneousCrossingFails()
        {
            var a = Car("A", new Vector3(-2.5f, 0, 0), new Vector3(7, 0, 0));
            var b = Car("B", new Vector3(0, 0, 2.5f), new Vector3(0, 0, -7));
            traffic.SetEntities(new[] { a, b }); traffic.Tap(a); traffic.Tap(b);
            for (int i = 0; i < 300 && !failed; i++) traffic.Tick(0.02f);
            Assert.That(failed, Is.True); Assert.That(complete, Is.False);
            Assert.That(a.State, Is.EqualTo(EntityState.Crashed)); Assert.That(b.State, Is.EqualTo(EntityState.Crashed));
            yield return null;
        }
        [UnityTest] public IEnumerator RearCarCannotPassWaitingFrontCar()
        {
            var front = Car("Front", new Vector3(0, 0, -2.5f), new Vector3(0, 0, 7));
            var rear = Car("Rear", new Vector3(0, 0, -4.5f), new Vector3(0, 0, 7));
            traffic.SetEntities(new[] { front, rear }); traffic.Tap(rear);
            for (int i = 0; i < 300 && !failed; i++) traffic.Tick(0.02f);
            Assert.That(failed, Is.True); yield return null;
        }
        [UnityTest] public IEnumerator PausedTrafficDoesNotMoveOrAcceptTap()
        {
            var car = Car("A", Vector3.zero, new Vector3(0, 0, 7)); traffic.SetEntities(new[] { car });
            traffic.Tap(car); var before = car.transform.position; traffic.IsPlaying = false;
            traffic.Tick(1); Assert.That(car.transform.position, Is.EqualTo(before)); Assert.That(traffic.Tap(car), Is.False);
            traffic.IsPlaying = true; UntilStopped(car); yield return null;
        }
        [UnityTest] public IEnumerator SweptCollisionCatchesFastVehicle()
        {
            var front = Car("Front", new Vector3(0, 0, 2), new Vector3(0, 0, 7));
            var rear = Car("Rear", new Vector3(0, 0, -2), new Vector3(0, 0, 7), 500);
            traffic.SetEntities(new[] { front, rear }); traffic.Tap(rear); traffic.Tick(0.02f);
            Assert.That(failed, Is.True); yield return null;
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator AllAuthoredSolutionsComplete()
        {
            for (int number = 1; number <= 3; number++)
            {
                complete = failed = false; traffic.IsPlaying = true;
                var level = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>("Assets/_Game/ScriptableObjects/Levels/Level" + number + ".asset");
                var cars = new List<TrafficEntity>();
                foreach (var spawn in level.vehicles)
                {
                    var route = level.Resolve(spawn);
                    var car = Car(spawn.id, route.Points[0], route.Points[route.Points.Length - 1]);
                    car.Initialize(spawn, route); cars.Add(car);
                }
                traffic.SetEntities(cars);
                foreach (var id in level.intendedSolution)
                {
                    var car = (Vehicle)cars.Find(c => c.Id == id);
                    Assert.That(traffic.Tap(car), Is.True); UntilStopped(car);
                }
                Assert.That(complete, Is.True, "Level " + number);
                Assert.That(failed, Is.False, "Level " + number);
            }
            yield return null;
        }
#endif
        [TearDown] public void Cleanup()
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear(); Physics.SyncTransforms();
        }
    }
}
