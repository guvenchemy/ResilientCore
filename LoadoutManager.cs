using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ResilientArena;

public class LoadoutManager
{
    private Dictionary<int, string> _playerLoadouts = new();


    private readonly HashSet<string> _pistols = new()
    {
        "weapon_deagle", "weapon_usp_silencer", "weapon_glock",
        "weapon_p250", "weapon_fiveseven", "weapon_tec9", "weapon_cz75a"
    };

    public void SetDefault(int slot)
    {
        if (!_playerLoadouts.ContainsKey(slot))
            _playerLoadouts[slot] = "weapon_ak47";
    }

    public void HandleChatCommand(CCSPlayerController player, string message)
    {
        string command = message.Substring(1).ToLower().Trim();
        string newWeapon = "";
        string weaponName = "";

        switch (command)
        {

            case "ak47":
            case "ak":
                newWeapon = "weapon_ak47"; weaponName = "AK-47"; break;
            case "m4a1":
            case "m4a1s":
            case "silencer":
                newWeapon = "weapon_m4a1_silencer"; weaponName = "M4A1-S"; break;
            case "m4":
            case "m4a4":
                newWeapon = "weapon_m4a1"; weaponName = "M4A4"; break;
            case "awp":
                newWeapon = "weapon_awp"; weaponName = "AWP"; break;
            case "scout":
            case "ssg":
            case "ssg08":
                newWeapon = "weapon_ssg08"; weaponName = "Scout (SSG)"; break;
            case "famas":
                newWeapon = "weapon_famas"; weaponName = "FAMAS"; break;
            case "galil":
            case "galilar":
                newWeapon = "weapon_galilar"; weaponName = "Galil AR"; break;
            case "aug":
                newWeapon = "weapon_aug"; weaponName = "AUG"; break;
            case "sg":
            case "sg553":
                newWeapon = "weapon_sg553"; weaponName = "SG 553"; break;


            case "deagle":
            case "deag":
                newWeapon = "weapon_deagle"; weaponName = "Deagle"; break;
            case "usp":
            case "usps":
                newWeapon = "weapon_usp_silencer"; weaponName = "USP-S"; break;
            case "glock":
                newWeapon = "weapon_glock"; weaponName = "Glock-18"; break;
            case "p250":
                newWeapon = "weapon_p250"; weaponName = "P250"; break;
            case "five":
            case "fiveseven":
                newWeapon = "weapon_fiveseven"; weaponName = "Five-Seven"; break;
            case "tec":
            case "tec9":
                newWeapon = "weapon_tec9"; weaponName = "Tec-9"; break;


            case "yardim":
            case "help":
            case "komutlar":
            case "silahlar":
                PrintHelp(player);
                return;

            default: return;
        }

        _playerLoadouts[player.Slot] = newWeapon;
        player.PrintToChat($" {ChatColors.Green}[YC] {ChatColors.White}Silah: {ChatColors.Gold}{weaponName} {ChatColors.White}olarak güncellendi.");

        if (player.PawnIsAlive) GiveLoadout(player);
    }

    public void GiveLoadout(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null) return;

        // 1. Önceki silahları temizle
        player.RemoveWeapons();

        // 2. Standart ekipman
        player.GiveNamedItem("weapon_knife");
        player.GiveNamedItem("item_assaultsuit"); // Zırh + Kask

        // 3. Seçilen silahı belirle
        string selectedWeapon = "weapon_ak47"; // Varsayılan
        if (_playerLoadouts.TryGetValue(player.Slot, out string? weapon))
        {
            selectedWeapon = weapon;
        }

        // 4. Silahı ver
        player.GiveNamedItem(selectedWeapon);

        // 5. AKILLI MANTIK: 
        // Eğer seçtiği silah bir tabanca DEĞİLSE (yani tüfekse), yanına Deagle ver.
        // Eğer zaten Deagle veya USP seçtiyse, ikinci bir tabanca verme.
        if (!_pistols.Contains(selectedWeapon))
        {
            player.GiveNamedItem("weapon_deagle");
        }
    }

    public void PrintHelp(CCSPlayerController player)
    {
        player.PrintToChat($" {ChatColors.Green}--- Yilmaz Hosting Silah Menusu ---");
        player.PrintToChat($" {ChatColors.Gold}.ak .m4 .m4a1 .awp .scout {ChatColors.White}-> Tufekler");
        player.PrintToChat($" {ChatColors.Gold}.deagle .usp .glock .p250 {ChatColors.White}-> Tabancalar");
        player.PrintToChat($" {ChatColors.Gold}.famas .galil .aug .sg  {ChatColors.White}-> Digerleri");
        player.PrintToChat($" {ChatColors.LightBlue}Not: Tufek alirsan yanina Deagle gelir.");
        player.PrintToChat($" {ChatColors.LightBlue}Tabanca alirsan sadece o gelir.");
    }
}