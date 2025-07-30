import json
import os

def format_zoid(zoid):
    lines = [f"Name: {zoid['Name']}"]

    lines.append("\nStats:")
    for key, value in zoid.get("Stats", {}).items():
        lines.append(f"  {key}: {value}")

    lines.append("\nDefenses:")
    for key, value in zoid.get("Defenses", {}).items():
        lines.append(f"  {key}: {value}")

    lines.append("\nPowers:")
    for power in zoid.get("Powers", []):
        power_lines = [f"  - Type: {power['Type']}"]
        for key, value in power.items():
            if key != "Type":
                if isinstance(value, list):
                    power_lines.append(f"    {key}: {', '.join(map(str, value))}")
                else:
                    power_lines.append(f"    {key}: {value}")
        lines.extend(power_lines)

    lines.append("\nMovement:")
    for key, value in zoid.get("Movement", {}).items():
        lines.append(f"  {key}: {value} m/6s")

    lines.append(f"\nTotal Power Points: {zoid['Total Power Points']}")
    lines.append(f"Power Level: {zoid['Power Level']}")
    lines.append(f"Power Level Source(s): {', '.join(zoid['Power Level Source'])}")
    lines.append(f"Cost: {zoid['Cost']} Credits")

    return "\n".join(lines)

def generate_zoid_texts(input_json_path, output_dir):
    with open(input_json_path, "r", encoding="utf-8") as infile:
        zoids = json.load(infile)

    os.makedirs(output_dir, exist_ok=True)

    for zoid in zoids:
        safe_name = zoid["Name"].replace(" ", "_").replace("/", "_")
        file_path = os.path.join(output_dir, f"{safe_name}.txt")
        with open(file_path, "w", encoding="utf-8") as outfile:
            outfile.write(format_zoid(zoid))

    print(f"Exported {len(zoids)} Zoid files to: {output_dir}")

# Example usage:
generate_zoid_texts("ConvertedZoidStats.json", "ZoidTextFiles")
