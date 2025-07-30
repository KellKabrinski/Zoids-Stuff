import tkinter as tk
from tkinter import ttk, scrolledtext
import json
import ZoidsGame

class ZoidsGameApp:
    def reset_action_queue(self):
        self.queued_action = {
            'move_attack': None,
            'move': None,
            'angle_change': None,
            'attack': None,
            'shield_toggle': None,
            'stealth_toggle': None
        }
    def __init__(self, root):
        self.root = root
        self.root.title("Zoids Game GUI")
        self.zoids = ZoidsGame.load_zoids("ConvertedZoidStats.json")
        self.battle_type = tk.StringVar(value="land")
        self.filtered_zoids = ZoidsGame.filter_zoids(self.zoids, self.battle_type.get())
        self.player1_zoid = None
        self.player2_zoid = None
        self.output_lines = []
        self.setup_widgets()

    def setup_widgets(self):
        # Battle type selection
        battle_frame = tk.Frame(self.root)
        battle_frame.pack(pady=5)
        tk.Label(battle_frame, text="Battle Type:").pack(side=tk.LEFT)
        for bt in ["land", "water", "air"]:
            tk.Radiobutton(battle_frame, text=bt.capitalize(), variable=self.battle_type, value=bt, command=self.update_zoid_lists).pack(side=tk.LEFT)

        # Zoid selection dropdowns
        self.select_frame = tk.Frame(self.root)
        self.select_frame.pack(pady=5)
        tk.Label(self.select_frame, text="Player 1 Zoid:").grid(row=0, column=0)
        tk.Label(self.select_frame, text="Player 2 Zoid:").grid(row=1, column=0)
        self.p1_var = tk.StringVar()
        self.p2_var = tk.StringVar()
        self.p1_dropdown = ttk.Combobox(self.select_frame, textvariable=self.p1_var, state="readonly")
        self.p2_dropdown = ttk.Combobox(self.select_frame, textvariable=self.p2_var, state="readonly")
        self.p1_dropdown.grid(row=0, column=1)
        self.p2_dropdown.grid(row=1, column=1)
        self.update_zoid_lists()
        tk.Button(self.select_frame, text="Start Game", command=self.start_game).grid(row=2, column=0, columnspan=2, pady=5)

        # Status frames
        self.status_frame = tk.Frame(self.root)
        self.status_frame.pack(pady=5)
        self.p1_status = tk.Label(self.status_frame, text="Player 1 Status", anchor="w", justify="left")
        self.p1_status.pack(fill=tk.X)
        self.p2_status = tk.Label(self.status_frame, text="Player 2 Status", anchor="w", justify="left")
        self.p2_status.pack(fill=tk.X)

        # Action buttons
        self.action_frame = tk.Frame(self.root)
        self.action_frame.pack(pady=5)

        self.move_btn = tk.Button(self.action_frame, text="Move", command=lambda: self.do_action("move"), state=tk.DISABLED)
        self.move_btn.pack(side=tk.LEFT, padx=2)

        self.attack_btn = tk.Button(self.action_frame, text="Attack", command=lambda: self.do_action("attack"), state=tk.DISABLED)
        self.attack_btn.pack(side=tk.LEFT, padx=2)

        self.shield_var = tk.StringVar(value="n")
        self.shield_btn = tk.Button(self.action_frame, text="Toggle Shield", command=lambda: self.do_action("shield"), state=tk.DISABLED)
        self.shield_btn.pack(side=tk.LEFT, padx=2)

        self.stealth_var = tk.StringVar(value="n")
        self.stealth_btn = tk.Button(self.action_frame, text="Toggle Stealth", command=lambda: self.do_action("stealth"), state=tk.DISABLED)
        self.stealth_btn.pack(side=tk.LEFT, padx=2)

        self.turn_complete_btn = tk.Button(self.action_frame, text="End Turn", command=lambda: self.do_action("turn_complete"), state=tk.DISABLED)
        self.turn_complete_btn.pack(side=tk.LEFT, padx=2)

        # Output textbox
        self.output_box = scrolledtext.ScrolledText(self.root, height=12, width=80, state=tk.DISABLED)
        self.output_box.pack(pady=5)

    def update_zoid_lists(self):
        self.filtered_zoids = sorted(ZoidsGame.filter_zoids(self.zoids, self.battle_type.get()),key=lambda z: (z.get('Power Level', 0), z['Name']))
        
        zoid_names = [z["Name"] for z in self.filtered_zoids]
        self.p1_dropdown["values"] = zoid_names
        self.p2_dropdown["values"] = zoid_names
        if zoid_names:
            self.p1_var.set(zoid_names[0])
            self.p2_var.set(zoid_names[0])

    def start_game(self):
        self.reset_action_queue()
        p1_name = self.p1_var.get()
        p2_name = self.p2_var.get()
        p1_data = next(z for z in self.filtered_zoids if z["Name"] == p1_name)
        p2_data = next(z for z in self.filtered_zoids if z["Name"] == p2_name)
        self.player1_zoid = ZoidsGame.Zoid(p1_data, output_func=self.append_output)
        self.player2_zoid = ZoidsGame.Zoid(p2_data, output_func=self.append_output)
        self.controller = ZoidsGame.GameController(self.player1_zoid, self.player2_zoid, self.battle_type.get(), output_func=self.append_output)
        self.ask_starting_distance()
        self.print_status()
        self.enable_actions()
        self.append_output(f"Game started: {p1_name} vs {p2_name} on {self.battle_type.get().capitalize()}!")

    def ask_starting_distance(self):
        popup = tk.Toplevel(self.root)
        popup.title("Starting Distance")
        tk.Label(popup, text="Enter starting distance between Zoids in meters:").pack(padx=10, pady=10)
        entry_var = tk.StringVar()
        entry = tk.Entry(popup, textvariable=entry_var)
        entry.pack(padx=10, pady=5)
        entry.focus_set()
        def submit():
            try:
                dist = float(entry_var.get())
                if dist >= 0:
                    self.controller.set_starting_distance(dist)
                    popup.destroy()
                else:
                    tk.messagebox.showerror("Invalid Input", "Distance must be non-negative.")
            except ValueError:
                tk.messagebox.showerror("Invalid Input", "Please enter a valid number.")
        tk.Button(popup, text="OK", command=submit).pack(pady=10)
        popup.transient(self.root)
        popup.grab_set()
        self.root.wait_window(popup)

    def print_status(self):
        self.p1_status["text"] = self.format_status(self.player1_zoid)
        self.p2_status["text"] = self.format_status(self.player2_zoid)
        # Show active zoid and distance in status frame
        active_zoid = None
        distance = None
        if hasattr(self, 'controller') and self.controller:
            active_zoid = self.controller.z1 if self.controller.turn % 2 == 0 else self.controller.z2
            distance = self.controller.distance
        status_text = ""
        if active_zoid is not None and distance is not None:
            status_text = f"Active Zoid: {active_zoid.name} | Distance: {distance:.1f} meters"
        else:
            status_text = "Active Zoid: N/A | Distance: N/A"
        # Add or update a label for this info
        if not hasattr(self, 'active_status_label'):
            self.active_status_label = tk.Label(self.status_frame, text=status_text, anchor="w", justify="left", fg="blue")
            self.active_status_label.pack(fill=tk.X)
        else:
            self.active_status_label["text"] = status_text

    def format_status(self, zoid):
        return (f"{zoid.name} | Shield: {'ON' if zoid.shield_on else 'OFF'} | Stealth: {'ON' if zoid.stealth_on else 'OFF'} | Dents: {zoid.dents} | Status: {zoid.status.capitalize()}\n"
                f"Stats: F:{zoid.fighting} S:{zoid.strength} D:{zoid.dexterity} A:{zoid.agility} W:{zoid.awareness} | "
                f"T:{zoid.toughness} P:{zoid.parry} Dg:{zoid.dodge} | L:{zoid.land} W:{zoid.water} A:{zoid.air}\n"
                f"Attacks: Melee:{zoid.melee} Close:{zoid.close_range} Mid:{zoid.mid_range} Long:{zoid.long_range} | Armor:{zoid.armor} | Angle:{zoid.angle}°")

    def enable_actions(self):
        self.move_btn["state"] = tk.NORMAL
        self.attack_btn["state"] = tk.NORMAL
        # Enable shield only if active zoid has a shield
        active_zoid = None
        if hasattr(self, 'controller') and self.controller:
            active_zoid = self.controller.z1 if self.controller.turn % 2 == 0 else self.controller.z2
        if active_zoid and hasattr(active_zoid, 'has_shield') and active_zoid.has_shield():
            self.shield_btn["state"] = tk.NORMAL
        else:
            self.shield_btn["state"] = tk.DISABLED
        self.stealth_btn["state"] = tk.NORMAL
        self.p1_dropdown["state"] = tk.DISABLED
        self.p2_dropdown["state"] = tk.DISABLED
        self.turn_complete_btn["state"] = tk.NORMAL
        self.select_frame.pack_forget()  # Hide the selection frame after starting the game

    def append_output(self, text):
        self.output_box["state"] = tk.NORMAL
        # Determine active zoid
        active_zoid = None
        if hasattr(self, 'controller') and self.controller:
            active_zoid = self.controller.z1 if self.controller.turn % 2 == 0 else self.controller.z2
        if active_zoid:
            self.output_box.insert(tk.END, f"[Active: {active_zoid.name}]\n\n")
        self.output_box.insert(tk.END, text + "\n")
        self.output_box.see(tk.END)
        self.output_box["state"] = tk.DISABLED

    def do_action(self, action):
        if not hasattr(self, 'queued_action'):
            self.reset_action_queue()
        if action == "move":
            self.show_move_popup()
            return
        elif action == "attack":
            self.queued_action['attack'] = "y"
            self.append_output("Attack action queued.")
            self.attack_btn["state"] = tk.DISABLED
        elif action == "shield":
            val = "y" if self.shield_var.get() == "y" else "n"
            self.queued_action['shield_toggle'] = val
            self.append_output(f"Shield toggle queued: {val}")
            self.shield_var.set("n" if self.shield_var.get() == "y" else "y")
        elif action == "stealth":
            val = "y" if self.stealth_var.get() == "y" else "n"
            self.queued_action['stealth_toggle'] = val
            self.append_output(f"Stealth toggle queued: {val}")
            self.stealth_var.set("n" if self.stealth_var.get() == "y" else "y")
        if action == "turn_complete":
            self.controller.do_turn(
                move_attack=self.queued_action['move_attack'],
                move=self.queued_action['move'],
                angle_change=self.queued_action['angle_change'],
                attack=self.queued_action['attack'],
                shield_toggle=self.queued_action['shield_toggle'],
                stealth_toggle=self.queued_action['stealth_toggle']
            )
            self.controller.turn += 1
            self.reset_action_queue()
            self.enable_actions()
            self.print_status()

    def show_move_popup(self):
        popup = tk.Toplevel(self.root)
        popup.title("Choose Movement")
        tk.Label(popup, text="Select your movement:").pack(padx=10, pady=10)
        move_var = tk.StringVar(value="1")
        moves = [
            ("1", "Close"),
            ("2", "Retreat"),
            ("3", "Circle Left"),
            ("4", "Circle Right"),
            ("5", "Stand Still")
        ]
        for val, label in moves:
            tk.Radiobutton(popup, text=label, variable=move_var, value=val).pack(anchor="w", padx=20)
        angle_var = tk.DoubleVar(value=0.0)
        angle_entry = None
        angle_label = None
        def on_move_select(*args):
            move = move_var.get()
            # Remove previous angle entry if present
            nonlocal angle_entry, angle_label
            if angle_entry:
                angle_entry.destroy()
                angle_entry = None
            if angle_label:
                angle_label.destroy()
                angle_label = None
            if move in ("3", "4"):
                # Get active zoid and distance
                active_zoid = self.controller.z1 if self.controller.turn % 2 == 0 else self.controller.z2
                distance = self.controller.distance
                speed = active_zoid.get_speed(self.controller.battle_type)
                # Calculate max allowed angle
                if distance <= 0.1:
                    max_angle = 360
                else:
                    max_angle = min(360, (speed * 180) / (3.14159 * distance))
                angle_label = tk.Label(popup, text=f"Enter angle (deg, max {max_angle:.1f}):")
                angle_label.pack(padx=10, pady=2)
                angle_entry = tk.Entry(popup, textvariable=angle_var, width=6)
                angle_entry.pack(padx=10, pady=2)
                angle_entry.focus_set()
        move_var.trace_add('write', on_move_select)
        def submit():
            move_attack = "m"
            move = move_var.get()
            angle_change = None
            if move in ("3", "4"):
                # Get active zoid and distance
                active_zoid = self.controller.z1 if self.controller.turn % 2 == 0 else self.controller.z2
                distance = self.controller.distance
                speed = active_zoid.get_speed(self.controller.battle_type)
                if distance <= 0.1:
                    max_angle = 360
                else:
                    max_angle = min(360, (speed * 180) / (3.14159 * distance))
                try:
                    angle_val = float(angle_var.get())
                    if 0 <= angle_val <= max_angle:
                        angle_change = angle_val
                    else:
                        tk.messagebox.showerror("Invalid Input", f"Angle must be between 0 and {max_angle:.1f}")
                        return
                except ValueError:
                    tk.messagebox.showerror("Invalid Input", "Please enter a valid number.")
                    return
            popup.destroy()
            self.queued_action['move_attack'] = move_attack
            self.queued_action['move'] = move
            self.queued_action['angle_change'] = angle_change
            self.append_output(f"Move action queued: {move} {'with angle ' + str(angle_change) if angle_change is not None else ''}")
            self.move_btn["state"] = tk.DISABLED
        tk.Button(popup, text="OK", command=submit).pack(pady=10)
        popup.transient(self.root)
        popup.grab_set()
        self.root.wait_window(popup)

def start_GUI():
    root = tk.Tk()
    app = ZoidsGameApp(root)
    root.mainloop()

if __name__ == "__main__":
    root = tk.Tk()
    app = ZoidsGameApp(root)
    root.mainloop()
