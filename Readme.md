# Game Match3 - README (cap nhat)

## Tong quan
Game ket hop Match-3 + Pet Battle. Data duoc quy ve 1 diem truy xuat de giam sai lech va loi load asset.

## Data hub moi
### `GameDataManager`
File: `Assets/Game/Scripts/GameDataManager.cs`

- Singleton, `DontDestroyOnLoad`
- Quan ly tap trung:
  - `PetDatabase`
  - `CardDatabase`
  - `GemCollection`
  - `Match3Resource` (Match3SpriteResource)
  - `PetResource`
- Script khac nen truy xuat data qua manager nay.
- Ho tro lay stat pet theo level (`TryGetPetStatSnapshot`) + max level (`GetPetMaxLevel`).

### `PetResource`
File: `Assets/Game/Scripts/Resource/PetResource.cs`

Asset map prefab pet de resolve nhanh:
- lookup theo `petId`
- lookup theo `prefabName`

### `SkillDatabase`
Files:
- `Assets/Game/Scripts/Battle/Skills/Data/SkillDatabase.cs`
- `Assets/Game/Data/Skills/SkillDatabase.asset`

`GameDataManager` da doi sang model 1 field `SkillDatabase`.
Tat ca truy xuat skill theo `skillId` se di qua:
- `GameDataManager.GetSkillData(skillId)`
- `SkillDatabase.GetSkillById(skillId)`

## Pet Configuration (PetLevelData) - Thiết lập Pet mới

File: `Assets/Game/Scripts/PetStatsHolder.cs`

Mỗi `PetLevelData` hiện hỗ trợ 3 trường cấu hình mới để kiểm soát hành vi pet:

### 1. Pet Scale (`petScale`)
- **Giá trị mặc định**: `1.0` (kích thước bình thường)
- **Mục đích**: Điều chỉnh kích thước pet khi spawn vào trận đấu
- **Ví dụ**: `0.6` để giảm size pet xuống 60% kích thước ban đầu
- **Nơi áp dụng**: `BattleSceneLoader.ApplyPetScale()` tự động áp dụng scale cho cả player pet và enemy pet
- **Lưu ý**: 
  - Enemy pet giữ nguyên phép lật X (-1) nhưng nhân thêm scale
  - Scale được clamp tối thiểu `0.1` để tránh quá nhỏ

### 2. Bullet Prefab (`bulletPrefab`)
- **Mục đích**: Định nghĩa loại đạn bay ra cho Range Attack của mỗi pet
- **Cách setup**:
  1. Tạo/chuẩn bị prefab viên đạn (có thể là simple sphere, custom model, particle effect)
  2. Gắn prefab vào field `bulletPrefab` của level data trong PetStatsHolder inspector
  3. Khi pet dùng Range attack, sẽ spawn viên đạn này
- **Luồng thực thi**:
  - `PlayerStats`/`AIStats` load `bulletPrefab` từ `PetLevelData`
  - Ghi vào `SkillContext.ProjectilePrefabOverride`
  - `ProjectileSkill.SpawnProjectileHitAsync()` ưu tiên dùng `ProjectilePrefabOverride`, fallback sang skill's projectile
  - Viên đạn tự động di chuyển từ spawn point đến target rồi biến mất sau khi va chạm

### 3. Melee Attack Move (`meleeAttackMoveX`)
- **Giá trị mặc định**: 
  - `-1.2` cho player pet (di chuyển phải để gần enemy)
  - `1.2` cho AI pet (di chuyển trái để gần player)
- **Mục đích**: Tùy chỉnh vị trí dừng của pet khi tấn công melee
- **Cách setup**:
  1. Điều chỉnh giá trị `meleeAttackMoveX` trong level data inspector
  2. Giá trị âm = di chuyển sang trái; dương = sang phải (relative tới target)
- **Luồng thực thi**:
  - `PlayerStats`/`AIStats` load `meleeAttackMoveX` từ `PetLevelData`
  - Ghi vào `SkillContext.MeleeAttackMoveX`
  - `MeleeSkill.ExecuteAsync()` sử dụng giá trị này thay vì hardcode `-1.2` / `1.2`
  - Pet di chuyển đến vị trí: `target.position + (meleeAttackMoveX, 0, 0)`

## Luong Battle Spawn (Update Pet Initialization)

Khi trận đấu bắt đầu:
1. `BattleSceneLoader.SpawnSelectedPets()` spawn player và enemy pet
2. Tự động gọi `ApplyPetScale()` để áp dụng `petScale` từ level data
3. `PlayerStats`/`AIStats` init → `LoadPetData()` → load tất cả config từ `PetLevelData` (bao gồm bulletPrefab, meleeAttackMoveX)
4. Khi attack, `BuildSkillContext()` đưa config vào SkillContext
5. Skill executor (MeleeSkill / ProjectileSkill) dùng config này để thực thi

## Luong Popup Select Pet
- `PopupSelectPet` tu load danh sach pet so huu tu `PlayerManager`.
- Render item bang `PetItemButton`.
- Chon pet tra ve `petId` qua callback.
- Popup dong/mo bang slide position (khong tat gameobject).

## Luong Popup Training
- `ChoosePet` nhan `petId` tu popup select.
- Resolve prefab qua `GameDataManager`.
- Lay stat current/next level tu `GameDataManager` (khong phu thuoc `PetStatsHolder`).
- Neu chua co pet hop le: chi so current/next dat mac dinh `0`.

## Luong Gem Upgrade (cap nhat)
- `GemUpdate` co 3 slot gem (3 button slot).
- Click slot se mo popup gem inventory theo element cua pet dang chon.
- Popup gem chi hien gem user dang so huu (`ownedGems`), sort theo level tang dan.
- Click item gem trong popup -> auto fill vao slot dang chon.
- Text ti le upgrade tinh theo `PetUpgradeService` + `UpgradeConfig`.
- Sau khi upgrade, UI gem reload lai de dong bo logic tru gem.

### Balance Service
File: `Assets/Game/Scripts/Gem/PetUpgradeService.cs`
- Base rate theo do lech `gemLevel - petLevel`
- Bonus du 3 gem / cung level
- Penalty khi dung gem yeu
- Clamp trong `[5%, 90%]`

## Shop + Match3
- `ShopManager` uu tien lay `CardDatabase`/`GemCollection` tu `GameDataManager`.
- `Board` uu tien lay `Match3Resource` tu `GameDataManager` neu chua gan tay.

## Luu y Inspector
Can tao 1 object `GameDataManager` trong bootstrap scene va gan du:
- `PetDatabase`
- `CardDatabase`
- `GemCollection`
- `Match3Resource`
- `PetResource` (map prefab)
- `SkillDatabase` (`Assets/Game/Data/Skills/SkillDatabase.asset`)

## Setup nhanh Skill UI/Inspector (ngan gon)
1. Trong Project, mo `Assets/Game/Data/Skills/SkillDatabase.asset`.
2. Kiem tra list `skills` co item mau `Skill_1001_BasicSlash`.
3. Chon object chua `GameDataManager` trong bootstrap scene, keo `SkillDatabase.asset` vao field `SkillDatabase`.
4. Chon prefab/player enemy co `PetStatsHolder`, gan `levels[i].skillId` = `1001` (hoac skill id ban muon).
5. Chon `PlayerStats` va `AIStats`, dam bao da gan:
  - `damagePopupPrefab`
  - `projectileSpawnPoint` (neu skill range/projectile)
6. Neu import tu Google Sheet: mo asset `SkillDataImporter`, gan `targetSkillDatabase`, bam `Import Skills from Google Sheet`.

## Cap nhat luong Prebattle -> Battle
- `PrebattlePopupUI` luu tam chon map/pet/card vao `PrebattleSelectionData`.
- `BattleSceneLoader` uu tien spawn pet theo `PrebattleSelectionData.PlayerPetId` / `EnemyPetId`.
- `CardManager` uu tien nap card da chon tu `PrebattleSelectionData.SelectedCards` (it hon so slot thi slot du se an).
- `GameManager` khi thang battle se goi `PlayerManager.AddMapWin(mapId)` + `SaveData()` de cap nhat tien do map.

## Cap nhat mo Prebattle tu MapEntry
- `MapEntryItemUI` tu tim `Button` neu thieu ref inspector (`GetComponent`/`GetComponentInChildren`).
- `MapPopupUI` tu tim `PrebattlePopupUI` neu thieu ref inspector va re-hook event map item khi mo popup map.
- Them warning log de de debug khi `mapData` hoac `PrebattlePopupUI` bi null.

## Cap nhat layer + cleanup popup pet
- `PrebattlePopupUI`: khi bam back/close se huy `spawnedPlayerPet` va `spawnedEnemyPet`.
- `PopupSelectPet`: popup canvas duoc force sorting order `20`; pet preview trong list duoc force sorting order `21`.
- `MapEntryItemUI`: pet preview map tiep tuc dung `ShortLayer`, force sorting order `3`.

## Cap nhat map popup transition + prebattle overlay
- `MapPopupUI` chuyen sang mo/an bang DOTween (`DOAnchorPos`), popup map luon active va truot tu duoi len.
- Khi mo `PrebattlePopupUI`: map popup tu dong truot an + tat preview pet map.
- Khi dong `PrebattlePopupUI` (back): map popup tu dong truot hien lai + bat lai preview pet map.
- `PrebattlePopupUI` ep sorting cho pet preview (player/enemy) thap hon popup select pet de tranh de layer.

## Cap nhat fix Shop Info + ShortLayer
- `ShopManager` popup info card doi sang logic dong khi click ngoai popup (khong dong ngay khi bam nut info).
- `ShopManager` tu tim `txtCardInfoDescription` neu thieu reference.
- `ShopItemUI` tu tim `BtnBuy`/`BtnInfo` neu thieu reference inspector.
- `PopupSelectPet` duoc ep `Canvas.overrideSorting` o order cao (`200`) de luon nam tren pet Training/Prebattle.
- `ShortLayer` bo debug log, them apply deferred + re-apply khi child thay doi de on dinh sorting luc spawn Spine.

## Cap nhat performance map area
- `MapPopupUI` doi sang load area theo `areaRootPrefabs` (prefab), spawn lazy theo tab de giam tai scene ban dau.
- `MapEntryItemUI` nam trong tung prefab area, `MapPopupUI` tu doc danh sach item trong area da spawn de refresh/hook event.
- Chuyen tab area co DOTween fade (`CanvasGroup.DOFade`) de muot hon khi spawn preview pet.
- Mo tu Home: reset ve area dau tien.
- Dang o area bat ky, vao `Prebattle` roi back: giu nguyen area vua chon.

## Cap nhat Battle UI Stats (moi)
- `UIManager` su dung `Image` component (fillAmount) thay vi `Slider` de hien thi HP/Mana/Rage bars.
- Data stats: Player HP, Mana, Rage lay tu `PlayerStats` (simulated data).
- Data stats: AI Health, Mana, Rage lay tu `AIStats` (simulated data).
- UI tu dong sync khi stats thay doi (Heal, TakeDamage, GainMana, GainRage).
- Them public methods de de dang ieu khien stat:
  - `DecreasePlayerHP()`, `IncreasePlayerHP()`, `IncreasePlayerMana()`, `IncreasePlayerRage()`
  - `DecreaseAIHP()`, `IncreaseAIHP()`, `IncreaseAIMana()`, `IncreaseAIRage()`

## Quick fixes (battle)

- Fix flip khi attack: l?u `originalScale` c?a `SkeletonAnimation` v� d�ng `FlipTowards(targetX)` tr??c khi di chuy?n t?i target, sau ?� `RestoreOriginalScale()` khi quay v?. (�p d?ng cho `PlayerStats` v� `AIStats`.)
- Fix tr??ng h?p prefab enemy c� `localScale.x` �m: restore `originalScale` ?? kh�ng m?t h??ng ban ??u.
- Damage text b�y gi? spawn d??i m?t `RectTransform popupParent` trong `DamagePopupManager` (UI), convert `WorldToScreenPoint` ?? tr�nh popup kh�ng b? m?t/kh�ng bi?n m?t.


## Cap nhat Audio (music + sfx)
- `AudioManager` them 2 playlist nhac:
  - `musicPlaylist` (scene thuong)
  - `battleMusicPlaylist` (scene battle)
- Tu dong doi playlist theo scene (`battleSceneName`, mac dinh `SceneBattle`), random + loop lien tuc.
- Them SFX moi:
  - `Upgrade Gem`: processing / success / fail
  - `Upgrade Pet`: success / fail
  - `Shop`: purchase success / fail
  - `Button Click` (auto bind button trong scene + register cho button spawn runtime)
- `UpdateGem` va `ChoosePet` deu co flow cho upgrade gem: play sound processing + hammer anim ~3s roi moi tra ket qua + sound success/fail.

## Cap nhat hammer prefab cho GemUpdate
- `GemUpdate` them `hammerPrefab` (keo prefab tu inspector).
- Khi upgrade trong training:
  - Spawn hammer tren tung slot da dat gem.
  - Play trong ~3s (`hammerEffectSeconds`).
  - Tu huy hammer roi moi xu ly ket qua nhu flow cu.
- `ChoosePet` uu tien goi flow hammer cua `GemUpdate`; neu khong co prefab se fallback hammer cu trong `ChoosePet`.
