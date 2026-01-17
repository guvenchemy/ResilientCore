# Resilient Arena (v1.0.0)

**Author:** Chemist
**Version:** 1.0.0 (Dynamic Matchmaking & Loadout System)
**Framework:** [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)

## Overview
**Resilient Arena** is a comprehensive 1v1 Arena and Deathmatch management system developed for **Counter-Strike 2** servers using **CounterStrikeSharp**. This plugin completely transforms your server into a dynamic 1v1 arena experience. It handles everything: automatic matchmaking, infinite round loops, weapon loadouts, and spatial management of multiple arenas within a single map.

## Getting Started / Tutorial

### Prerequisites
1.  Install Metamod:Source for CS2.
2.  Install **CounterStrikeSharp** (latest version).
3.  Place the `ResilientArena` plugin binaries into `game/csgo/addons/counterstrikesharp/plugins/ResilientArena`.

### Step-by-Step Setup
1.  **Launch Server:** Start your CS2 server with the map you want to configure (e.g., `de_mirage`).
2.  **Join Server:** Connect to your server as an administrator.
3.  **Locate Arena 1:** Go to the location where you want the first duel to happen.
4.  **Set Spawn Points:**
    *   Stand where you want Team A (CT) to spawn and type: `ra_arena_add 1 ct`
    *   Stand where you want Team B (T) to spawn and type: `ra_arena_add 1 t`
5.  **Create More Arenas:** Move to a new location for Arena 2 and repeat:
    *   `ra_arena_add 2 ct`
    *   `ra_arena_add 2 t`
6.  **Save Configuration:** Once you have created all your arenas, type: `ra_arena_save`
    *   *This is critical! If you don't save, all points will be lost on restart.*
7.  **Restart:** Type `ra_start` or restart the server to begin the matchmaking loop.

---

## Admin Commands Explained

### `ra_arena_add <no> <team>`
**What does it do?**
It records your player's current X, Y, Z coordinates and looking angle as a spawn point for a specific "Arena" and "Team".

**Why do we need it?**
CS2 maps usually have standard spawn points (T Spawn / CT Spawn). However, for a 1v1 Multi-Arena mode, we need custom spawn points scattered across the map (e.g., one pair in A site, one pair in B site, one pair in Mid). This command allows you to define these custom duel locations manually without editing the map file itself.

**How to use:**
- `ra_arena_add 1 ct`: Sets the Counter-Terrorist spawn for Arena #1 at your feet.
- `ra_arena_add 1 t`: Sets the Terrorist spawn for Arena #1 at your feet.

### `ra_arena_save`
**What does it do?**
Writes all the points you added using `ra_arena_add` into a JSON file (e.g., `arenas_de_mirage.json`).

**Why do we need it?**
The plugin keeps your added points in temporary memory (RAM). If the server crashes or changes level, they are gone. This command persists them to disk so they are loaded automatically next time.

### `ra_fix_data`
**What does it do?**
Scans the configuration for common mistakes, specifically where a user might have saved a T spawn but forgot the CT spawn, or saved data into the wrong "slot". It tries to auto-correct standard human errors.

### `ra_start`
**What does it do?**
Forces the Matchmaking Manager to reset and start the game loop immediately. Useful if the game state gets stuck or you just finished setting up arenas.

---

## Key Features

### 1. Dynamic Matchmaking
- **No Waiting:** Players joining the server are included in the system within 2 seconds.
- **Winner Stays On:** The winner of a duel stays in the arena, while the loser or a newly joined player is immediately placed in an available arena.
- **Smart Management:** The `MatchmakingManager` class instantly tracks all player states (Idle, Fighting, Dead).

### 2. Loadout & Weapon System
- **Chat Commands:** Players can manage their weapons or preferences using commands starting with `.` or `!` (e.g., `.help`) via the `LoadoutManager`.
- **Automatic Distribution:** Equipment is automatically given to players upon every respawn or round start.

### 3. Map Rotation
- **Workshop Integration:** When a round or match ends, the server automatically switches to the specified Workshop map (`host_workshop_map`).

## Acknowledgments
This project is built upon the powerful **CounterStrikeSharp** framework, which allows C# development for Counter-Strike 2.
