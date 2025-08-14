using System;
using System.Collections.Generic;
using System.Linq;

namespace ZoidsBattle
{
    /// <summary>
    /// Abstract base class for Zoids battle game engine.
    /// Separates game logic from user interface to allow reuse in different UI contexts.
    /// </summary>
    public abstract class GameEngine
    {
        protected static readonly Random random = new Random();

        // Abstract methods that must be implemented by UI-specific subclasses
        public abstract string ChooseBattleType();
        public abstract bool ChooseOpponentType(); // Returns true for AI, false for PvP
        public abstract Zoid ChoosePlayerZoid(IEnumerable<ZoidData> availableZoids, CharacterData playerData);
        public abstract double GetStartingDistance();
        public abstract PlayerAction GetPlayerAction(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState);
        public abstract void DisplayMessage(string message);
        public abstract void DisplayZoidStatus(Zoid zoid, double distance);
        public abstract void DisplayBattleStart(Zoid zoid1, Zoid zoid2);
        public abstract void DisplayTurnStart(Zoid currentZoid, int turnNumber);
        public abstract void DisplayBattleResult(Zoid winner, Zoid loser);
        public abstract bool AskPlayAgain();

        // Core game logic methods (can be overridden if needed)
        public virtual BattleResult RunBattle(List<ZoidData> availableZoids, CharacterData playerData)
        {
            var battleType = ChooseBattleType();
            var filtered = FilterZoids(availableZoids, battleType);
            
            if (!filtered.Any())
            {
                DisplayMessage("No Zoids available for that environment!");
                return new BattleResult { Winner = null, PlayerData = playerData };
            }

            bool aiMode = ChooseOpponentType();
            var (zoid1, zoid2, updatedPlayerData) = ChooseZoidsForBattle(filtered, playerData, aiMode);
            
            DisplayBattleStart(zoid1, zoid2);
            
            var gameState = new GameState
            {
                BattleType = battleType,
                Distance = GetStartingDistance(),
                TurnNumber = 0,
                IsAIMode = aiMode
            };

            var winner = ExecuteBattle(zoid1, zoid2, gameState);
            
            DisplayBattleResult(winner, winner == zoid1 ? zoid2 : zoid1);
            
            // Award credits for winning
            if (winner == zoid1 && aiMode)
            {
                updatedPlayerData.credits += 5000;
            }

            // Reset zoids to base state
            zoid1.ReturnToBaseState();
            zoid2.ReturnToBaseState();

            return new BattleResult 
            { 
                Winner = winner, 
                PlayerData = updatedPlayerData,
                Player1Zoid = zoid1,
                Player2Zoid = zoid2
            };
        }

        protected virtual (Zoid zoid1, Zoid zoid2, CharacterData playerData) ChooseZoidsForBattle(
            IEnumerable<ZoidData> filtered, CharacterData playerData, bool aiMode)
        {
            Zoid zoid1;
            Zoid zoid2;

            if (aiMode)
            {
                zoid1 = ChoosePlayerZoid(filtered, playerData);
                zoid2 = ChooseAIZoid(filtered, zoid1);
            }
            else
            {
                zoid1 = ChoosePlayerZoid(filtered, playerData);
                zoid2 = ChoosePlayerZoid(filtered, playerData); // For PvP, both choose
            }

            return (zoid1, zoid2, playerData);
        }

        protected virtual Zoid ChooseAIZoid(IEnumerable<ZoidData> availableZoids, Zoid playerZoid)
        {
            var personality = random.Next(0, 2) == 0 ? AIPersonality.Aggressive : AIPersonality.Defensive;
            
            // AI chooses a Zoid within one power level of the player
            var aiCandidates = availableZoids
                .Where(z => Math.Abs(z.PowerLevel - playerZoid.PowerLevel) <= 1)
                .ToList();

            if (!aiCandidates.Any())
            {
                aiCandidates = availableZoids.ToList();
            }

            ZoidData aiPick;
            if (personality == AIPersonality.Defensive)
            {
                aiPick = aiCandidates
                    .OrderByDescending(z => z.Powers.Any(p => p.Type == "E-Shield" && p.Rank.HasValue && p.Rank.Value > 0))
                    .ThenBy(_ => random.Next())
                    .First();
            }
            else
            {
                aiPick = aiCandidates
                    .OrderByDescending(z =>
                        z.Powers.Where(p =>
                            p.Type == "Melee" || p.Type == "Close-Range" || p.Type == "Mid-Range" || p.Type == "Long-Range")
                        .Select(p => p.Rank ?? 0).DefaultIfEmpty(0).Max())
                    .ThenBy(_ => random.Next())
                    .First();
            }

            var aiZoid = new Zoid(aiPick);
            DisplayMessage($"AI ({personality}) selects: {aiZoid.ZoidName} (PL {aiZoid.PowerLevel})");
            return aiZoid;
        }

        protected virtual Zoid ExecuteBattle(Zoid zoid1, Zoid zoid2, GameState gameState)
        {
            var zoids = new Dictionary<int, Zoid> { { 1, zoid1 }, { 2, zoid2 } };
            
            // DEBUG: Log the player assignments - add unique identifiers
            DisplayMessage($"DEBUG: Player 1 = {zoid1.ZoidName} (P1), Player 2 = {zoid2.ZoidName} (P2)");
            
            var (first, second) = PickFirst(zoid1, zoid2);
            int[] order = [first, second];

            // Initialize Zoid statuses
            zoid1.Status = "intact";
            zoid2.Status = "intact";

            while (zoid1.Status != "defeated" && zoid2.Status != "defeated")
            {
                int player = order[gameState.TurnNumber % 2];
                var current = zoids[player];
                var enemy = zoids[player == 1 ? 2 : 1];

                // DEBUG: Log turn details with player identifiers
                string currentId = current == zoid1 ? "P1" : "P2";
                string enemyId = enemy == zoid1 ? "P1" : "P2";
                DisplayMessage($"DEBUG: Turn {gameState.TurnNumber + 1}, Player {player}, Current = {current.ZoidName} ({currentId}), Enemy = {enemy.ZoidName} ({enemyId})");

                DisplayTurnStart(current, gameState.TurnNumber + 1);

                if (gameState.IsAIMode && player == 2)
                {
                    ExecuteAITurn(current, enemy, gameState);
                }
                else
                {
                    ExecutePlayerTurn(current, enemy, gameState);
                }

                // Status cleanup
                if (current.Status == "stunned") current.Status = "dazed";
                else if (current.Status == "dazed") current.Status = "intact";

                gameState.TurnNumber++;
            }

            return zoid1.Status == "defeated" ? zoid2 : zoid1;
        }

        protected virtual void ExecutePlayerTurn(Zoid current, Zoid enemy, GameState gameState)
        {
            bool enemyDetected = !enemy.StealthOn || SearchCheck(current, enemy);
            
            if (!enemyDetected)
            {
                DisplayMessage($"{enemy.ZoidName} is in stealth mode!");
                DisplayMessage($"{current.ZoidName} cannot locate {enemy.ZoidName}!");
            }

            DisplayZoidStatus(current, gameState.Distance);

            if (current.Status == "stunned")
            {
                DisplayMessage("You are STUNNED! You cannot move or attack this turn.");
                var action = GetPlayerAction(current, enemy, gameState.Distance, enemyDetected, gameState);
                HandleShieldAndStealth(current, action);
                return;
            }

            var playerAction = GetPlayerAction(current, enemy, gameState.Distance, enemyDetected, gameState);
            ExecuteAction(current, enemy, playerAction, gameState, ref enemyDetected);
        }

        protected virtual void ExecuteAITurn(Zoid current, Zoid enemy, GameState gameState)
        {
            var personality = random.Next(0, 2) == 0 ? AIPersonality.Aggressive : AIPersonality.Defensive;
            bool enemyDetected = !enemy.StealthOn || SearchCheck(current, enemy);

            var aiAction = GenerateAIAction(current, enemy, gameState.Distance, enemyDetected, personality);
            ExecuteAction(current, enemy, aiAction, gameState, ref enemyDetected);
        }

        protected virtual void ExecuteAction(Zoid current, Zoid enemy, PlayerAction action, GameState gameState, ref bool enemyDetected)
        {
            // Execute actions in the order specified by the player
            foreach (var actionType in action.ActionSequence)
            {
                switch (actionType)
                {
                    case ActionType.Move:
                        ExecuteMovementAction(current, enemy, action, gameState, ref enemyDetected);
                        break;
                    case ActionType.Attack:
                        ExecuteAttackAction(current, enemy, gameState, enemyDetected);
                        break;
                    case ActionType.Shield:
                        ExecuteShieldAction(current, action);
                        break;
                    case ActionType.Stealth:
                        ExecuteStealthAction(current, action);
                        break;
                }
                
                // Small delay between actions for visual clarity
                System.Threading.Thread.Sleep(500);
            }
        }

        private void ExecuteMovementAction(Zoid current, Zoid enemy, PlayerAction action, GameState gameState, ref bool enemyDetected)
        {
            double oldDistance = gameState.Distance;

            if (action.MovementType != MovementType.None && action.MovementType != MovementType.StandStill)
            {
                var newDistance = ExecuteMovement(current, enemy, action, gameState, ref enemyDetected);
                gameState.Distance = newDistance;
                DisplayMessage($"Distance updated: {oldDistance:F1}m -> {newDistance:F1}m");
            }
            else
            {
                DisplayMessage($"{current.ZoidName} stands still at {gameState.Distance:F1}m");
            }
        }

        private void ExecuteAttackAction(Zoid current, Zoid enemy, GameState gameState, bool enemyDetected)
        {
            bool canAttackNow = CanAttack(current, enemy, gameState, false);
            DisplayMessage($"Attack check: Distance={gameState.Distance:F1}m, CanAttack={canAttackNow}");
            
            if (canAttackNow)
            {
                var range = GetRange(gameState.Distance);
                DisplayMessage($"Executing {range} range attack at {gameState.Distance:F1}m");
                DisplayMessage($"DEBUG ATTACK: {current.ZoidName} attacking {enemy.ZoidName}");
                HandleAttack(current, enemy, range, enemyDetected);
            }
            else
            {
                DisplayMessage($"{current.ZoidName} cannot attack at current distance ({gameState.Distance:F1}m) or is blocked by status/shield");
            }
        }

        private void ExecuteShieldAction(Zoid current, PlayerAction action)
        {
            if (current.ShieldRank > 0)
            {
                current.ShieldOn = !current.ShieldOn;
                DisplayMessage($"{current.ZoidName} {(current.ShieldOn ? "activated" : "deactivated")} shield");
            }
            else
            {
                DisplayMessage($"{current.ZoidName} has no shield capability");
            }
        }

        private void ExecuteStealthAction(Zoid current, PlayerAction action)
        {
            if (current.StealthRank > 0)
            {
                current.StealthOn = !current.StealthOn;
                DisplayMessage($"{current.ZoidName} {(current.StealthOn ? "activated" : "deactivated")} stealth");
            }
            else
            {
                DisplayMessage($"{current.ZoidName} has no stealth capability");
            }
        }

        // Core game mechanics (protected methods)
        protected virtual double ExecuteMovement(Zoid current, Zoid enemy, PlayerAction action, GameState gameState, ref bool enemyDetected)
        {
            int speed = GetSpeed(current, gameState.BattleType);
            double newDistance = gameState.Distance;

            switch (action.MovementType)
            {
                case MovementType.Close:
                    newDistance = Math.Max(0, gameState.Distance - action.MoveDistance);
                    current.Position = "close";
                    DisplayMessage($"{current.ZoidName} moves closer by {action.MoveDistance} meters.");
                    break;
                case MovementType.Retreat:
                    newDistance = gameState.Distance + action.MoveDistance;
                    current.Position = "retreat";
                    DisplayMessage($"{current.ZoidName} retreats by {action.MoveDistance} meters.");
                    break;
                case MovementType.Circle:
                    current.Angle = (current.Angle + action.AngleChange) % 360;
                    current.Position = "circle";
                    DisplayMessage($"{current.ZoidName} circles by {action.AngleChange} degrees.");
                    break;
                case MovementType.Search:
                    bool closer = random.Next(0, 2) == 0;
                    if (closer)
                        newDistance = Math.Max(0, gameState.Distance - speed * 0.5);
                    else
                        newDistance = gameState.Distance + speed * 0.5;
                    enemyDetected = SearchCheck(current, enemy);
                    DisplayMessage($"{current.ZoidName} searches for the enemy.");
                    break;
                case MovementType.StandStill:
                    current.Position = "stand still";
                    if (!enemyDetected) enemyDetected = SearchCheck(current, enemy);
                    break;
            }

            return newDistance;
        }

        protected virtual void HandleShieldAndStealth(Zoid zoid, PlayerAction action)
        {
            if (action.ToggleShield && zoid.HasShield())
            {
                zoid.ShieldOn = !zoid.ShieldOn;
                DisplayMessage($"{zoid.ZoidName} shield is now {(zoid.ShieldOn ? "ON" : "OFF")}");
            }

            if (action.ToggleStealth && zoid.HasStealth())
            {
                zoid.StealthOn = !zoid.StealthOn;
                DisplayMessage($"{zoid.ZoidName} stealth is now {(zoid.StealthOn ? "ON" : "OFF")}");
            }
        }

        protected virtual void HandleAttack(Zoid attacker, Zoid defender, Ranges range, bool enemyDetected)
        {
            if (defender.StealthOn && !enemyDetected)
            {
                DisplayMessage("Target is concealed! 50% miss chance.");
                if (random.Next(0, 2) == 0)
                {
                    DisplayMessage("Your attack misses the target's last known location!");
                    return;
                }
                else
                {
                    DisplayMessage("You get lucky and land a hit despite concealment!");
                }
            }

            DisplayMessage($"{attacker.ZoidName} attacks {defender.ZoidName} with a {range} attack!");
            
            int damage = range switch
            {
                Ranges.Melee => attacker.Melee,
                Ranges.Close => attacker.CloseRange,
                Ranges.Mid => attacker.MidRange,
                _ => attacker.LongRange
            };

            int attackRoll = RollD20() + (range == Ranges.Melee ? 
                attacker.Fighting + attacker.CloseCombat : 
                attacker.Dexterity + attacker.RangedCombat);
            
            int defenseRoll = 10 + (range == Ranges.Melee ? defender.Parry : defender.Dodge);

            bool hit = attackRoll >= defenseRoll;
            if (hit)
            {
                DisplayMessage($"Attack roll: {attackRoll} vs Defense roll: {defenseRoll}");
                DisplayMessage($"{attacker.ZoidName} hits {defender.ZoidName} for {damage} damage!");
                ProcessDamage(attacker, defender, damage);
            }
            else
            {
                DisplayMessage($"{attacker.ZoidName} misses the attack on {defender.ZoidName}!");
            }
        }

        protected virtual void ProcessDamage(Zoid attacker, Zoid defender, int damage)
        {
            if (defender.HasShield() && defender.ShieldOn && IsAttackInShieldArc(attacker, defender))
            {
                int shieldRoll = RollD20() + defender.ShieldRank;
                if (shieldRoll >= damage + 15)
                {
                    defender.ShieldDisabled = true;
                    defender.ShieldOn = false;
                    DisplayMessage($"{defender.ZoidName}'s shield is disabled!");
                }
                return;
            }

            int toughRoll = RollD20() + defender.Toughness - defender.Dents;
            DisplayMessage($"Enemy toughness roll: {toughRoll} (Toughness: {defender.Toughness}, Dents: {defender.Dents})");
            
            int diff = damage + 15 - toughRoll;
            if (diff <= 0)
            {
                DisplayMessage($"{defender.ZoidName} successfully defends against the attack!");
            }
            else if (diff < 5)
            {
                DisplayMessage($"{defender.ZoidName} takes a minor hit!");
                defender.Dents++;
                DisplayMessage($"{defender.ZoidName} receives a DENT! (Total dents: {defender.Dents})");
            }
            else if (diff < 10)
            {
                DisplayMessage($"{defender.ZoidName} takes a moderate hit!");
                defender.Dents++;
                DisplayMessage($"{defender.ZoidName} receives a DENT! (Total dents: {defender.Dents})");
                defender.Status = "dazed";
                DisplayMessage($"{defender.ZoidName} is now DAZED!");
            }
            else if (diff < 15)
            {
                DisplayMessage($"{defender.ZoidName} takes a heavy hit!");
                defender.Dents++;
                DisplayMessage($"{defender.ZoidName} receives a DENT! (Total dents: {defender.Dents})");
                defender.Status = "stunned";
                DisplayMessage($"{defender.ZoidName} is now STUNNED!");
            }
            else
            {
                DisplayMessage($"{defender.ZoidName} takes a critical hit!");
                defender.Dents++;
                DisplayMessage($"{defender.ZoidName} receives a DENT! (Total dents: {defender.Dents})");
                defender.Status = "defeated";
                DisplayMessage($"{defender.ZoidName} is now DEFEATED!");
            }
        }

        // Utility methods
        protected virtual PlayerAction GenerateAIAction(Zoid current, Zoid enemy, double distance, bool enemyDetected, AIPersonality personality)
        {
            var action = new PlayerAction();

            // AI always tries to use stealth
            if (current.HasStealth() && !current.StealthOn)
            {
                action.ToggleStealth = true;
            }

            // Handle shield based on personality
            if (current.HasShield())
            {
                if (personality == AIPersonality.Defensive)
                {
                    var range = GetRange(distance);
                    int myDamage = GetDamageForRange(current, range);
                    int enemyDamage = GetDamageForRange(enemy, range);
                    
                    if (myDamage <= enemyDamage && !current.ShieldOn)
                        action.ToggleShield = true;
                    else if (myDamage > enemyDamage && current.ShieldOn)
                        action.ToggleShield = true;
                }
                else // Aggressive
                {
                    if (!current.CanAttack(distance) && !current.ShieldOn)
                        action.ToggleShield = true;
                    else if (current.CanAttack(distance) && current.ShieldOn)
                        action.ToggleShield = true;
                }
            }

            // Handle movement
            if (!enemyDetected)
            {
                action.MovementType = MovementType.Search;
            }
            else
            {
                GenerateAIMovement(current, enemy, distance, personality, action);
            }

            // Handle attack
            action.ShouldAttack = current.CanAttack(distance) && !current.ShieldOn;

            return action;
        }

        protected virtual void GenerateAIMovement(Zoid current, Zoid enemy, double distance, AIPersonality personality, PlayerAction action)
        {
            if (personality == AIPersonality.Aggressive)
            {
                double targetDistance = current.BestRange switch
                {
                    Ranges.Melee => 0,
                    Ranges.Close => 500,
                    Ranges.Mid => 1000,
                    Ranges.Long => 1500,
                    _ => distance
                };

                if (Math.Abs(distance - targetDistance) > 50) // Only move if meaningful difference
                {
                    if (distance > targetDistance)
                    {
                        action.MovementType = MovementType.Close;
                        action.MoveDistance = Math.Min(GetSpeed(current, "land"), distance - targetDistance);
                    }
                    else
                    {
                        action.MovementType = MovementType.Retreat;
                        action.MoveDistance = Math.Min(GetSpeed(current, "land"), targetDistance - distance);
                    }
                }
                else
                {
                    action.MovementType = MovementType.StandStill;
                }
            }
            else // Defensive
            {
                double targetDistance = enemy.WorstRange switch
                {
                    Ranges.Melee => 0,
                    Ranges.Close => 500,
                    Ranges.Mid => 1000,
                    Ranges.Long => 1500,
                    _ => distance
                };

                if (!current.CanAttack(targetDistance))
                {
                    // Find the closest range that zoid can attack from
                    var possibleRanges = new[] { Ranges.Melee, Ranges.Close, Ranges.Mid, Ranges.Long }
                        .Where(r => current.CanAttack(GetDistanceForRange(r)))
                        .OrderBy(r => Math.Abs(GetDistanceForRange(r) - targetDistance))
                        .FirstOrDefault();

                    targetDistance = GetDistanceForRange(possibleRanges);
                }

                if (Math.Abs(distance - targetDistance) > 50)
                {
                    if (distance > targetDistance)
                    {
                        action.MovementType = MovementType.Close;
                        action.MoveDistance = Math.Min(GetSpeed(current, "land"), distance - targetDistance);
                    }
                    else
                    {
                        action.MovementType = MovementType.Retreat;
                        action.MoveDistance = Math.Min(GetSpeed(current, "land"), targetDistance - distance);
                    }
                }
                else
                {
                    action.MovementType = MovementType.StandStill;
                }
            }
        }

        // Helper methods
        protected static IEnumerable<ZoidData> FilterZoids(IEnumerable<ZoidData> zoids, string battleType)
        {
            return zoids.Where(z => battleType switch
            {
                "land" => z.Movement.Land > 0,
                "water" => z.Movement.Water > 0,
                "air" => z.Movement.Air > 0,
                _ => false
            });
        }

        protected static (int, int) PickFirst(Zoid z1, Zoid z2)
        {
            int first = random.Next(1, 3);
            return first == 1 ? (1, 2) : (2, 1);
        }

        protected static Ranges GetRange(double distance)
        {
            if (distance == 0) return Ranges.Melee;
            if (distance <= 500) return Ranges.Close;
            if (distance <= 1000) return Ranges.Mid;
            return Ranges.Long;
        }

        protected static double GetDistanceForRange(Ranges range)
        {
            return range switch
            {
                Ranges.Melee => 0,
                Ranges.Close => 500,
                Ranges.Mid => 1000,
                Ranges.Long => 1500,
                _ => 0
            };
        }

        protected static int GetDamageForRange(Zoid zoid, Ranges range)
        {
            return range switch
            {
                Ranges.Melee => zoid.Melee,
                Ranges.Close => zoid.CloseRange,
                Ranges.Mid => zoid.MidRange,
                _ => zoid.LongRange
            };
        }

        protected static bool IsAttackInShieldArc(Zoid attacker, Zoid defender)
        {
            double rel = (attacker.Angle - defender.Angle) % 360;
            if (rel > 180) rel = 360 - rel;
            return Math.Abs(rel) <= 45;
        }

        protected static int RollD20() => random.Next(1, 21);

        protected static bool SearchCheck(Zoid searcher, Zoid target)
        {
            int roll = RollD20();
            int total = roll + searcher.Awareness;
            int dc = (target.HasStealth() && target.StealthOn && target.StealthRank > 0) ? 5 + target.StealthRank : 0;
            return total >= dc;
        }

        protected static int GetSpeed(Zoid zoid, string battleType)
        {
            return battleType.ToLower() switch
            {
                "land" => zoid.Land,
                "water" => zoid.Water,
                "air" => zoid.Air,
                _ => 0
            };
        }

        protected static bool CanAttack(Zoid current, Zoid enemy, GameState gameState, bool didMove)
        {
            if (current.ShieldOn && current.HasShield()) return false;
            if (current.Status == "stunned") return false;
            if (current.Status == "dazed" && didMove) return false;
            return current.CanAttack(gameState.Distance);
        }
    }
}
