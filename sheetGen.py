import json
import os

def format_zoid(zoid):
    safe_name = zoid["Name"].replace(" ", "_").replace("/", "_").lower()
    lines = [f"====== {zoid['Name']} ======"]
    
    # Add lore link at the top
    lines.append(f"[[{safe_name}|Lore]] | **RPG Stats**")
    lines.append("")

    # Stats Table
    lines.append("===== Stats =====")
    lines.append("^ Attribute ^ Value ^")
    for key, value in zoid.get("Stats", {}).items():
        lines.append(f"| {key} | {value} |")

    # Defenses Table
    lines.append("===== Defenses =====")
    lines.append("^ Defense Type ^ Value ^")
    for key, value in zoid.get("Defenses", {}).items():
        lines.append(f"| {key} | {value} |")

    # Powers Section
    lines.append("")
    lines.append("===== Powers =====")
    powers = zoid.get("Powers", [])
    if powers:
        lines.append("^ Type ^ Details ^")
        for power in powers:
            power_type = power.get('Type', 'Unknown')
            details = []
            for key, value in power.items():
                if key != "Type":
                    if isinstance(value, list):
                        details.append(f"**{key}**: {', '.join(map(str, value))}")
                    else:
                        details.append(f"**{key}**: {value}")
            detail_text = " \\\\ ".join(details) if details else "—"
            lines.append(f"| {power_type} | {detail_text} |")
    else:
        lines.append("No special powers.")

    # Movement Table
    lines.append("")
    lines.append("===== Movement =====")
    lines.append("^ Terrain ^ Speed ^")
    for key, value in zoid.get("Movement", {}).items():
        lines.append(f"| {key} | {value} m/6s |")

    # Summary Information
    lines.append("")
    lines.append("===== Summary =====")
    lines.append("^ Attribute ^ Value ^")
    lines.append(f"| Total Power Points | {zoid['Total Power Points']} |")
    lines.append(f"| Power Level | {zoid['Power Level']} |")
    lines.append(f"| Power Level Source(s) | {', '.join(zoid['Power Level Source'])} |")
    lines.append(f"| Cost | {zoid['Cost']} Credits |")

    return "\n".join(lines)

def generate_zoid_index(input_json_path, output_dir):
    with open(input_json_path, "r", encoding="utf-8") as infile:
        zoids = json.load(infile)

    # Group zoids by power level
    power_level_groups = {}
    for zoid in zoids:
        power_level = zoid.get('Power Level', 0)
        if power_level not in power_level_groups:
            power_level_groups[power_level] = []
        power_level_groups[power_level].append(zoid)
    
    # Sort power levels
    sorted_power_levels = sorted(power_level_groups.keys())
    
    lines = ["====== Zoids RPG Stats Index ======"]
    lines.append("")
    lines.append("This page contains links to all available Zoid stat sheets, organized by Power Level.")
    lines.append("")
    
    # Create summary table
    lines.append("===== Power Level Summary =====")
    lines.append("^ Power Level ^ Number of Zoids ^")
    for power_level in sorted_power_levels:
        count = len(power_level_groups[power_level])
        lines.append(f"| {power_level} | {count} |")
    
    lines.append("")
    
    # Create detailed listings by power level
    for power_level in sorted_power_levels:
        zoids_at_level = power_level_groups[power_level]
        # Sort zoids alphabetically within each power level
        zoids_at_level.sort(key=lambda z: z['Name'])
        
        lines.append(f"===== Power Level {power_level} =====")
        
        # Create a table with Zoid info
        lines.append("^ Name ^ Cost (Credits) ^ Movement Type ^ Primary Weapons ^")
        
        for zoid in zoids_at_level:
            safe_name = zoid["Name"].replace(" ", "_").replace("/", "_").lower()
            zoid_link = f"[[{safe_name}_rp|{zoid['Name']}]]"
            
            # Get cost
            cost = f"{zoid.get('Cost', 0):,.0f}"
            
            # Determine primary movement type
            movement = zoid.get('Movement', {})
            movement_types = []
            if movement.get('Land', 0) > 0:
                movement_types.append('Land')
            if movement.get('Water', 0) > 0:
                movement_types.append('Water')
            if movement.get('Air', 0) > 0:
                movement_types.append('Air')
            movement_type = ', '.join(movement_types) if movement_types else 'None'
            
            # Get primary weapons (powers with combat types)
            powers = zoid.get('Powers', [])
            weapon_types = []
            for power in powers:
                power_type = power.get('Type', '')
                if any(keyword in power_type.lower() for keyword in ['melee', 'range', 'combat']):
                    if power_type not in weapon_types:
                        weapon_types.append(power_type)
            weapons = ', '.join(weapon_types) if weapon_types else 'None'
            
            lines.append(f"| {zoid_link} | {cost} | {movement_type} | {weapons} |")
        
        lines.append("")
    
    # Add footer
    lines.append("----")
    lines.append("//Generated automatically from Zoid stat data//")
    
    # Write the index file
    os.makedirs(output_dir, exist_ok=True)
    index_path = os.path.join(output_dir, "rp_stats.txt")
    with open(index_path, "w", encoding="utf-8") as outfile:
        outfile.write("\n".join(lines))
    
    print(f"Generated Zoid index page: {index_path}")
    return len(zoids), len(sorted_power_levels)

def generate_zoid_texts(input_json_path, output_dir):
    with open(input_json_path, "r", encoding="utf-8") as infile:
        zoids = json.load(infile)

    os.makedirs(output_dir, exist_ok=True)

    for zoid in zoids:
        safe_name = zoid["Name"].replace(" ", "_").replace("/", "_").lower()
        file_path = os.path.join(output_dir, f"{safe_name}_rp.txt")
        with open(file_path, "w", encoding="utf-8") as outfile:
            outfile.write(format_zoid(zoid))

    print(f"Exported {len(zoids)} Zoid files in DokuWiki format to: {output_dir}")

# Example usage:
generate_zoid_texts("ConvertedZoidStats.json", "ZoidTextFiles")
generate_zoid_index("ConvertedZoidStats.json", "ZoidTextFiles")
