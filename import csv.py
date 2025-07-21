import csv
import json

def csv_to_json(csv_file_path, json_file_path):
    data = []

    # Open and read the CSV file
    with open(csv_file_path, newline='', encoding='utf-8') as csvfile:
        reader = csv.DictReader(csvfile)
        for row in reader:
            data.append(row)

    # Write to JSON file
    with open(json_file_path, 'w', encoding='utf-8') as jsonfile:
        json.dump(data, jsonfile, indent=4)

# Example usage
csv_file = 'input.csv'
json_file = 'ZoidStats.json'
csv_to_json(csv_file, json_file)
