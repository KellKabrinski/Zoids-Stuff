def get_battle_type_input():
    return input("Battle type: ")

def get_zoid_choice_input(player_num):
    return input(f"\nEnter number for Player {player_num}: ")

def get_starting_distance_input():
    return input("\nEnter starting distance between Zoids in meters: ")

def get_move_attack_input():
    return input("  Move (m) or Attack (a) or Skip (s)? ")

def get_attack_input():
    return input("  Attack? (y/n): ")

def get_movement_input(enemy_concealed):
    if enemy_concealed:
        return input("Enemy is concealed! 1: Search for Enemy  2: Stand Still\nChoice: ")
    else:
        return input("  1: Close\n  2: Retreat\n  3: Circle Left\n 4:Circle Right\n  5: Stand Still\n  Choice: ")

def get_angle_change_input(max_angle):
    return input(f"How many degrees do you want to circle? (0 to {max_angle:.1f}): ")

def get_shield_toggle_input(shield_on):
    return input("  Toggle shield? (y/n): ")

def get_stealth_toggle_input(stealth_on):
    return input("  Toggle stealth? (y/n): ")