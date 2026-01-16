using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace YilmazHostingCore;

// JSON Config Yapısı
public class ArenaConfig
{
    public string MapName { get; set; } = "";
    public List<ArenaDefinition> Arenas { get; set; } = new();
}

// Tek bir arenanın tanımı
public class ArenaDefinition
{
    public int ArenaId { get; set; }
    public SpawnPoint? T_Spawn { get; set; }
    public SpawnPoint? CT_Spawn { get; set; }
}

// Koordinat Yapısı
public class SpawnPoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Angle { get; set; }

    // Oyuncudan koordinat alma yardımcısı
    public static SpawnPoint FromPlayer(Vector pos, QAngle ang)
    {
        return new SpawnPoint
        {
            X = pos.X,
            Y = pos.Y,
            Z = pos.Z,
            Angle = ang.Y
        };
    }
}