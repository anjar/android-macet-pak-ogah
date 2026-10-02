namespace Macet
{
    public enum GameState { Loading, Playing, Paused, Failed, Completed }
    public enum EntityState { Waiting, Moving, Blocked, Exited, Crashed }
    public enum EntityType { Car, Motorcycle, Angkot, Pedestrian }
    public enum LevelDifficulty { Normal, Hard, SuperHard, UltraHard }
}
