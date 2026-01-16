# Yilmaz Hosting Core (v8.0.0)

**Yazar:** Chemist
**Versiyon:** 8.0.0 (Dynamic Matchmaking & Loadout System)

## Genel Bakış
Yilmaz Hosting Core, Counter-Strike 2 sunucuları için geliştirilmiş, **tamamen dinamik** bir 1v1 Arena ve Deathmatch yönetim sistemidir. v8.0.0 ile birlikte gelen yeni "Dynamic Matchmaking" sistemi, oyuncuları anlık olarak takip eder, oyuna girenleri beklemeden eşleştirir ve ölenleri anında yeni bir rakiple buluşturur.

## Yeni Özellikler (v8.0.0)

### 1. Dinamik Eşleştirme (Dynamic Matchmaking)
- **Bekleme Yok:** Sunucuya giren oyuncu 2 saniye içinde sisteme dahil olur.
- **Kazanan Kalır:** Bir düelloyu kazanan kişi arenada kalır, kaybeden veya yeni giren kişi anında uygun bir arenaya yerleştirilir.
- **Akıllı Yönetim:** `MatchmakingManager` sınıfı, tüm oyuncu durumlarını (Boşta, Savaşta, Ölü) anlık takip eder.

### 2. Loadout & Silah Sistemi
- **Chat Komutları:** Oyuncular `.` veya `!` ile başlayan komutlarla (örn: `.yardim`) silahlarını veya tercihlerini yönetebilir (`LoadoutManager`).
- **Otomatik Dağıtım:** Her yeniden doğuşta veya raund başında oyunculara belirlenen teçhizatlar otomatik verilir.

### 3. Harita Döngüsü
- **Workshop Entegrasyonu:** Raund veya maç bittiğinde sunucu otomatik olarak belirlenen Workshop haritasına (`host_workshop_map`) geçiş yapar.

---

## Yönetici Komutları (Admin Commands)

Aşağıdaki komutlar sunucu yöneticileri içindir:

### `yh_arena_add <no> <takım>`
Doğma noktası ekler.
- `yh_arena_add 1 ct`
- `yh_arena_add 1 t`

### `yh_arena_save`
Arena ayarlarını JSON dosyasına kaydeder.

### `yh_fix_data`
Hatalı kaydedilmiş (T/CT karışmış) verileri onarır.

### `yh_start`
Oyunu ve modu manuel olarak başlatır/resetler.

### `yh_get_pos`
Anlık koordinat bilgisini gösterir.

### Bot Yönetimi
- `yh_bot_add`: Bot ekler.
- `yh_bot_kick`: Botları atar.

---

## Teknik Detaylar (Geliştiriciler İçin)

### Mimari Değişiklikler
v8.0.0 ile birlikte kod yapısı modüler hale getirilmiştir:
- **`YilmazPlugin`**: Ana giriş noktası. Eventleri dinler ve ilgili Manager'a iletir.
- **`ArenaManager`**: Arena koordinatlarını ve dosya işlemlerini yönetir.
- **`MatchmakingManager`**: Oyuncu eşleştirmelerini, arena doluluk oranlarını ve spawn sırasını yönetir.
- **`LoadoutManager`**: Silah, skin ve chat komutlarını işler.

### Yapılandırma
`arenas_<MapName>.json` dosyası, harita bazlı spawn noktalarını tutmaya devam eder.

```json
{
  "MapName": "de_mirage",
  "Arenas": [ { "ArenaID": 1, ... } ]
}
```
