using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace YilmazHostingCore;

public class MatchmakingManager
{
    private readonly ArenaManager _arenaManager;
    private readonly LoadoutManager _loadoutManager;

    // Bekleme Sırası (Maç arayan oyuncular)
    private Queue<int> _waitingQueue = new();

    // Hangi oyuncu hangi arenada? (Slot -> ArenaID)
    private Dictionary<int, int> _playerArenaMap = new();

    // Hangi arenada kimler var? (ArenaID -> Oyuncu Listesi)
    private Dictionary<int, List<int>> _arenaOccupants = new();

    public MatchmakingManager(ArenaManager am, LoadoutManager lm)
    {
        _arenaManager = am;
        _loadoutManager = lm;
    }

    // Oyuncu sunucuya girince
    public void AddPlayer(int playerSlot)
    {
        if (!_waitingQueue.Contains(playerSlot))
        {
            _waitingQueue.Enqueue(playerSlot);
            ProcessQueue(); // Hemen eşleştirmeyi dene
        }
    }

    // Oyuncu çıkınca
    public void RemovePlayer(int playerSlot)
    {
        // 1. Sıradaysa çıkar
        if (_waitingQueue.Contains(playerSlot))
        {
            // Kuyruktan silme işlemi biraz manuel (Queue'da Remove yok)
            var list = _waitingQueue.ToList();
            list.Remove(playerSlot);
            _waitingQueue = new Queue<int>(list);
        }

        // 2. Arenadaysa
        if (_playerArenaMap.TryGetValue(playerSlot, out int arenaId))
        {
            _playerArenaMap.Remove(playerSlot);

            if (_arenaOccupants.ContainsKey(arenaId))
            {
                _arenaOccupants[arenaId].Remove(playerSlot);

                // Eğer içeride rakip tek kaldıysa, ona haber ver
                if (_arenaOccupants[arenaId].Count == 1)
                {
                    int survivor = _arenaOccupants[arenaId][0];
                    var pSurvivor = Utilities.GetPlayerFromSlot(survivor);
                    if (pSurvivor != null && pSurvivor.IsValid)
                    {
                        pSurvivor.PrintToChat($" {ChatColors.Red}[YC] {ChatColors.White}Rakibin kacti! Yeni rakip bekleniyor...");
                        pSurvivor.PlayerPawn.Value!.Health = 100; // Ödül
                    }
                    // Tek kalanı eşleştirmek için sırayı tetikle
                    ProcessQueue();
                }
                else if (_arenaOccupants[arenaId].Count == 0)
                {
                    _arenaOccupants.Remove(arenaId); // Arena tamamen boşaldı
                }
            }
        }
    }

    // Ölüm anı (Asıl olay burada)
    public void HandleDeath(int victimSlot, int killerSlot)
    {
        var victim = Utilities.GetPlayerFromSlot(victimSlot);
        var killer = Utilities.GetPlayerFromSlot(killerSlot);

        // 1. Öldüren (Kazanan): Arenada kalır, canı fullenir, beklemeye geçer
        if (killer != null && killer.IsValid && killer.PawnIsAlive)
        {
            killer.PlayerPawn.Value!.Health = 100;
            // Killer zaten arenada, yerini koruyor. Sadece durumu "Bekliyor" gibi işlem görüyor.
            // Onu yerinden oynatmıyoruz, rakip ona gelecek.
        }

        // 2. Ölen (Kaybeden): Arenadan atılır -> Sıraya girer -> Yeni arena arar
        if (victim != null && victim.IsValid)
        {
            // Mevcut arenadan kaydını sil
            if (_playerArenaMap.TryGetValue(victimSlot, out int arenaId))
            {
                _playerArenaMap.Remove(victimSlot);
                if (_arenaOccupants.ContainsKey(arenaId))
                    _arenaOccupants[arenaId].Remove(victimSlot);
            }

            // Kuyruğa ekle ama hemen değil (Spawn süresi kadar beklet)
            // Plugin tarafında Timer ile respawn olunca AddPlayer çağıracağız.
        }

        // Ölen çıktığı için killer tek kaldı, ProcessQueue çağırarak sıradakini killer'ın yanına yolla
        ProcessQueue();
    }

    // Kuyruktaki oyuncuları boş yerlere dağıt
    public void ProcessQueue()
    {
        if (_arenaManager.ActiveConfig == null) return;

        // Eşleşme döngüsü
        while (_waitingQueue.Count > 0)
        {
            // 1. Sırada bekleyen oyuncuyu al
            int playerSlot = _waitingQueue.Peek();
            var player = Utilities.GetPlayerFromSlot(playerSlot);

            // Oyuncu geçersizse kuyruktan at ve devam et
            if (player == null || !player.IsValid || player.IsBot) // Botları şimdilik sıraya sokma
            {
                _waitingQueue.Dequeue();
                continue;
            }

            // 2. HEDEF ARENA BUL

            // Öncelik A: İçinde TEK kişi bekleyen bir arena var mı? (Hemen kapışma)
            var waitingArena = _arenaOccupants.FirstOrDefault(x => x.Value.Count == 1);

            // Öncelik B: Tamamen BOŞ bir arena var mı?
            var emptyArenaDef = _arenaManager.ActiveConfig.Arenas
                .FirstOrDefault(a => !_arenaOccupants.ContainsKey(a.ArenaId) || _arenaOccupants[a.ArenaId].Count == 0);

            if (waitingArena.Key != 0) // Birisi rakip bekliyor!
            {
                _waitingQueue.Dequeue(); // Kuyruktan çıkar
                JoinArena(player, waitingArena.Key, isSecondPlayer: true);
            }
            else if (emptyArenaDef != null) // Boş arena var
            {
                _waitingQueue.Dequeue(); // Kuyruktan çıkar
                JoinArena(player, emptyArenaDef.ArenaId, isSecondPlayer: false);
            }
            else
            {
                // Ne boş yer var ne de bekleyen biri.
                // Oyuncu kuyrukta kalmaya devam eder.
                player.PrintToChat($" {ChatColors.Yellow}[YC] {ChatColors.White}Tum arenalar dolu! Sira bekleniyor...");
                break; // Döngüyü kır, yer açılınca tekrar çalışır
            }
        }
    }

    private void JoinArena(CCSPlayerController player, int arenaId, bool isSecondPlayer)
    {
        var arenaDef = _arenaManager.ActiveConfig!.Arenas.FirstOrDefault(a => a.ArenaId == arenaId);
        if (arenaDef == null) return;

        // Kayıtları güncelle
        _playerArenaMap[player.Slot] = arenaId;
        if (!_arenaOccupants.ContainsKey(arenaId)) _arenaOccupants[arenaId] = new List<int>();
        _arenaOccupants[arenaId].Add(player.Slot);

        // Koordinat belirle (İlk giren T, ikinci giren CT olsun veya tam tersi)
        // Eğer ikinci oyuncuysak, içeridekinin zıttına koymalıyız ama basitçe:
        // İçerideki T ise biz CT, içerideki CT ise biz T.
        SpawnPoint? targetSpawn;

        if (isSecondPlayer)
        {
            // Rakibi bul
            int opponentSlot = _arenaOccupants[arenaId].FirstOrDefault(s => s != player.Slot);
            var opponent = Utilities.GetPlayerFromSlot(opponentSlot);

            // Rakip T spawnına yakınsa biz CT'ye geçelim (Basit mantık: CT spawnı ver)
            // Daha sağlamı: Arena doluysa direkt CT verelim, boşsa T verelim.
            targetSpawn = arenaDef.CT_Spawn;

            if (opponent != null)
            {
                player.PrintToChat($" {ChatColors.Green}[VS] {ChatColors.Red}{opponent.PlayerName} {ChatColors.White}ile eslesildi!");
                opponent.PrintToChat($" {ChatColors.Green}[VS] {ChatColors.Blue}{player.PlayerName} {ChatColors.White}geldi! Hazir ol!");
            }
        }
        else
        {
            // İlk giren biziz, T spawnına geçip bekleyelim
            targetSpawn = arenaDef.T_Spawn;
            player.PrintToChat($" {ChatColors.Green}[Arena {arenaId}] {ChatColors.White}Rakip bekleniyor...");
        }

        // Işınla ve Silah Ver
        Teleport(player, targetSpawn);
    }

    private void Teleport(CCSPlayerController player, SpawnPoint? point)
    {
        if (player.PlayerPawn.Value == null || point == null) return;
        player.PlayerPawn.Value.Velocity.X = 0; player.PlayerPawn.Value.Velocity.Y = 0; player.PlayerPawn.Value.Velocity.Z = 0;
        player.PlayerPawn.Value.Health = 100;
        player.PlayerPawn.Value.Teleport(new Vector(point.X, point.Y, point.Z), new QAngle(0, point.Angle, 0), new Vector(0, 0, 0));
        _loadoutManager.GiveLoadout(player);
    }
}