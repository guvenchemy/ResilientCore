using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace ResilientArena;

public class ResilientArenaPlugin : BasePlugin
{
    public override string ModuleName => "ResilientArenaPlugin ";
    public override string ModuleVersion => "1.0.0"; // Dynamic Matchmaking
    public override string ModuleAuthor => "Chemist";

    private ArenaManager _arenaManager = null!;
    private LoadoutManager _loadoutManager = new();
    private MatchmakingManager _matchmakingManager = null!;

    // Map ID that i found on workshop
    // You can find it on https://steamcommunity.com/workshop/filedetails/?id=3242420753
    private const string MAP_WORKSHOP_ID = "3242420753";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("[ResilientArena] Dinamik Sistem Baslatiliyor...");

        _arenaManager = new ArenaManager(ModuleDirectory);
        // Matchmaking Manager'ı oluşturup diğer managerları ona veriyoruz
        _matchmakingManager = new MatchmakingManager(_arenaManager, _loadoutManager);

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            _arenaManager.LoadConfig(mapName);
            ApplyServerSettings();
        });

        // 1. Oyuncu Girince -> Sıraya Ekle
        RegisterListener<Listeners.OnClientPutInServer>(slot =>
        {
            _loadoutManager.SetDefault(slot);
            // 2 saniye bekle (tam yüklensin) sonra sıraya al
            AddTimer(2.0f, () => _matchmakingManager.AddPlayer(slot));

            // Hoşgeldin mesajı
            AddTimer(6.0f, () => PrintWelcome(slot));
        });

        // 2. Oyuncu Çıkınca -> Listeden Sil
        RegisterListener<Listeners.OnClientDisconnect>(slot => _matchmakingManager.RemovePlayer(slot));

        AddCommandListener("say", OnClientChat);
        AddCommandListener("say_team", OnClientChat);
        AddCommandListener("drop", OnWeaponDrop);

        if (hotReload)
        {
            _arenaManager.LoadConfig(Server.MapName);
            ApplyServerSettings();
        }

        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
    }

    // --- DİNAMİK ÖLÜM SİSTEMİ ---
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var killer = @event.Attacker;

        if (victim == null || !victim.IsValid) return HookResult.Continue;

        int killerSlot = (killer != null && killer.IsValid) ? killer.Slot : -1;
        _matchmakingManager.HandleDeath(victim.Slot, killerSlot);

        AddTimer(1.5f, () =>
        {
            if (victim.IsValid)
            {
                victim.Respawn();
                // Respawn olduğu an sıraya girer ve boşta olanın yanına ışınlanır
                _matchmakingManager.AddPlayer(victim.Slot);
            }
        });

        return HookResult.Continue;
    }
    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        AddTimer(3.0f, () => { Server.ExecuteCommand($"host_workshop_map {MAP_WORKSHOP_ID}"); });
        return HookResult.Continue;
    }

    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        Server.ExecuteCommand($"host_workshop_map {MAP_WORKSHOP_ID}");
        return HookResult.Handled;
    }

    private void PrintWelcome(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (player != null && player.IsValid && !player.IsBot)
        {
            player.PrintToChat($" {ChatColors.Green}[YC] {ChatColors.White}Hosgeldin {ChatColors.Gold}{player.PlayerName}!");
            player.PrintToChat($" {ChatColors.Green}[YC] {ChatColors.White}Sistem: {ChatColors.Red}Dinamik Eslesme (Kazanan Kalir)");
            player.PrintToChat($" {ChatColors.Green}[YC] {ChatColors.White}Komutlar: {ChatColors.Red}.yardim");
        }
    }

    private void ApplyServerSettings()
    {
        Server.ExecuteCommand("mp_teammates_are_enemies 1");
        Server.ExecuteCommand("mp_solid_teammates 1");
        Server.ExecuteCommand("mp_respawn_on_death_ct 1");
        Server.ExecuteCommand("mp_respawn_on_death_t 1");
        Server.ExecuteCommand("mp_respawn_immunitytime 2");
        Server.ExecuteCommand("mp_death_drop_gun 0");
        Server.ExecuteCommand("mp_free_armor 1");
        Server.ExecuteCommand("mp_roundtime 60");
        Server.ExecuteCommand("mp_maxrounds 0");
        Server.ExecuteCommand("mp_timelimit 0");
        Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
        Server.ExecuteCommand("mp_match_end_restart 1");
        Server.ExecuteCommand("mp_endmatch_votenextmap 0");
        Console.WriteLine("[ResilientArena] Dinamik Ayarlar Yuklendi.");
    }

    private HookResult OnWeaponDrop(CCSPlayerController? player, CommandInfo info) => HookResult.Handled;

    private HookResult OnClientChat(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return HookResult.Continue;
        string msg = info.GetArg(1);
        if (msg.StartsWith(".") || msg.StartsWith("!")) { _loadoutManager.HandleChatCommand(player, msg); return HookResult.Continue; }
        return HookResult.Continue;
    }

    // Management Commands (Arena Add vb.)
    [ConsoleCommand("ra_arena_add")] public void OnAddArena(CCSPlayerController? player, CommandInfo command) { if (player == null || command.ArgCount < 3) return; if (!int.TryParse(command.GetArg(1), out int id)) return; _arenaManager.AddSpawn(player, id, command.GetArg(2).ToLower()); }
    [ConsoleCommand("ra_arena_save")] public void OnSave(CCSPlayerController? player, CommandInfo command) { _arenaManager.SaveConfig(Server.MapName); player?.PrintToChat(" {ChatColors.Green}Kaydedildi!"); }
    [ConsoleCommand("ra_fix_data")] public void OnFixData(CCSPlayerController? player, CommandInfo command) { if (player != null) _arenaManager.FixData(player, Server.MapName); }
    [ConsoleCommand("ra_get_pos")] public void OnGetPos(CCSPlayerController? player, CommandInfo command) { if (player == null || !player.IsValid) return; var pos = player.PlayerPawn.Value?.AbsOrigin; if (pos != null) player.PrintToChat($" {ChatColors.Green}[POS] {pos.X:F2} {pos.Y:F2} {pos.Z:F2}"); }
    [ConsoleCommand("ra_start")] public void OnStart(CCSPlayerController? p, CommandInfo c) { Server.ExecuteCommand("mp_warmup_end"); Server.ExecuteCommand("mp_restartgame 1"); }
    [ConsoleCommand("ra_bot_add")] public void OnBotAdd(CCSPlayerController? p, CommandInfo c) => Server.ExecuteCommand("bot_add");
    [ConsoleCommand("ra_bot_kick")] public void OnBotKick(CCSPlayerController? p, CommandInfo c) => Server.ExecuteCommand("bot_kick");
}