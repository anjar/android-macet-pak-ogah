using UnityEngine;
namespace Macet
{
    public sealed class GameManager : MonoBehaviour
    {
        public LevelData[] levels;
        public LevelLoader loader;
        public TrafficManager traffic;
        public CollisionManager collisions;
        public UIManager ui;
        public Camera view;
        public GameState State { get; private set; } = GameState.Loading;
        public int LevelIndex { get; private set; }
        void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            traffic.Configure(collisions);
            traffic.AllExited += Complete;
            traffic.Collided += Fail;
            ui.Initialize(this); Menu();
        }
        public void StartLevel(int index)
        {
            if (index < 0 || index >= levels.Length) return;
            SetState(GameState.Loading); LevelIndex = index;
            traffic.SetEntities(loader.Load(levels[index], view));
            SetState(GameState.Playing); ui.ShowGame(levels[index]);
        }
        public void Restart() { StartLevel(LevelIndex); }
        public void Next() { if (LevelIndex + 1 < levels.Length) StartLevel(LevelIndex + 1); else Menu(); }
        public void Pause() { if (State != GameState.Playing) return; SetState(GameState.Paused); ui.ShowPause(); }
        public void Resume() { if (State != GameState.Paused) return; SetState(GameState.Playing); ui.ShowGame(levels[LevelIndex]); }
        public void Menu() { SetState(GameState.Loading); ui.ShowMenu(levels); }
        void Complete() { SetState(GameState.Completed); ui.ShowResult(true, LevelIndex + 1 < levels.Length); }
        void Fail(TrafficEntity a, TrafficEntity b) { SetState(GameState.Failed); ui.ShowResult(false, false); }
        void SetState(GameState state) { State = state; traffic.IsPlaying = state == GameState.Playing; }
        void OnDestroy() { traffic.AllExited -= Complete; traffic.Collided -= Fail; }
    }
}
