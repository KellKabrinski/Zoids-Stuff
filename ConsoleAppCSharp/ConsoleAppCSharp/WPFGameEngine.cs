using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZoidsBattle
{
    /// <summary>
    /// WPF-specific implementation of the game engine
    /// Uses callback functions to communicate with the WPF UI
    /// </summary>
    public class WPFGameEngine : GameEngine
    {
        private readonly Func<Task<string>> _chooseBattleTypeCallback;
        private readonly Func<Task<bool>> _chooseOpponentTypeCallback;
        private readonly Func<IEnumerable<ZoidData>, bool, Task<Zoid>> _chooseZoidCallback;
        private readonly Func<Task<double>> _getStartingDistanceCallback;
        private readonly Func<Zoid, Zoid, double, bool, GameState, Task<PlayerAction>> _getPlayerActionCallback;
        private readonly Action<string> _displayMessageCallback;
        private readonly Action<Zoid, double> _displayZoidStatusCallback;
        private readonly Action<Zoid, Zoid> _displayBattleStartCallback;
        private readonly Action<Zoid, int> _displayTurnStartCallback;
        private readonly Action<Zoid, Zoid> _displayBattleResultCallback;
        private readonly Func<Task<bool>> _askPlayAgainCallback;

        public WPFGameEngine(
            Func<Task<string>> chooseBattleType,
            Func<Task<bool>> chooseOpponentType,
            Func<IEnumerable<ZoidData>, bool, Task<Zoid>> chooseZoid,
            Func<Task<double>> getStartingDistance,
            Func<Zoid, Zoid, double, bool, GameState, Task<PlayerAction>> getPlayerAction,
            Action<string> displayMessage,
            Action<Zoid, double> displayZoidStatus,
            Action<Zoid, Zoid> displayBattleStart,
            Action<Zoid, int> displayTurnStart,
            Action<Zoid, Zoid> displayBattleResult,
            Func<Task<bool>> askPlayAgain)
        {
            _chooseBattleTypeCallback = chooseBattleType;
            _chooseOpponentTypeCallback = chooseOpponentType;
            _chooseZoidCallback = chooseZoid;
            _getStartingDistanceCallback = getStartingDistance;
            _getPlayerActionCallback = getPlayerAction;
            _displayMessageCallback = displayMessage;
            _displayZoidStatusCallback = displayZoidStatus;
            _displayBattleStartCallback = displayBattleStart;
            _displayTurnStartCallback = displayTurnStart;
            _displayBattleResultCallback = displayBattleResult;
            _askPlayAgainCallback = askPlayAgain;
        }

        public override string ChooseBattleType()
        {
            return _chooseBattleTypeCallback().Result;
        }

        public override bool ChooseOpponentType()
        {
            return _chooseOpponentTypeCallback().Result;
        }

        public override Zoid ChoosePlayerZoid(IEnumerable<ZoidData> availableZoids, CharacterData playerData)
        {
            // For PvP mode, we don't use save files - just let players pick from available zoids
            bool isAIMode = false; // This will be set appropriately by the calling context
            return _chooseZoidCallback(availableZoids, isAIMode).Result;
        }

        public override double GetStartingDistance()
        {
            return _getStartingDistanceCallback().Result;
        }

        public override PlayerAction GetPlayerAction(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState)
        {
            return _getPlayerActionCallback(currentZoid, enemyZoid, distance, enemyDetected, gameState).Result;
        }

        public override void DisplayMessage(string message)
        {
            _displayMessageCallback(message);
        }

        public override void DisplayZoidStatus(Zoid zoid, double distance)
        {
            _displayZoidStatusCallback(zoid, distance);
        }

        public override void DisplayBattleStart(Zoid zoid1, Zoid zoid2)
        {
            _displayBattleStartCallback(zoid1, zoid2);
        }

        public override void DisplayTurnStart(Zoid currentZoid, int turnNumber)
        {
            _displayTurnStartCallback(currentZoid, turnNumber);
        }

        public override void DisplayBattleResult(Zoid winner, Zoid loser)
        {
            _displayBattleResultCallback(winner, loser);
        }

        public override bool AskPlayAgain()
        {
            return _askPlayAgainCallback().Result;
        }

        // Override the zoid selection logic for WPF to handle PvP vs AI differently
        protected override (Zoid zoid1, Zoid zoid2, CharacterData playerData) ChooseZoidsForBattle(
            IEnumerable<ZoidData> filtered, CharacterData playerData, bool aiMode)
        {
            Zoid zoid1;
            Zoid zoid2;

            if (aiMode)
            {
                // AI mode: Player 1 chooses from save file, AI chooses based on logic
                zoid1 = _chooseZoidCallback(filtered, true).Result; // true = AI mode for save file handling
                zoid2 = ChooseAIZoid(filtered, zoid1);
            }
            else
            {
                // PvP mode: Both players choose from full list, no save files
                _displayMessageCallback("Player 1: Choose your Zoid");
                zoid1 = _chooseZoidCallback(filtered, false).Result; // false = PvP mode, no save files
                
                _displayMessageCallback("Player 2: Choose your Zoid");
                zoid2 = _chooseZoidCallback(filtered, false).Result; // false = PvP mode, no save files
            }

            return (zoid1, zoid2, playerData);
        }
    }
}
