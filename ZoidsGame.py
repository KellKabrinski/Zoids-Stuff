import json
import math
import random
import inputHandlers
import sys

class Zoid:
    def __init__(self, zoid_data, output_func=None):
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
        self.shield = next((p.get('Rank') for p in self.powers if p['Type'] == 'E-Shield'), None)
        self.shieldDisabled=False
        self.stealth = next((p.get('Rank') for p in self.powers if p['Type'] == 'Concealment' and 'Visual' in p.get('Senses', [])), None)
        self.armor = next((p.get('Rank') for p in self.powers if p['Type'] == 'Armor'), None)

        
        self.shield_on = False
        self.stealth_on = False
        self.dents = 0
        self.angle = 0.0
        self.status = "intact"  # "intact", "dazed", "stunned", "defeated"
        self.output_func = output_func if output_func else print
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
        self.output_func(f"\n{self.name}'s status: "
              f"Shield={'ON' if self.shield_on else 'OFF'} (Rank={self.shield if self.shield is not None else '-'})"
              f", Stealth={'ON' if self.stealth_on else 'OFF'} (Rank={self.stealth if self.stealth is not None else '-'})"
              f", Dents={self.dents}, Status={self.status.capitalize()}")
        self.output_func("Stats: "
              f"Fighting={self.fighting}, Strength={self.strength}, Dexterity={self.dexterity}, "
              f"Agility={self.agility}, Awareness={self.awareness} | "
              f"Toughness={self.toughness}, Parry={self.parry}, Dodge={self.dodge} | "
              f"Land={self.land}, Water={self.water}, Air={self.air}")
        self.output_func("Attacks: "
              f"Melee={self.melee if self.melee is not None else '-'}, "
              f"Close={self.close_range if self.close_range is not None else '-'}, "
              f"Mid={self.mid_range if self.mid_range is not None else '-'}, "
              f"Long={self.long_range if self.long_range is not None else '-'}")
        self.output_func(f"Armor: {self.armor if self.armor is not None else '-'}")
        self.output_func(f"Angle: {self.angle}° (0° is facing enemy)")

class GameController:
    def __init__(self, z1, z2, battle_type, output_func=None):
        self.z1 = z1
        self.z2 = z2
        self.battle_type = battle_type
        self.zoid_objs = {1: z1, 2: z2}
        self.order = pick_first(z1, z2)
        self.turn = 0
        self.distance = 0
        self.output_func = output_func if output_func else print
        self.enemyDetected = True

    def set_starting_distance(self, distance):
        self.distance = distance

    def get_current_player(self):
        player = self.order[self.turn % 2]
        return self.zoid_objs[player], self.zoid_objs[1 if player == 2 else 2]

    def print_status(self):
        self.output_func(f"\n{self.z1.name}'s status: Shield={'ON' if self.z1.shield_on else 'OFF'} (Rank={self.z1.shield if self.z1.shield is not None else '-'})"
                        f", Stealth={'ON' if self.z1.stealth_on else 'OFF'} (Rank={self.z1.stealth if self.z1.stealth is not None else '-'})"
                        f", Dents={self.z1.dents}, Status={self.z1.status.capitalize()}")
        self.output_func(f"{self.z2.name}'s status: Shield={'ON' if self.z2.shield_on else 'OFF'} (Rank={self.z2.shield if self.z2.shield is not None else '-'})"
                        f", Stealth={'ON' if self.z2.stealth_on else 'OFF'} (Rank={self.z2.stealth if self.z2.stealth is not None else '-'})"
                        f", Dents={self.z2.dents}, Status={self.z2.status.capitalize()}")

    def do_turn(self, move_attack=None, move=None, angle_change=None, attack=None, shield_toggle=None, stealth_toggle=None):
        zoid, enemy = self.get_current_player()
        prior_status = zoid.status
        # Concealment: search at start of turn if enemy is stealthed
        self.enemyDetected = True
        if enemy.stealth_on:
            self.output_func(f"\n{enemy.name} is in stealth mode!")
            self.enemyDetected = search_check(zoid, enemy)
            if not self.enemyDetected:
                self.output_func(f"{zoid.name} cannot locate {enemy.name}!")
        self.print_status()
        self.output_func(f"\nCurrent distance between Zoids: {self.distance:.1f} meters")
        self.output_func(f"{zoid.name}'s turn!")

        # STUNNED: Cannot move or attack
        if zoid.status == "stunned":
            self.output_func("You are STUNNED! You cannot move or attack this turn.")
            self.handle_shield_stealth(zoid, shield_toggle, stealth_toggle)
            zoid.status = "dazed"
            self.turn += 1
            return

        did_move = False

        # DAZED: Can move or attack, not both
        if zoid.status == "dazed":
            self.output_func("You are DAZED! You may move OR attack, not both.")
            if move_attack and move_attack.lower().startswith('m'):
                self.output_func("Choose maneuver:")
                self.distance, did_move, self.enemyDetected = self.handle_movement(zoid, enemy, move, angle_change)
        else:
            # MOVEMENT PHASE
            self.output_func("Choose maneuver:")
            self.distance, did_move, self.enemyDetected = self.handle_movement(zoid, enemy, move, angle_change)

        # SHIELD & STEALTH PHASE (always available)
        self.handle_shield_stealth(zoid, shield_toggle, stealth_toggle)

        # ATTACK PHASE
        did_attack = False
        if zoid.shield_on and zoid.has_shield():
            self.output_func(f"{zoid.name} cannot attack while shield is on.")
            self.turn += 1
            return
        if not (zoid.status == "dazed" and did_move):
            range = get_range(self.distance)
            self.output_func(f"\n{zoid.name} is in {range} range of {enemy.name}.")
            if not zoid.can_attack(self.distance):
                self.output_func(f"{zoid.name} cannot attack {enemy.name} from {range} range!")
                self.output_func(f"{zoid.name} skips the attack phase.")
                self.turn += 1
                return
            else:
                if attack and attack.lower().startswith('y'):
                    # Miss chance if enemy is still concealed
                    if enemy.stealth_on and not self.enemyDetected:
                        self.output_func("Target is concealed! 50% miss chance.")
                        if random.choice([True, False]):
                            self.output_func("Your attack misses the target's last known location!")
                            did_attack = True
                        else:
                            self.output_func("You get lucky and land a hit despite concealment!")
                    if not (enemy.stealth_on and not self.enemyDetected) or not did_attack:
                        self.output_func(f"{zoid.name} attacks {enemy.name} with a {range} attack!")
                        attack_roll = 0
                        defense_roll = 0
                        damage = 0
                        if range == "melee":
                            damage = zoid.melee
                            attack_roll = random.randint(1, 20) + zoid.fighting
                            defense_roll = 10 + enemy.parry
                        elif range == "close":
                            damage = zoid.close_range
                            attack_roll = random.randint(1, 20) + zoid.dexterity
                            defense_roll = 10 + enemy.dodge
                        elif range == "mid":
                            damage = zoid.mid_range
                            attack_roll = random.randint(1, 20) + zoid.dexterity
                            defense_roll = 10 + enemy.dodge
                        elif range == "long":
                            damage = zoid.long_range
                            attack_roll = random.randint(1, 20) + zoid.dexterity
                            defense_roll = 10 + enemy.dodge
                        did_hit = attack_roll >= defense_roll
                        if did_hit:
                            self.output_func(f"Attack roll: {attack_roll} vs Defense roll: {defense_roll}")
                            self.output_func(f"{zoid.name} hits {enemy.name} for {damage} damage!")
                            if enemy.has_shield() and enemy.shield_on and is_attack_in_shield_arc(zoid, enemy):
                                shield_roll = random.randint(1, 20) + enemy.shield
                                if shield_roll >= damage + 15:
                                    enemy.shieldDisabled = True
                                    self.output_func(f"{enemy.name}'s shield is disabled!")
                            else:
                                toughness_roll = random.randint(1, 20) + enemy.toughness - enemy.dents
                                self.output_func(f"Enemy toughness roll: {toughness_roll} (Toughness: {enemy.toughness}, Dents: {enemy.dents})")
                                damageDifference = damage + 15 - toughness_roll
                                if damageDifference <= 0:
                                    self.output_func(f"{enemy.name} successfully defends against the attack!")
                                elif damageDifference <= 5:
                                    self.output_func(f"{enemy.name} takes a minor hit!")
                                    enemy.dents += 1
                                    self.output_func(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                                elif damageDifference <= 10:
                                    self.output_func(f"{enemy.name} takes a moderate hit!")
                                    enemy.dents += 1
                                    self.output_func(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                                    self.output_func(f"{enemy.name} is now DAZED! ")
                                    enemy.status = "dazed"
                                elif damageDifference <= 15:
                                    self.output_func(f"{enemy.name} takes a heavy hit!")
                                    enemy.dents += 1
                                    self.output_func(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                                    self.output_func(f"{enemy.name} is now STUNNED! ")
                                    enemy.status = "stunned"
                                else:
                                    self.output_func(f"{enemy.name} takes a critical hit!")
                                    enemy.dents += 1
                                    self.output_func(f"{enemy.name} receives a DENT! (Total dents: {enemy.dents})")
                                    self.output_func(f"{enemy.name} is now DEFEATED! ")
                                    enemy.status = "defeated"
                        else:
                            self.output_func(f"{zoid.name} misses the attack on {enemy.name}!")
                        did_attack = True

        # End of turn status logic
        if prior_status == "stunned":
            zoid.status = "dazed"
        elif prior_status == "dazed":
            zoid.status = "intact"

        

    def handle_movement(self, zoid, enemy, move, angle_change):
        speed = zoid.get_speed(self.battle_type)
        enemyDetected = self.enemyDetected
        distance = self.distance
        did_move = False
        if not enemyDetected:
            if move == "1":
                direction = random.choice(['closer', 'retreat'])
                if direction == 'closer':
                    distance = max(0, distance - speed * 0.5)
                else:
                    distance += speed * 0.5
                did_move = True
                enemyDetected = search_check(zoid, enemy)
            else:
                did_move = False
        else:
            if move == "1":
                distance = max(0, distance - speed)
                did_move = True
            elif move == "2":
                distance += speed
                did_move = True
            elif move in ("3","4"):
                max_angle = max_circling_angle(speed, distance)
                if angle_change is not None and 0 <= angle_change <= max_angle:
                    if move == "3":
                        zoid.angle = (zoid.angle + angle_change) % 360
                        self.output_func(f"You circle left! New angle: {zoid.angle:.1f}°")
                    else:
                        zoid.angle = (zoid.angle - angle_change) % 360
                        self.output_func(f"You circle right! New angle: {zoid.angle:.1f}°")
                    did_move = True
            elif move == "5":
                did_move = False
        return distance, did_move, enemyDetected

    def handle_shield_stealth(self, zoid, shield_toggle, stealth_toggle):
        if zoid.has_shield() and not zoid.shieldDisabled:
            self.output_func(f"  Shield is currently {'ON' if zoid.shield_on else 'OFF'}")
            if shield_toggle and shield_toggle.lower().startswith('y'):
                zoid.shield_on = not zoid.shield_on
        if zoid.has_stealth():
            self.output_func(f"  Stealth is currently {'ON' if zoid.stealth_on else 'OFF'}")
            if stealth_toggle and stealth_toggle.lower().startswith('y'):
                zoid.stealth_on = not zoid.stealth_on

    

def load_zoids(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)

def pick_battle_type():
    print("\nChoose battle type:")
    print("1: Land")
    print("2: Water")
    print("3: Air")
    while True:
        choice = inputHandlers.get_battle_type_input()
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
    print_func = print
    if zoids_by_pl and isinstance(zoids_by_pl[0], Zoid):
        print_func = zoids_by_pl[0].output_func
    print_func("\nAvailable Zoids:")
    for idx, z in enumerate(zoids_by_pl):
        print_func(f"{idx + 1}: {z['Name']} (PL {z.get('Power Level', 0)})")
    return zoids_by_pl

def choose_zoid(zoids, player_num):
    zoids_by_pl = display_zoids(zoids)
    while True:
        try:
            choice = int(inputHandlers.get_zoid_choice_input(player_num)) - 1
            if 0 <= choice < len(zoids_by_pl):
                return Zoid(zoids_by_pl[choice])
        except ValueError:
            pass
        print("Invalid input. Try again.")

def pick_first(z1, z2):
    first = random.choice([1, 2])
    output_func = z1.output_func if hasattr(z1, 'output_func') else print
    output_func(f"\n{z1.name if first==1 else z2.name} goes first!\n")
    return (1, 2) if first == 1 else (2, 1)

def get_starting_distance():
    while True:
        try:
            dist = float(inputHandlers.get_starting_distance_input())
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

def is_attack_in_shield_arc(attacker,defender):
    rel_angle=(attacker.angle-defender.angle) % 360
    if rel_angle > 180:
        rel_angle = 360 - rel_angle
    return abs(rel_angle) <= 45

def d20():
    return random.randint(1, 20)

def search_check(searcher: Zoid, target: Zoid):
    roll = d20()
    total = roll + searcher.awareness
    if target.has_stealth() and target.stealth_on and target.stealth is not None:
        target_dc = 5 + target.stealth
    else:
        target_dc=0
    output_func = searcher.output_func if hasattr(searcher, 'output_func') else print
    output_func(f"  Search Check: d20({roll}) + Awareness({searcher.awareness}) = {total} vs DC {target_dc}")
    if total >= target_dc:
        output_func("  Enemy detected!")
        return True
    else:
        output_func("  You fail to locate the enemy!")
        return False
    
def max_circling_angle(speed, distance):
    if distance <= 0.1:  # Allow full 360 at melee
        return 360
    return min(360, (speed * 180) / (math.pi * distance))


def game_loop(z1, z2, battle_type):
    zoid_objs = {1: z1, 2: z2}
    order = pick_first(z1, z2)
    turn = 0
    distance = get_starting_distance()
    while z1.status != "defeated" and z2.status != "defeated":
        player = order[turn % 2]
        zoid = zoid_objs[player]
        enemy = zoid_objs[1 if player == 2 else 2]
        # Concealment: search at start of turn if enemy is stealthed
        enemyDetected = True
        if enemy.stealth_on:
            print(f"\n{enemy.name} is in stealth mode!")
            enemyDetected = search_check(zoid, enemy)
            if not enemyDetected:
                print(f"{zoid.name} cannot locate {enemy.name}!")
        zoid.print_status()
        print(f"\nCurrent distance between Zoids: {distance:.1f} meters")
        print(f"{zoid.name}'s turn!")

        prior_status = zoid.status

        # STUNNED: Cannot move or attack
        if zoid.status == "stunned":
            print("You are STUNNED! You cannot move or attack this turn.")
            ShieldAndStealth(zoid)
            zoid.status = "dazed"
            turn += 1
            continue

        did_move = False

        # DAZED: Can move or attack, not both
        if zoid.status == "dazed":
            print("You are DAZED! You may move OR attack, not both.")
            move_attack = inputHandlers.get_move_attack_input()
            if move_attack.lower().startswith('m'):
                print("Choose maneuver:")
                distance, did_move, enemyDetected = Movement(
                    battle_type, distance, zoid, did_move, enemyDetected, enemy
                )
        else:
            # MOVEMENT PHASE
            print("Choose maneuver:")
            distance, did_move, enemyDetected = Movement(
                battle_type, distance, zoid, did_move, enemyDetected, enemy
            )

        # SHIELD & STEALTH PHASE (always available)
        ShieldAndStealth(zoid)

        # ATTACK PHASE
        did_attack = False
        if zoid.shield_on and zoid.has_shield():
            print(f"{zoid.name} cannot attack while shield is on.")
            turn += 1
            continue
        if not (zoid.status == "dazed" and did_move):
            range = get_range(distance)
            print(f"\n{zoid.name} is in {range} range of {enemy.name}.")
            if not zoid.can_attack(distance):
                print(f"{zoid.name} cannot attack {enemy.name} from {range} range!")
                print(f"{zoid.name} skips the attack phase.")
                turn += 1
                continue
            else:
                attack = inputHandlers.get_attack_input()
                if attack.lower().startswith('y'):
                    # Miss chance if enemy is still concealed
                    if enemy.stealth_on and not enemyDetected:
                        print("Target is concealed! 50% miss chance.")
                        if random.choice([True, False]):
                            print("Your attack misses the target's last known location!")
                            did_attack = True
                            # End attack phase
                        else:
                            print("You get lucky and land a hit despite concealment!")
                            # Proceed to attack as normal below
                    if not (enemy.stealth_on and not enemyDetected) or not did_attack:
                        print(f"{zoid.name} attacks {enemy.name} with a {range} attack!")
                        attack_roll = 0
                        defense_roll = 0
                        damage = 0
                        if range == "melee":
                            damage = zoid.melee
                            attack_roll = random.randint(1, 20) + zoid.fighting
                            defense_roll = 10 + enemy.parry
                        elif range == "close":
                            damage = zoid.close_range
                            attack_roll = random.randint(1, 20) + zoid.dexterity
                            defense_roll = 10 + enemy.dodge
                        elif range == "mid":
                            damage = zoid.mid_range
                            attack_roll = random.randint(1, 20) + zoid.dexterity
                            defense_roll = 10 + enemy.dodge
                        elif range == "long":
                            damage = zoid.long_range
                            attack_roll = random.randint(1, 20) + zoid.dexterity
                            defense_roll = 10 + enemy.dodge
                        did_hit = attack_roll >= defense_roll
                        if did_hit:
                            print(f"Attack roll: {attack_roll} vs Defense roll: {defense_roll}")
                            print(f"{zoid.name} hits {enemy.name} for {damage} damage!")
                            if enemy.has_shield() and enemy.shield_on and is_attack_in_shield_arc(zoid, enemy):
                                shield_roll = random.randint(1, 20) + enemy.shield
                                if shield_roll >= damage + 15:
                                    enemy.shieldDisabled = True
                                    print(f"{enemy.name}'s shield is disabled!")
                            else:
                                toughness_roll = random.randint(1, 20) + enemy.toughness - enemy.dents
                                print(f"Enemy toughness roll: {toughness_roll} (Toughness: {enemy.toughness}, Dents: {enemy.dents})")
                                damageDifference = damage + 15 - toughness_roll
                                if damageDifference <= 0:
                                    print(f"{enemy.name} successfully defends against the attack!")
                                elif damageDifference <= 5:
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

def Movement(battle_type, distance, zoid, did_move, enemyDetected, enemyZoid):
    speed = zoid.get_speed(battle_type)
    # If the enemy is not detected (concealment working), restrict options
    if not enemyDetected:
        move = inputHandlers.get_movement_input(True)
        if move == "1":
            direction = random.choice(['closer', 'retreat'])
            if direction == 'closer':
                distance = max(0, distance - speed * 0.5)
            else:
                distance += speed * 0.5
            did_move = True
            # New search check after random movement
            enemyDetected = search_check(zoid, enemyZoid)
        else:
            did_move = False
            # No additional search check if standing still
    else:
        move = inputHandlers.get_movement_input(False)
        if move == "1":
            distance = max(0, distance - speed)
            did_move = True
        elif move == "2":
            distance += speed
            did_move = True
        elif move in ("3","4"):
            max_angle=max_circling_angle(speed,distance)
            while True:
                try:
                    angle_change = float(inputHandlers.get_angle_change_input(max_angle))
                    if 0 <= angle_change <= max_angle:
                        break
                except ValueError:
                    pass
                print("Invalid angle. Try again.")
            if move == "3":
                zoid.angle = (zoid.angle + angle_change) % 360
                print(f"You circle left! New angle: {zoid.angle:.1f}°")
            else:
                zoid.angle = (zoid.angle - angle_change) % 360
                print(f"You circle right! New angle: {zoid.angle:.1f}°")
            did_move = True
        elif move == "5":
            did_move = False
    return distance, did_move, enemyDetected

def ShieldAndStealth(zoid):
    if zoid.has_shield() and not zoid.shieldDisabled:
        print(f"  Shield is currently {'ON' if zoid.shield_on else 'OFF'}")
        s_toggle = inputHandlers.get_shield_toggle_input(zoid.shield_on)
        if s_toggle.lower().startswith('y'):
            zoid.shield_on = not zoid.shield_on

    if zoid.has_stealth():
        print(f"  Stealth is currently {'ON' if zoid.stealth_on else 'OFF'}")
        st_toggle = inputHandlers.get_stealth_toggle_input(zoid.stealth_on)
        if st_toggle.lower().startswith('y'):
            zoid.stealth_on = not zoid.stealth_on

def main():
    if len(sys.argv) > 1 and sys.argv[1].upper() == "GUI":
        import ZoidsGameGUI
        ZoidsGameGUI.start_GUI()
        return
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
