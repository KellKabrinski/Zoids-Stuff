using ZoidsGameMAUI.Models;

namespace ZoidsGameMAUI.Services
{
    public class GameEngine
    {
        private Random _random = new Random();

        public BattleResult ProcessAttack(Zoid attacker, Zoid target, Ranges range, double distance, double angle)
        {
            var result = new BattleResult
            {
                AttackerName = attacker.Name,
                TargetName = target.Name,
                Range = range,
                Distance = distance,
                Success = false,
                Damage = 0,
                Message = ""
            };

            // Check if attack is possible at this range
            int attackDamage = GetAttackDamage(attacker, range);
            if (attackDamage <= 0)
            {
                result.Message = $"{attacker.Name} has no attack at {range} range!";
                return result;
            }

            // Calculate attack bonus
            int attackBonus = CalculateAttackBonus(attacker, range);
            int attackRoll = RollD20() + attackBonus;

            // Calculate target defense
            int targetDefense = CalculateDefense(target, range, angle);

            result.AttackRoll = attackRoll;
            result.DefenseValue = targetDefense;

            // Check if attack hits
            if (attackRoll >= targetDefense)
            {
                result.Success = true;
                result.Damage = attackDamage;

                // Apply damage
                target.Dents += attackDamage;
                
                // Check for status effects
                CheckStatusEffects(target, attackDamage);

                result.Message = $"{attacker.Name} hits {target.Name} for {attackDamage} damage! " +
                               $"(Roll: {attackRoll} vs Defense: {targetDefense})";
            }
            else
            {
                result.Message = $"{attacker.Name} misses {target.Name}! " +
                               $"(Roll: {attackRoll} vs Defense: {targetDefense})";
            }

            return result;
        }

        public void ProcessMove(Zoid zoid, double newAngle, string newPosition)
        {
            zoid.Angle = newAngle;
            zoid.Position = newPosition;
        }

        public void ProcessShieldToggle(Zoid zoid)
        {
            if (zoid.HasShield())
            {
                zoid.ShieldOn = !zoid.ShieldOn;
            }
        }

        public void ProcessStealthToggle(Zoid zoid)
        {
            if (zoid.HasStealth())
            {
                zoid.StealthOn = !zoid.StealthOn;
            }
        }

        private int GetAttackDamage(Zoid attacker, Ranges range)
        {
            return range switch
            {
                Ranges.Melee => attacker.Melee,
                Ranges.Close => attacker.CloseRange,
                Ranges.Mid => attacker.MidRange,
                Ranges.Long => attacker.LongRange,
                _ => 0
            };
        }

        private int CalculateAttackBonus(Zoid attacker, Ranges range)
        {
            int baseBonus = range switch
            {
                Ranges.Melee => attacker.Fighting,
                Ranges.Close => attacker.Dexterity,
                Ranges.Mid => attacker.Dexterity,
                Ranges.Long => attacker.Dexterity,
                _ => 0
            };

            return baseBonus;
        }

        private int CalculateDefense(Zoid target, Ranges range, double angle)
        {
            int baseDefense = range switch
            {
                Ranges.Melee => target.Parry,
                _ => target.Dodge
            };

            // Apply shield bonus if shield is on and not disabled
            if (target.ShieldOn && target.HasShield())
            {
                baseDefense += target.ShieldRank;
            }

            // Apply stealth bonus if stealth is on
            if (target.StealthOn && target.HasStealth())
            {
                baseDefense += target.StealthRank;
            }

            // Apply angle penalties (rear attacks are easier)
            if (IsRearAngle(angle))
            {
                baseDefense -= 2; // Easier to hit from behind
            }

            return Math.Max(baseDefense, 1); // Minimum defense of 1
        }

        private bool IsRearAngle(double angle)
        {
            // Normalize angle to 0-360
            angle = ((angle % 360) + 360) % 360;
            // Rear angle is roughly 135-225 degrees (90 degree arc behind)
            return angle >= 135 && angle <= 225;
        }

        private void CheckStatusEffects(Zoid target, int damage)
        {
            // Check for dazed/stunned/defeated based on damage
            if (target.Dents >= target.Toughness * 3)
            {
                target.Status = "defeated";
            }
            else if (target.Dents >= target.Toughness * 2)
            {
                target.Status = "stunned";
            }
            else if (target.Dents >= target.Toughness)
            {
                target.Status = "dazed";
            }
        }

        private int RollD20()
        {
            return _random.Next(1, 21);
        }

        public double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        public Ranges DetermineRange(double distance)
        {
            if (distance == 0) return Ranges.Melee;
            if (distance <= 500) return Ranges.Close;
            if (distance <= 1000) return Ranges.Mid;
            return Ranges.Long;
        }
    }

    public class BattleResult
    {
        public string AttackerName { get; set; } = "";
        public string TargetName { get; set; } = "";
        public Ranges Range { get; set; }
        public double Distance { get; set; }
        public bool Success { get; set; }
        public int Damage { get; set; }
        public int AttackRoll { get; set; }
        public int DefenseValue { get; set; }
        public string Message { get; set; } = "";
    }
}
