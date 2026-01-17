using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ResilientArena;

public class ArenaManager
{
    public ArenaConfig? ActiveConfig { get; private set; }
    private ArenaConfig _tempConfig = new();
    private string _moduleDirectory;

    public ArenaManager(string moduleDirectory)
    {
        _moduleDirectory = moduleDirectory;
    }

    // --- YÜKLEME & KAYDETME ---
    public void LoadConfig(string mapName)
    {
        string path = Path.Combine(_moduleDirectory, $"arenas_{mapName}.json");
        if (File.Exists(path))
        {
            try
            {
                ActiveConfig = JsonSerializer.Deserialize<ArenaConfig>(File.ReadAllText(path));
                // Temp configi de güncelle ki üzerine ekleme yapabilelim
                if (ActiveConfig != null) _tempConfig = ActiveConfig;
            }
            catch { Console.WriteLine("[ArenaManager] JSON Hatasi!"); }
        }
        else
        {
            ActiveConfig = null;
            // Config yoksa temp'i sıfırla ama Map ismini ver
            _tempConfig = new ArenaConfig { MapName = mapName };
        }
    }

    public void SaveConfig(string mapName)
    {
        string path = Path.Combine(_moduleDirectory, $"arenas_{mapName}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(_tempConfig, options));

        // Kaydettikten sonra belleği de tazele
        LoadConfig(mapName);
    }

    // --- ARENA EKLEME ---
    public void AddSpawn(CCSPlayerController player, int arenaId, string teamArg)
    {
        var pos = player.PlayerPawn.Value?.AbsOrigin;
        var ang = player.PlayerPawn.Value?.EyeAngles;
        if (pos == null || ang == null) return;

        // Config map ismi boşsa doldur
        if (string.IsNullOrEmpty(_tempConfig.MapName)) _tempConfig.MapName = Server.MapName;

        // Arena var mı bak, yoksa oluştur
        var arena = _tempConfig.Arenas.FirstOrDefault(a => a.ArenaId == arenaId);
        if (arena == null)
        {
            arena = new ArenaDefinition { ArenaId = arenaId };
            _tempConfig.Arenas.Add(arena);
        }

        // T/CT Mantığı (Senin düzelttiğin hatasız hali)
        if (teamArg == "ct" || teamArg == "counter-terrorist")
        {
            arena.CT_Spawn = SpawnPoint.FromPlayer(pos, ang);
            player.PrintToChat($" {ChatColors.Blue}Arena {arenaId} [CT] noktasi kaydedildi!");
        }
        else if (teamArg == "t" || teamArg == "terrorist")
        {
            arena.T_Spawn = SpawnPoint.FromPlayer(pos, ang);
            player.PrintToChat($" {ChatColors.Red}Arena {arenaId} [T] noktasi kaydedildi!");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Yellow}Hata: Takim olarak sadece 't' veya 'ct' yazmalisin.");
        }
    }

    // --- KURTARMA OPERASYONU (Fix Data) ---
    public void FixData(CCSPlayerController player, string mapName)
    {
        string path = Path.Combine(_moduleDirectory, $"arenas_{mapName}.json");
        if (!File.Exists(path)) { player.PrintToChat("Dosya yok!"); return; }

        try
        {
            var config = JsonSerializer.Deserialize<ArenaConfig>(File.ReadAllText(path));
            if (config == null) return;

            int duzeltilen = 0;
            foreach (var arena in config.Arenas)
            {
                // T dolu, CT boş ise -> Kaydır
                if (arena.T_Spawn != null && arena.CT_Spawn == null)
                {
                    arena.CT_Spawn = arena.T_Spawn;
                    arena.T_Spawn = null;
                    duzeltilen++;
                }
            }

            // Kaydet ve Yükle
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(config, options));

            _tempConfig = config;
            LoadConfig(mapName);

            player.PrintToChat($" {ChatColors.Green}KURTARMA BASARILI: {duzeltilen} adet arena duzeltildi!");
        }
        catch (Exception ex) { player.PrintToChat($"Hata: {ex.Message}"); }
    }
}