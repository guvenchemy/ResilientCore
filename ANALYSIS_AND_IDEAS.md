# Yilmaz Hosting Core - Analysis, Edge Cases & Roadmap

This document outlines scenarios that are currently unhandled, potential bugs identified in the logic, and ideas for future improvements.

## 1. Unhandled Edge Cases & Potential Bugs

### A. Team Switching (Spectators)
- **Issue:** The current system tracks players by `Slot` when they connect (`OnClientPutInServer`). It does NOT listen for team change events.
- **Scenario:** If a player joins the server and immediately switches to **Spectator**, they remain in the `_waitingQueue`.
- **Consequence:** `ProcessQueue` might try to teleport a Spectator into an arena. Since Spectators don't have a valid `PlayerPawn` or physical body in the same way, `Teleport` might fail or behave weirdly.
- **Fix:** Listen to `EventPlayerTeam` and remove players from the queue/areas if they switch to Spectator.

### B. Suicide / World Damage
- **Issue:** `HandleDeath` relies on `killer` being valid to reward the winner.
- **Scenario:** Player A fights Player B. Player A accidentally falls or throws a grenade at themselves (Suicide).
- **Consequence:** Player A dies and is removed. Player B (the survivor) is **NOT healed** because they are not the `Attacker`. Player B remains in the arena with low HP to fight the next challenger.
- **Fix:** In `HandleDeath`, if `killer` is invalid or same as `victim`, find the *other* player in that specific arena and treat them as the winner (Heal them).

### C. Bot Handling
- **Issue:** Code explicitly ignores bots in `ProcessQueue` (`player.IsBot` check).
- **Scenario:** Server is empty, a player adds bots (`yh_bot_add`).
- **Consequence:** Bots will just stand in spawn or wander aimlessly. They will NOT be matched into arenas. The real player will wait in the queue forever if there are no other humans.
- **Improvement:** Allow bots to enter the queue if the player count is low, or create a specific "Play vs Bot" logic.

### D. Missing Config / Empty Map
- **Issue:** If `arenas_<map>.json` is missing or has 0 arenas.
- **Scenario:** `ProcessQueue` checks `ActiveConfig == null` and returns.
- **Consequence:** Players stack up in the queue with no feedback. They just sit in spawn.
- **Fix:** Check for config validity on connection and warn admins or print a message to chat "Arena config not found, please notify admin".

---

## 2. Technical Improvements (Refactoring)

### A. Race Conditions with Disconnects
- **Current:** `OnClientDisconnect` removes the player. `HandleDeath` uses a 1.5s Timer.
- **Risk:** If a player dies and disconnects *immediately* (rage quit), the `RemovePlayer` might run before the Timer. The Timer callback checks `victim.IsValid`, so it should be safe, but logic for the opponent "waiting" might get desynchronized if not careful.

### B. Persistence
- **Current:** `_playerLoadouts` is in-memory.
- **Improvement:** Save player preferences (Skin, Weapon choice) to a database or JSON file so they remember their loadout on next map/reconnect.

---

## 3. New Feature Ideas & Roadmap

### A. ELO / Skill Based Matchmaking
- **Idea:** Instead of "First Come First Serve" in the queue, sort the `_waitingQueue` based on a hidden skill rating.
- **Benefit:** Better matches, newbies don't get stomped by pros repeatedly.

### B. Arena Preferences (Biome/Type)
- **Idea:** If arenas have different designs (Long Range vs Close Quarters), allow players to set a preference.
- **Implementation:** `yh_pref long` or `yh_pref close`. `ProcessQueue` tries to match preference with Arena ID tags.

### C. "King of the Hill" Visuals
- **Idea:** Show a HUD message (HTML Center Print) for the player who has the highest win streak in the arena.
- **Implementation:** Track `WinStreak` in `MatchmakingManager`.

### D. 2v2 Mode
- **Idea:** Allow arenas to handle 2v2 duels.
- **Implementation:** Update `ArenaDefinition` to have `T_Spawn_2` and `CT_Spawn_2`. Update `MatchmakingManager` to group queue by 2.

### E. Challenge Mode
- **Idea:** Allow a player to specifically challenge another player.
- **Command:** `.wager <player_name>` or `.vs <player_name>`.
- **Logic:** Pull both specifically from queue and put them in a reserved arena.
