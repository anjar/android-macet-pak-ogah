using Macet;
using NUnit.Framework;
using UnityEditor;
namespace Macet.Tests
{
    public class LevelDataTests
    {
        [TestCase(1, 2)] [TestCase(2, 3)] [TestCase(3, 4)]
        public void PhaseOneLevelsHaveValidRoutesAndSolutions(int number, int count)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/_Game/ScriptableObjects/Levels/Level" + number + ".asset");
            Assert.That(level, Is.Not.Null);
            Assert.That(level.ValidateData(), Is.Empty);
            Assert.That(level.difficulty, Is.EqualTo(LevelDifficulty.Normal));
            Assert.That(level.vehicles.Length, Is.EqualTo(count));
            foreach (var spawn in level.vehicles) Assert.That(level.Resolve(spawn).Points.Length, Is.GreaterThanOrEqualTo(2));
        }
        [Test] public void MissingNodeIsRejected()
        {
            var original = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/_Game/ScriptableObjects/Levels/Level1.asset");
            var level = UnityEngine.Object.Instantiate(original);
            try { level.vehicles[0].route[0] = "missing"; Assert.That(level.ValidateData(), Is.Not.Empty); }
            finally { UnityEngine.Object.DestroyImmediate(level); }
        }
    }
}
