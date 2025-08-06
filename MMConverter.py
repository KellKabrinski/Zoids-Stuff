import json
import random

def calculateSpeedRank(speedMPH):
    if speedMPH == 0:
        return 0
    elif speedMPH < 120:
        return 5
    elif speedMPH < 250:
        return 6
    elif speedMPH < 500:
        return 7
    elif speedMPH < 1000:
        return 8
    elif speedMPH < 2000:
        return 9
    elif speedMPH < 4000:
        return 10
    print (f"Warning: Speed {speedMPH} is not in the expected range, defaulting to 0.")
    return 0
def convert_zoid_stats(input_file, output_file):
    with open(input_file, 'r', encoding='utf-8') as infile:
        zoids = json.load(infile)

    converted = []
    log=dict()

    for zoid in zoids:
        land_speed_rank=0
        water_speed_rank=0
        air_speed_rank=0
        try:
            name = zoid["Zoid"]
            melee = int(zoid.get("Melee", 0))
            close = int(zoid.get("Close-Range", 0))
            mid = int(zoid.get("Mid-Range", 0))
            long = int(zoid.get("Long-Range", 0))
            armour = int(zoid.get("Armour", 0))
            mobility = int(zoid.get("Mobility", 0))
            handling = int(zoid.get("Handling", 0))
            detection = int(zoid.get("Detection", 0))
            e_shield = int(zoid.get("E-Shield", 0))
            stealth = int(zoid.get("Stealth", 0))
            ecm = int(zoid.get("ECM", 0))
        except (ValueError, KeyError):
            continue

        # Movement conversions
        try:
            land_speed_kph = float(zoid.get("Ground Speed", 0))
            land_speed_m6s = round((land_speed_kph * 1000) / 600, 1)
            land_speed_mph = land_speed_kph * 0.621371
            land_speed_rank = calculateSpeedRank(land_speed_mph)

        except ValueError:
            land_speed_m6s = 0

        try:
            water_speed_knots = float(zoid.get("Water Speed", 0))
            water_speed_m6s = round((water_speed_knots * 1852) / 600, 1)
            water_speed_mph = water_speed_knots * 1.15078
            water_speed_rank = calculateSpeedRank(water_speed_mph)
        except ValueError:
            water_speed_m6s = 0

        try:
            air_speed_mach = float(zoid.get("Air Speed", 0))
            air_speed_m6s = round((air_speed_mach * 343000) / 600, 1)
            air_speed_mph = air_speed_mach * 761.207
            air_speed_rank = calculateSpeedRank(air_speed_mph)
        except ValueError:
            air_speed_m6s = 0
            air_speed_rank = 0
            print(f"Warning: Air Speed for {name} is not a valid number, defaulting to 0.")

        highest_ranged = max(close, mid, long)

        fighting = (mobility + handling + melee) // 3
        dexterity = mobility
        agility = (mobility + handling + highest_ranged) // 3
        strength = melee
        awareness = detection

        stat_cost = 2
        total_power_points = (
            fighting * stat_cost +
            agility * stat_cost +
            dexterity * stat_cost +
            strength * stat_cost +
            awareness * stat_cost
        )

        total_power_points += land_speed_rank + water_speed_rank + 2*air_speed_rank

        powers = []
        max_ranged = 0
        

        if melee > 0:
            powers.append({
                "Type": "Melee",
                "Rank": melee,
                "Power Points": 0
            })

        if close > 0:
            pp = close * 2
            powers.append({
                "Type": "Close-Range",
                "Rank": close,
                "Extras": ["Increased Range"],
                "Power Points": pp
            })
            total_power_points += pp
            max_ranged = max(max_ranged, close)

        if mid > 0:
            pp = close * 2 + 1
            powers.append({
                "Type": "Mid-Range",
                "Rank": mid,
                "Extras": ["Increased Range", "Extended Range 1"],
                "Power Points": pp
            })
            total_power_points += pp
            max_ranged = max(max_ranged, mid)

        if long > 0:
            pp = close * 2 + 2
            powers.append({
                "Type": "Long-Range",
                "Rank": long,
                "Extras": ["Increased Range", "Extended Range 2"],
                "Power Points": pp
            })
            total_power_points += pp
            max_ranged = max(max_ranged, long)

        if max_ranged < melee:
            powers.append({
                "Type": "Close Combat",
                "Rank": 3,
                "Power Points": 3
                })
            total_power_points += 3
        else:
            powers.append({
                "Type": "Ranged Combat",
                "Rank": 2,
                "Power Points": 2
            })
            total_power_points += 2
        
            

        if armour > 0:
            powers.append({
                "Type": "Protection",
                "Rank": armour,
                "Power Points": armour
            })
            total_power_points += armour

        if e_shield > 0:
            powers.append({
                "Type": "E-Shield",
                "Rank": e_shield,
                "Power Points": e_shield
            })
            total_power_points += e_shield

        if stealth > 0:
            visual_conceal = {
                "Type": "Concealment",
                "Rank": stealth,
                "Power Points": stealth * 0.5
            }
            powers.append(visual_conceal)
            total_power_points += stealth * 0.5

        if ecm > 0:
            sensor_conceal = {
                "Type": "Jamming",
                "Rank": ecm,
                "Power Points": ecm * 0.5
            }
            powers.append(sensor_conceal)
            total_power_points += ecm * 0.5
        if land_speed_rank>0:
            powers.append({
                "Type": "Speed",
                "Rank": land_speed_rank,
                "Power Points": land_speed_rank
            })
            if water_speed_rank > 0:
                powers.append({
                    "Type": "Swimming",
                    "Rank": water_speed_rank,
                    "Power Points": water_speed_rank
                })
            if air_speed_rank > 0:
                powers.append({
                    "Type": "Flight",
                    "Rank": air_speed_rank,
                    "Power Points": air_speed_rank
                })

        # Power Level calculation with tie tracking
        option_1 = (fighting + melee + 3)
        option_2 = (dexterity + max_ranged + 2)
        option_3 = (agility + armour)
        option_4 = (fighting + armour)

        power_level = max(option_1, option_2, option_3, option_4)

        power_level_sources = []
        if option_1 == power_level:
            power_level_sources.append("Fighting + Melee")
        if option_2 == power_level:
            power_level_sources.append("Dexterity + Ranged")
        if option_3 == power_level:
            power_level_sources.append("Agility + Toughness")
        if option_4 == power_level:
            power_level_sources.append("Fighting + Toughness")

        converted.append({
            "Name": name,
            "Stats": {
                "Fighting": fighting,
                "Strength": strength,
                "Dexterity": dexterity,
                "Agility": agility,
                "Awareness": awareness
            },
            "Defenses": {
                "Toughness": armour,
                "Parry": fighting,
                "Dodge": agility
            },
            "Powers": powers,
            "Movement": {
                "Land": land_speed_m6s,
                "Water": water_speed_m6s,
                "Air": air_speed_m6s
            },
            "Total Power Points": total_power_points,
            "Power Level": power_level,
            "Power Level Source": power_level_sources,
            "Cost": total_power_points*350
        })
        # Logging for averages by power level
        pl_key = power_level
        if pl_key not in log:
            log[pl_key] = {
                "count": 0,
                "melee": 0,
                "best ranged": 0,
                "toughness": 0
            }
        log[pl_key]["count"] += 1
        log[pl_key]["melee"] += melee
        log[pl_key]["best ranged"] += max_ranged
        log[pl_key]["toughness"] += armour
            

    with open(output_file, 'w', encoding='utf-8') as outfile:
        json.dump(converted, outfile, indent=4)
    return log
    

# Example usage
input_path = 'ZoidStats.json'
output_path = 'ConvertedZoidStats.json'
log = convert_zoid_stats(input_path, output_path)
for power_level in sorted(list(log)):
    data= log[power_level]
    if data['count'] == 0:
        continue
    print(f"Power Level {power_level}")
    print(f"  Average Melee: {data['melee'] / data['count'] if data['count'] > 0 else 0}")
    print(f"  Average Best Ranged: {data['best ranged'] / data['count'] if data['count'] > 0 else 0}")
    print(f"  Average Toughness: {data['toughness'] / data['count'] if data['count'] > 0 else 0}")
    print()
