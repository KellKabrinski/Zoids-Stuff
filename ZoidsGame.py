import json
import random

class Zoid:
    def __init__(self, zoid_data):
        self.name = zoid_data["Name"]
        # Stats
        self.fighting = zoid_data["Stats"].get("Fighting", 0)
        self.strength = zoid_data["Stats"].get("Strength", 0)
        self.dexterity = zoid_data["Stats"].get("Dexterity", 0)
        self.agility = zoid_data["Stats"].get("Agility", 0)
        self.awareness = zoid_data["Stats"].get("Awareness", 0)
        # Defenses
        self.toughness = zoid_data["Defenses"].get("Toughness", 0)
        self.parry = zoid_data["Defenses"].get("Parry", 0)
        self.dodge = zoid_data["Defenses"].get("Dodge", 0)
        # Movement
        self.land = zoid_data["Movement"].get("Land", 0)
        self.water = zoid_data["Movement"].get("Water", 0)
        self.air = zoid_data["Movement"].get("Air", 0)
        # Powers (only relevant ranks kept)
        self.powers = zoid_data.get("Powers", [])

        self.melee = next((p.get('Damage') for p in self.powers if p['Type'] == 'Melee'), None)
        self.close_range = next((p.get('Damage') for p in self.powers if p['Type'] == 'Close-Range'), None)
        self.mid_range = next((p.get('Damage') for p in self.powers if p['Type'] == 'Mid-Range'), None)
        self.long_range = next((p.get('Damage') for p in self.powers if p['Type'] == 'Long-Range'), None)
        self.shield = next((p.get('Rank') for p in self.powers if p['Type'] == 'Create'), None)
        self.shieldDisabled=False
        self.stealth = next((p.get('Rank') for p in self.powers if p['Type'] == 'Concealment' and 'Visual' in p.get('Senses', [])), None)
        self.armor = next((p.get('Protection') for p in self.powers if p['Type'] == 'Armor'), None)

        # Battle state
        self.position = "neutral"
        self.shield_on = False
        self.stealth_on = False
        self.dents = 0
        self.status = "intact"  # "intact", "dazed", "stunned", "defeated"

    def has_shield(self):
        return self.shield is not None and self.shieldDisabled is False

    def has_stealth(self):
        return self.stealth is not None

    def get_speed(self, battle_type):
        if battle_type == "land":
            return self.land
        elif battle_type == "water":
            return self.water
        elif battle_type == "air":
            return self.air
        return 0
    
    def can_attack(self,distance):
        if self.melee and distance == 0:
            return True
        if self.close_range and distance <= 500:
            return True
        if self.mid_range and distance <= 1000:
            return True
        if self.long_range and distance > 1000:
            return True
        return False

    def print_status(self):
        print(f"\n{self.name}'s status: "
              f"Position={self.position}, "
              f"Shield={'ON' if self.shield_on else 'OFF'} (Rank={self.shield if self.shield is not None else '-'})"
              f", Stealth={'ON' if self.stealth_on else 'OFF'} (Rank={self.stealth if self.stealth is not None else '-'})"
              f", Dents={self.dents}, Status={self.status.capitalize()}")
        print("Stats: "
              f"Fighting={self.fighting}, Strength={self.strength}, Dexterity={self.dexterity}, "
              f"Agility={self.agility}, Awareness={self.awareness} | "
              f"Toughness={self.toughness}, Parry={self.parry}, Dodge={self.dodge} | "
              f"Land={self.land}, Water={self.water}, Air={self.air}")
        print("Attacks: "
              f"Melee={self.melee if self.melee is not None else '-'}, "
              f"Close={self.close_range if self.close_range is not None else '-'}, "
              f"Mid={self.mid_range if self.mid_range is not None else '-'}, "
              f"Long={self.long_range if self.long_range is not None else '-'}")
        print(f"Armor: {self.armor if self.armor is not None else '-'}")

def load_zoids(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)

def pick_battle_type():
    print("\nChoose battle type:")
    print("1: Land")
    print("2: Water")
    print("3: Air")
    while True:
        choice = input("Battle type: ")
        if choice == "1":
            return "land"
        elif choice == "2":
            return "water"
        elif choice == "3":
            return "air"
        print("Invalid input. Try again.")

def filter_zoids(zoids, battle_type):
    filtered = []
    for z in zoids:
        m = z.get("Movement", {})
        if battle_type == "land" and m.get("Land", 0) > 0:
            filtered.append(z)
        elif battle_type == "water" and m.get("Water", 0) > 0:
            filtered.append(z)
        elif battle_type == "air" and m.get("Air", 0) > 0:
            filtered.append(z)
    return filtered

def display_zoids(zoids):
    zoids_by_pl = sorted(zoids, key=lambda z: (z.get('Power Level', 0), z['Name']))
    print("\nAvailable Zoids:")
    for idx, z in enumerate(zoids_by_pl):
        print(f"{idx + 1}: {z['Name']} (PL {z.get('Power Level', 0)})")
    return zoids_by_pl

def choose_zoid(zoids, player_num):
    zoids_by_pl = display_zoids(zoids)
    while True:
        try:
            choice = int(input(f"\nEnter number for Player {player_num}: ")) - 1
            if 0 <= choice < len(zoids_by_pl):
                return Zoid(zoids_by_pl[choice])
        except ValueError:
            pass
        print("Invalid input. Try again.")

def pick_first(z1, z2):
    first = random.choice([1, 2])
    print(f"\n{z1.name if first==1 else z2.name} goes first!\n")
    return (1, 2) if first == 1 else (2, 1)

def get_starting_distance():
    while True:
        try:
            dist = float(input("\nEnter starting distance between Zoids in meters: "))
            if dist >= 0:
                return dist
        except ValueError:
            pass
        print("Invalid input. Try again.")
def get_range(distance):
    if distance == 0:
        return "melee"
    elif distance <= 500:
        return "close"
    elif distance <= 1000:
        return "mid"
    else:
        return "long"

def game_loop(z1, z2, battle_type):
    zoid_objs = {1: z1, 2: z2}
    order = pick_first(z1, z2)
    turn = 0
    distance = get_starting_distance()
    while z1.status != "defeated" and z2.status != "defeated":
        player = order[turn % 2]
        zoid = zoid_objs[player]
        enemy = zoid_objs[1 if player == 2 else 2]
        zoid.print_status()
        print(f"\nCurrent distance between Zoids: {distance:.1f} meters")
        print(f"{zoid.name}'s turn!")

        prior_status = zoid.status

        # STUNNED: Cannot move or attack
        if zoid.status == "stunned":
            print("You are STUNNED! You cannot move or attack this turn.")
            if zoid.has_shield():
                s_toggle = input("  Toggle shield? (y/n): ")
                if s_toggle.lower().startswith('y'):
                    zoid.shield_on = not zoid.shield_on
            if zoid.has_stealth():
                st_toggle = input("  Toggle stealth? (y/n): ")
                if st_toggle.lower().startswith('y'):
                    zoid.stealth_on = not zoid.stealth_on
            zoid.status = "dazed"
            turn += 1
            continue

        did_move = False

        # DAZED: Can move or attack, not both
        if zoid.status == "dazed":
            print("You are DAZED! You may move OR attack, not both.")
            move_attack = input("  Move (m) or Attack (a) or Skip (s)? ")
            if move_attack.lower().startswith('m'):
                print("Choose maneuver:")
                move = input("  1: Close\n  2: Retreat\n  3: Circle\n  4: Stand Still\n  Choice: ")
                speed = zoid.get_speed(battle_type)
                if move == "1":
                    zoid.position = 'close'
                    distance = max(0, distance - speed)
                elif move == "2":
                    zoid.position = 'retreat'
                    distance += speed
                elif move == "3":
                    zoid.position = 'circle'
                elif move == "4":
                    zoid.position = 'stand still'
                did_move = False
        else:
            # MOVEMENT PHASE
            print("Choose maneuver:")
            move = input("  1: Close\n  2: Retreat\n  3: Circle\n  4: Stand Still\n  Choice: ")
            speed = zoid.get_speed(battle_type)
            if move == "1":
                zoid.position = 'close'
                distance = max(0, distance - speed)
                did_move = True
            elif move == "2":
                zoid.position = 'retreat'
                distance += speed
                did_move = True
            elif move == "3":
                zoid.position = 'circle'
                did_move = True
            elif move == "4":
                zoid.position = 'stand still'
                did_move = False

        # SHIELD & STEALTH PHASE (always available)
        if zoid.has_shield():
            print(f"  Shield is currently {'ON' if zoid.shield_on else 'OFF'}")
            s_toggle = input("  Toggle shield? (y/n): ")
            if s_toggle.lower().startswith('y'):
                zoid.shield_on = not zoid.shield_on

        if zoid.has_stealth():
            print(f"  Stealth is currently {'ON' if zoid.stealth_on else 'OFF'}")
            st_toggle = input("  Toggle stealth? (y/n): ")
            if st_toggle.lower().startswith('y'):
                zoid.stealth_on = not zoid.stealth_on

        # ATTACK PHASE
        did_attack = False
        if not (zoid.status == "dazed" and did_move):
            range = get_range(distance)
            print(f"\n{zoid.name} is in {range} range of {enemy.name}.")
            if not zoid.can_attack(distance):
                print(f"{zoid.name} cannot attack {enemy.name} from {range} range!")
                if not did_move:
                    attack = input("  Do you want to move instead? (y/n): ")
                    if attack.lower().startswith('y'):
                        continue
                    else:
                        print(f"{zoid.name} skips the attack phase.")
                        turn += 1
                        continue
            else:
                attack = input("  Attack? (y/n): ")
                if attack.lower().startswith('y'):
                    print(f"{zoid.name} attacks {enemy.name} with a {range} attack!")
                    if range == "melee":
                        damage = zoid.melee
                    elif range == "close":
                        damage = zoid.close_range
                    elif range == "mid":
                        damage = zoid.mid_range
                    elif range == "long":
                        damage = zoid.long_range
                    if range== "melee":
                        attack_roll = random.randint(1, 20) + zoid.fighting
                        defense_roll = 10 + enemy.parry
                    else:
                        attack_roll = random.randint(1, 20) + zoid.dexterity
                        defense_roll = 10 + enemy.dodge
                    did_hit = attack_roll >= defense_roll
                    if did_hit:
                        print(f"{zoid.name} hits {enemy.name} for {damage} damage!")
                        if enemy.has_shield() and enemy.shield_on:
                            shield_roll = random.randint(1, 20) + enemy.shield
                            if shield_roll>=damage+15:
                                enemy.shieldDisabled=True
                                print(f"{enemy.name}'s shield is disabled!")
                        else:
                            toughness_roll = random.randint(1, 20) + enemy.toughness - enemy.dents
                            damageDifference = damage + 15 - toughness_roll
                            if damageDifference <=0:
                                print(f"{enemy.name} successfully defends against the attack!")
                            elif damageDifference<=5:
                                print(f"{enemy.name} takes a minor hit!")
                                enemy.dents += 1
                                print(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                            elif damageDifference <= 10:
                                print(f"{enemy.name} takes a moderate hit!")
                                enemy.dents += 1
                                print(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                                print(f"{enemy.name} is now DAZED! ")
                                enemy.status = "dazed"
                            elif damageDifference <= 15:
                                print(f"{enemy.name} takes a heavy hit!")
                                enemy.dents += 1
                                print(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                                print(f"{enemy.name} is now STUNNED! ")
                                enemy.status = "stunned"
                            else:
                                print(f"{enemy.name} takes a critical hit!")
                                enemy.dents += 1
                                print(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                                print(f"{enemy.name} is now DEFEATED! ")
                                enemy.status = "defeated"
                    else:
                        print(f"{zoid.name} misses the attack on {enemy.name}!")
                    did_attack = True

        # End of turn status logic
        if prior_status == "stunned":
            zoid.status = "dazed"
        elif prior_status == "dazed":
            zoid.status = "intact"

        turn += 1

def main():
    zoids = load_zoids("ConvertedZoidStats.json")
    battle_type = pick_battle_type()
    filtered_zoids = filter_zoids(zoids, battle_type)
    if not filtered_zoids:
        print("No Zoids available for that environment!")
        return
    player1_zoid = choose_zoid(filtered_zoids, 1)
    player2_zoid = choose_zoid(filtered_zoids, 2)
    print(f"\nPlayer 1: {player1_zoid.name} vs Player 2: {player2_zoid.name}")
    game_loop(player1_zoid, player2_zoid, battle_type)

if __name__ == "__main__":
    main()
