# Yilmaz Hosting Core - Algorithms & Logic Documentation

This document describes the internal logic and algorithms used in the Yilmaz Hosting Core plugin (v8.0.0).

## 1. Matchmaking System (`MatchmakingManager.cs`)

The matchmaking system uses a **Winner Stays On** logic with a dynamic queuing system. It ensures that players are constantly matched without waiting for a round end.

### Core Logic

#### A. Player Queue (`_waitingQueue`)
- **Structure:** A FIFO (First-In-First-Out) queue holding the `slot` indices of players waiting for a match.
- **Logic:**
  - Upon connecting, a player is added to the queue after 2 seconds.
  - Upon losing a duel (death), the victim is respawned and added back to the queue.
  - The queue is processed (`ProcessQueue`) whenever a player joins it or a match ends.

#### B. Arena Management (`_arenaOccupants`)
- **Structure:** A dictionary mapping `ArenaID` to a list of `PlayerSlots` currently in that arena.
- **Logic:**
  - `ProcessQueue` checks if there is any arena with exactly **1 player** (Waiting for opponent).
    - If found, the first player in the queue is sent there immediately.
  - If no arena has a waiter, it checks for a **Empty Arena** (0 players).
    - If found, the first player in the queue is sent there to wait.
  - If all arenas are full (2 players each), the players in the queue continue to wait until a match finishes.

#### C. Death Handling (`HandleDeath`)
When a player dies:
1.  **The Killer (Winner):**
    - Remains in the arena.
    - HP is restored to 100.
    - Effectively becomes a "Waiting Player" in that arena.
2.  **The Victim (Loser):**
    - Is removed from the arena immediately.
    - Respawns after 1.5 seconds.
    - Is added to the back of the `_waitingQueue`.
3.  **The Flow:** The system immediately calls `ProcessQueue` to send a new challenger to the Killer.

---

## 2. Loadout System (`LoadoutManager.cs`)

The loadout system allows players to choose their weapons via chat commands and ensures balanced gameplay.

### Core Logic

#### A. Weapon Selection
- Players use chat commands (e.g., `.ak`, `.awp`, `.deagle`).
- The selection is stored in a dictionary `_playerLoadouts`.
- Default weapon is `weapon_ak47` if no selection is made.

#### B. Distribution Logic (`GiveLoadout`)
Every time a player spawns or enters an arena:
1.  **Strip:** All existing weapons are removed.
2.  **Defaults:** Knife and Armor (Assault Suit) are given.
3.  **Primary:** The selected weapon is given.
4.  **Secondary Rule:**
    - If the selected weapon is a **Rifle/Sniper** (not a pistol), a `Deagle` is automatically given as a secondary.
    - If the selected weapon is a **Pistol**, no secondary is given (to prevent dual pistols).

---

## 3. Arena Configuration (`ArenaManager.cs`)

Manages the spatial data for where players should spawn.

### Core Logic

#### A. JSON Storage
- Data is saved per map in `arenas_<mapname>.json`.
- Structure:
  - `MapName`: Name of the map.
  - `Arenas`: List of arenas, each containing:
    - `ArenaID`: Integer identifier.
    - `T_Spawn`: Tuple of (X, Y, Z, Angle) for Terrorist spawn.
    - `CT_Spawn`: Tuple of (X, Y, Z, Angle) for Counter-Terrorist spawn.

#### B. Data Recovery (`FixData`)
- A specialized algorithm to fix corrupted data where spawned points might be misaligned.
- **Logic:** Iterates through all arenas. If it finds an arena where `T_Spawn` is set but `CT_Spawn` is null, it assumes a user error (saving CT point as T point) and shifts the data to `CT_Spawn`, clearing `T_Spawn` for re-recording.

---

## 4. Main Plugin Lifecycle (`YilmazPlugin.cs`)

The entry point that orchestrates the managers.

- **OnMapStart:** Initializes `ArenaManager` and loads the config. Sets global server cvars (`mp_teammates_are_enemies`, etc.).
- **OnClientPutInServer:** Hands the new player to `LoadoutManager` (defaults) and `MatchmakingManager` (queue).
- **OnPlayerDeath:** Delegates the logic to `MatchmakingManager.HandleDeath`.
- **OnRoundEnd / OnMatchEnd:** Forces a map change to the Workshop map defined in `MAP_WORKSHOP_ID` to ensure the server stays on the correct loop.
