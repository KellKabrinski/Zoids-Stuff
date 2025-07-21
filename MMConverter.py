import json

def convert_zoid_stats(input_file, output_file):
    with open(input_file, 'r', encoding='utf-8') as infile:
        zoids = json.load(infile)

    converted = []

    for zoid in zoids:
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
        except ValueError:
            land_speed_m6s = 0

        try:
            water_speed_knots = float(zoid.get("Water Speed", 0))
            water_speed_m6s = round((water_speed_knots * 1852) / 600, 1)
        except ValueError:
            water_speed_m6s = 0

        try:
            air_speed_mach = float(zoid.get("Air Speed", 0))
            air_speed_m6s = round((air_speed_mach * 343000) / 600, 1)
        except ValueError:
            air_speed_m6s = 0

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

        powers = []
        max_ranged = 0

        if melee > 0:
            powers.append({
                "Type": "Melee",
                "Damage": melee,
                "Power Points": 0
            })

        if close > 0:
            pp = close * 2
            powers.append({
                "Type": "Close-Range",
                "Damage": close,
                "Extras": ["Increased Range"],
                "Power Points": pp
            })
            total_power_points += pp
            max_ranged = max(max_ranged, close)

        if mid > 0:
            pp = close * 2 + 1
            powers.append({
                "Type": "Mid-Range",
                "Damage": mid,
                "Extras": ["Increased Range", "Extended Range 1"],
                "Power Points": pp
            })
            total_power_points += pp
            max_ranged = max(max_ranged, mid)

        if long > 0:
            pp = close * 2 + 2
            powers.append({
                "Type": "Long-Range",
                "Damage": long,
                "Extras": ["Increased Range", "Extended Range 2"],
                "Power Points": pp
            })
            total_power_points += pp
            max_ranged = max(max_ranged, long)

        if armour > 0:
            powers.append({
                "Type": "Armor",
                "Protection": armour,
                "Power Points": armour
            })
            total_power_points += armour

        if e_shield > 0:
            powers.append({
                "Type": "Create (E-Shield)",
                "Rank": e_shield,
                "Extras": [
                    "Tethered",
                    "Limited to hemisphere in front of Zoid",
                    "Limited (Damage remains after dismissing)"
                ],
                "Power Points": e_shield
            })
            total_power_points += e_shield

        if stealth > 0:
            visual_conceal = {
                "Type": "Concealment",
                "Senses": ["Visual"],
                "Rank": stealth,
                "Power Points": stealth * 0.5
            }
            powers.append(visual_conceal)
            total_power_points += stealth * 0.5

        if ecm > 0:
            sensor_conceal = {
                "Type": "Concealment",
                "Senses": ["Sensor"],
                "Rank": ecm,
                "Power Points": ecm * 0.5
            }
            powers.append(sensor_conceal)
            total_power_points += ecm * 0.5

        # Power Level calculation with tie tracking
        option_1 = fighting + melee
        option_2 = dexterity + max_ranged
        option_3 = agility + armour
        option_4 = fighting + armour

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
            "Power Level Source": power_level_sources
        })

    with open(output_file, 'w', encoding='utf-8') as outfile:
        json.dump(converted, outfile, indent=4)

# Example usage
input_path = 'ZoidStats.json'
output_path = 'ConvertedZoidStats.json'
convert_zoid_stats(input_path, output_path)
