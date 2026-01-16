using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;

namespace YilmazHostingCore;

public class OneVsOneMode
{
    private readonly YilmazPlugin _plugin;
    private readonly ArenaManager _arenaManager;
    private readonly LoadoutManager _loadoutManager;
    private readonly MatchmakingManager _matchmakingManager;

    // Harita ID
    private const string MAP_WORKSHOP_ID = "3242420753";

    public OneVsOneMode(YilmazPlugin plugin)
    {
        _plugin = plugin;
        _arenaManager = new ArenaManager(plugin.ModuleDirectory);
        _loadoutManager = new LoadoutManager();
        _matchmakingManager = new MatchmakingManager(_arenaManager, _loadoutManager);
    }

    public void Initialize()
    {
        Console.WriteLine("[YilmazCore] 1v1 Modu Yukleniyor...");

        // Eventleri plugin üzerinden kaydet
        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        _plugin.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);

        _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        _plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        _plugin.RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);

        // Komutları Dinle
        _plugin.AddCommandListener("say", OnClientChat);
        _plugin.AddCommandListener("say_team", OnClientChat);
        _plugin.AddCommandListener("drop", OnWeaponDrop);

        // Yönetim Komutları (Sadece 1v1'e özel olanlar)
        _plugin.AddCommand("yh_arena_add", "Arena Ekle", OnAddArena);
        _plugin.AddCommand("yh_arena_save", "Kaydet", OnSave);
        _plugin.AddCommand("yh_fix_data", "Fix", OnFixData);
        _plugin.AddCommand("yh_get_pos", "Pos", OnGetPos);

        // Eğer reload atıldıysa hemen yapılandırmayı yükle
        _arenaManager.LoadConfig(Server.MapName);
        ApplyServerSettings();
    }

    private void OnMapStart(string mapName)
    {
        _arenaManager.LoadConfig(mapName);
        ApplyServerSettings();
    }

    private void OnClientPutInServer(int slot)
    {
        _loadoutManager.SetDefault(slot);
        _plugin.AddTimer(2.0f, () => _matchmakingManager.AddPlayer(slot));
        _plugin.AddTimer(6.0f, () => PrintWelcome(slot));
    }

    private void OnClientDisconnect(int slot)
    {
        _matchmakingManager.RemovePlayer(slot);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var killer = @event.Attacker;

        if (victim == null || !victim.IsValid) return HookResult.Continue;

        int killerSlot = (killer != null && killer.IsValid) ? killer.Slot : -1;

        // Matchmaking mantığını çalıştır
        _matchmakingManager.HandleDeath(victim.Slot, killerSlot);

        // Öleni canlandır ve sıraya sok
        _plugin.AddTimer(1.5f, () =>
        {
            if (victim.IsValid)
            {
                victim.Respawn();
                _matchmakingManager.AddPlayer(victim.Slot);
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        _plugin.AddTimer(3.0f, () => { Server.ExecuteCommand($"host_workshop_map {MAP_WORKSHOP_ID}"); });
        return HookResult.Continue;
    }

    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        Server.ExecuteCommand($"host_workshop_map {MAP_WORKSHOP_ID}");
        return HookResult.Handled;
    }

    private HookResult OnClientChat(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return HookResult.Continue;
        string msg = info.GetArg(1);
        if (msg.StartsWith(".") || msg.StartsWith("!"))
        {
            _loadoutManager.HandleChatCommand(player, msg);
            return HookResult.Continue;
        }
        return HookResult.Continue;
    }

    private HookResult OnWeaponDrop(CCSPlayerController? player, CommandInfo info) => HookResult.Handled;

    private void ApplyServerSettings()
    {
        // 1v1 Ayarları
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
    }

    private void PrintWelcome(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (player != null && player.IsValid && !player.IsBot)
        {
            player.PrintToChat($" {ChatColors.Green}[YC] {ChatColors.White}Hosgeldin {ChatColors.Gold}{player.PlayerName}!");
            player.PrintToChat($" {ChatColors.Green}[YC] {ChatColors.White}Mod: {ChatColors.Red}1v1 Arena (Dinamik)");
            player.PrintToChat($" {ChatColors.Green}[YC] {ChatColors.White}Komutlar: {ChatColors.Red}.yardim");
        }
    }

    // Komut Yönlendirmeleri
    private void OnAddArena(CCSPlayerController? p, CommandInfo c) { if (p != null && c.ArgCount >= 3 && int.TryParse(c.GetArg(1), out int id)) _arenaManager.AddSpawn(p, id, c.GetArg(2).ToLower()); }
    private void OnSave(CCSPlayerController? p, CommandInfo c) { _arenaManager.SaveConfig(Server.MapName); p?.PrintToChat(" {ChatColors.Green}Kaydedildi!"); }
    private void OnFixData(CCSPlayerController? p, CommandInfo c) { if (p != null) _arenaManager.FixData(p, Server.MapName); }
    private void OnGetPos(CCSPlayerController? p, CommandInfo c)
    {
        var pos = p?.PlayerPawn.Value?.AbsOrigin;
        if (pos != null) p!.PrintToChat($" {ChatColors.Green}[POS] {pos.X:F2} {pos.Y:F2} {pos.Z:F2}");
    }
}