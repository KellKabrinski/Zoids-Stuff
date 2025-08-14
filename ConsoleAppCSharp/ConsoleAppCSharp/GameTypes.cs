namespace ZoidsBattle
{
    /// <summary>
    /// Represents the current state of a battle
    /// </summary>
    public class GameState
    {
        public string BattleType { get; set; } = "land";
        public double Distance { get; set; }
        public int TurnNumber { get; set; }
        public bool IsAIMode { get; set; }
        public int CurrentPlayer { get; set; } // 1 for Player 1, 2 for Player 2
    }

    /// <summary>
    /// Represents a player's action for a turn
    /// </summary>
    public class PlayerAction
    {
        public List<ActionType> ActionSequence { get; set; } = new List<ActionType>();
        public MovementType MovementType { get; set; } = MovementType.None;
        public double MoveDistance { get; set; }
        public double AngleChange { get; set; }
        public bool ShouldAttack { get; set; }
        public bool ToggleShield { get; set; }
        public bool ToggleStealth { get; set; }
    }

    /// <summary>
    /// Types of actions a player can take
    /// </summary>
    public enum ActionType
    {
        Move,
        Attack,
        Shield,
        Stealth
    }

    /// <summary>
    /// Types of movement actions
    /// </summary>
    public enum MovementType
    {
        None,
        Close,
        Retreat,
        Circle,
        Search,
        StandStill
    }

    /// <summary>
    /// Result of a battle
    /// </summary>
    public class BattleResult
    {
        public Zoid? Winner { get; set; }
        public CharacterData PlayerData { get; set; } = new CharacterData();
        public Zoid? Player1Zoid { get; set; }
        public Zoid? Player2Zoid { get; set; }
    }
}
