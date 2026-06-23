# Data.md - Cau truc du lieu (cap nhat ngan gon)

## 1) Data trung tam

### `GameDataManager` (`Assets/Game/Scripts/GameDataManager.cs`)
Singleton `DontDestroyOnLoad`, giu tham chieu data dung chung:
- `PetDatabase`
- `CardDatabase`
- `GemCollection`
- `Match3SpriteResource` (hien tai dung type `Match3Resource`)
- `PetResource`
- `SkillDatabase` (1 ScriptableObject trung tam chua toan bo skill)

API chinh:
- `GetPetPrefab(int petId, string fallbackName = "")`
- `GetPetPrefabByName(string prefabName)`
- `TryGetPetStatSnapshot(int petId, int level, out PetStatSnapshot)`
- `GetPetMaxLevel(int petId)`
- `GetSkillData(int skillId)`

### `SkillDatabase` (`Assets/Game/Scripts/Battle/Skills/Data/SkillDatabase.cs`)
ScriptableObject trung tam cho Skill system:
- Field: `skills: List<SkillData>`
- API: `GetSkillById(int skillId)`
- API: `SetSkills(List<SkillData>)` (duoc importer su dung de sync list)

Data mau da tao san:
- `Assets/Game/Data/Skills/SkillDatabase.asset`
- `Assets/Game/Data/Skills/Skill_1001_BasicSlash.asset`

### `PetResource` (`Assets/Game/Scripts/Resource/PetResource.cs`)
Bang map prefab pet de lookup nhanh:
- `petId`
- `prefabName`
- `prefab`

Ho tro:
- `GetPrefabByPetId(...)`
- `GetPrefabByName(...)`

## 2) Player data

`PlayerSaveData` (`PlayerManager.cs`):
- profile: `playerName`, `level`, `currentExp`, `gold`, `diamond`
- flags: `hasConfirmedPlayerName`, `hasSelectedStarterPet`, `starterPetId`
- inventory:
  - `ownedPets: List<OwnedPetData>`
  - `ownedCards: List<OwnedCardData>`
  - `ownedGems: List<OwnedGemData>`

Models:
- `OwnedPetData`: `petId`, `petName`, `petLevel`
- `OwnedCardData`: `cardId`, `cardLevel`, `quantity`
- `OwnedGemData`: `elementId`, `gemLevel`, `quantity`

## 3) Popup Select Pet / Training

- `PopupSelectPet` tu dong doc `ownedPets` va render danh sach item.
- Item UI dung `PetItemButton` (spawn preview + idle + callback petId).
- `ChoosePet` nhan petId duoc chon, load prefab + stat current/next level qua `GameDataManager`.
- Khong con phu thuoc `PetStatsHolder` de lay stat upgrade UI.

## 4) Gem Upgrade Flow (moi)

- `GemUpdate` dung 3 slot gem (`slotButton1..3`).
- Click slot -> mo popup gem inventory theo `element` cua pet da chon.
- Popup gem chi hien gem user dang co (`PlayerSaveData.ownedGems`), sort theo `gemLevel` tang dan.
- Chon gem trong popup -> tu dong fill vao slot dang chon.
- Sau khi upgrade (success/fail) va tru gem -> `ClearSelectedGems()` + reload popup list.

### `PetUpgradeService` (`Assets/Game/Scripts/Gem/PetUpgradeService.cs`)
- Dung `UpgradeConfig.GetBaseRate(petLevel, gemLevel)`.
- Combine probability theo fail product.
- Bonus:
  - +5% neu du 3 gem
  - +5% neu 3 gem cung level
- Penalty:
  - *0.8 neu co gem yeu hon pet >= 2 level
- Clamp rate trong `[5%, 90%]`.

## 5) Shop + Match3

- `ShopManager` uu tien lay `CardDatabase` + `GemCollection` tu `GameDataManager`.
- `Board` uu tien lay `Match3Resource` tu `GameDataManager` neu chua gan inspector.

## 6) Prebattle tam luu du lieu

`PrebattleSelectionData` (`Assets/Game/Scripts/Map/PrebattleSelectionData.cs`):
- `MapId`
- `PlayerPetId`
- `EnemyPetId`
- `SelectedCards: List<PrebattleCardData>`

Su dung:
- `PrebattlePopupUI` ghi du lieu truoc khi vao battle.
- `BattleSceneLoader` doc pet id de spawn dung pet da chon.
- `CardManager` doc `SelectedCards` de nap card vao slot battle.
- `GameManager` dung `MapId` khi win de cong `mapWins` qua `PlayerManager`.

## 7) Event mo popup tu map

- `MapEntryItemUI` phat su kien `OnMapClicked(MapDataAsset)` khi click map button.
- `MapPopupUI` subscribe su kien nay de goi `PrebattlePopupUI.Open(mapData)`.
- Neu thieu ref inspector, `MapPopupUI` se tu tim `PrebattlePopupUI` bang `GetComponentInChildren(true)`.

## 8) Sorting + huy object tam

- `PrebattlePopupUI.Close()` huy 2 pet preview da spawn trong prebattle.
- `PopupSelectPet`:
  - Canvas sorting order: `20`
  - Pet preview sorting order: `21`
- `MapEntryItemUI` pet preview map: `ShortLayer.SortingOrder = 3`.

## 9) Battle Stats UI (moi)

### `PlayerStats` (simulated data)
- Stores: `HP`, `Mana`, `Rage` (current values)
- Stores: `maxHP`, `maxMana`, `maxRage` (max values)
- Methods:
  - `Init()` - Khoi tao stats khi battle start
  - `TakeDamage(int dmg)` - Giam HP theo sai thuong (tinh armor)
  - `Heal(int amount)` - Tang HP
  - `GainMana(int amount)` - Tang Mana
  - `GainRage(int amount)` - Tang Rage
  - `UpdateUI()` - Cap nhat UI Image bar va text

### `AIStats` (simulated data)
- Stores: `Health`, `Mana`, `Rage` (current values)
- Stores: `maxHealth`, `maxMana`, `maxRage` (max values)
- Methods:
  - `Init()` - Khoi tao stats khi battle start
  - `TakeDamage(int amount)` - Giam Health theo sai thuong (tinh armor)
  - `Heal(int amount)` - Tang Health
  - `GainMana(int amount)` - Tang Mana
  - `GainRage(int amount)` - Tang Rage
  - `UpdateUI()` - Cap nhat UI Image bar va text

### `UIManager` - Battle UI Management
- Su dung `Image` component (fillAmount) thay vi `Slider`
- UI References (Image bars):
  - Player: `playerHpBar`, `playerManaBar`, `playerRageBar`
  - AI: `aiHpBar`, `aiManaBar`, `aiRageBar`
- Text References:
  - Player: `playerHpText`, `playerManaText`, `playerRageText`
  - AI: `aiHpText`, `aiManaText`, `aiRageText`
- Methods:
  - `UpdatePlayerStat(string statType, int current, int max)` - Cap nhat Player bar + text
  - `UpdateAIStat(string statType, int current, int max)` - Cap nhat AI bar + text
  - `DecreasePlayerHP(int amount)` - Giam HP player
  - `IncreasePlayerHP(int amount)` - Tang HP player
  - `IncreasePlayerMana(int amount)` - Tang Mana player
  - `IncreasePlayerRage(int amount)` - Tang Rage player
  - `DecreaseAIHP(int amount)` - Giam HP AI
  - `IncreaseAIHP(int amount)` - Tang HP AI
  - `IncreaseAIMana(int amount)` - Tang Mana AI
  - `IncreaseAIRage(int amount)` - Tang Rage AI


## 9) Map popup animation state

- `MapPopupUI` khong tat `popupRoot`, chi doi vi tri `RectTransform.anchoredPosition` bang DOTween.
- Trang thai:
  - Hien: `shownPosition`
  - An: `hiddenPosition` (truot xuong duoi)
- Khi prebattle open/close, `MapPopupUI` nhan event tu `PrebattlePopupUI` de doi trang thai + toggle pet preview map.

## 10) Layer uu tien Popup Select Pet

- `PopupSelectPet` co `popupOverrideCanvas` va `overrideSorting` (order cao) de dam bao hien tren cung.
- Pet spawn trong `ChoosePet` / `PrebattlePopupUI` duoc ep `ShortLayer.SortingOrder < 20`.
- `ShortLayer` re-apply khi child thay doi + apply deferred 1 frame de bat renderer/SortingGroup tao muon.

## 11) Shop Card Info

- Nut info cua card goi `ShowCardInfo(description)`.
- Popup info dong khi click ngoai khung `popupCardInfoRect`.
- Neu thieu ref text mo ta, `ShopManager` tu tim `TextMeshProUGUI` trong `popupCardInfo`.

## 12) Map area prefab flow

- `MapPopupUI` su dung:
  - `areaRootPrefabs[]`: danh sach prefab tung area
  - `areaContentRoot`: noi instantiate prefab area
- Moi area duoc spawn lazy khi can, khong dat san 7 area trong scene.
- Sau khi spawn, `MapPopupUI` cache `MapEntryItemUI[]` cua area do de:
  - hook `OnMapClicked`
  - refresh data map item
  - bat/tat preview pet
- Chuyen area: fade `CanvasGroup` bang DOTween.
- State area:
  - `OpenPopup()` tu Home => area `0`
  - back tu Prebattle => giu `currentArea`.

## 13) Audio data flow

### `AudioManager` (`Assets/Game/Scripts/Manager/AudioManager.cs`)
- Playlist:
  - `musicPlaylist` (normal)
  - `battleMusicPlaylist` (battle)
- Scene switch:
  - theo `battleSceneName` => battle playlist
  - scene khac => normal playlist
- SFX key methods:
  - `PlayButtonClickSound()`
  - `PlayUpgradeGemProcessingSound()`
  - `PlayUpgradeGemSuccessSound()` / `PlayUpgradeGemFailedSound()`
  - `PlayUpgradePetSuccessSound()` / `PlayUpgradePetFailedSound()`
  - `PlayPurchaseSuccessSound()` / `PlayPurchaseFailedSound()`

### Runtime button click hookup
- `AudioManager.RegisterButtonClick(Button)` duoc goi cho button sinh runtime:
  - `ShopItemUI`, `PetItemButton`, `GemInventoryItemButton`, `PrebattleCardItemButton`, `MapEntryItemUI`.

### Upgrade flow timing
- `UpdateGem` / `ChoosePet` khi upgrade gem:
  - play SFX processing + hammer anim
  - cho ~3s
  - moi hien ket qua + play SFX success/fail.

## 14) GemUpdate hammer prefab binding

## Quick fix notes (battle)

- `PlayerStats`/`AIStats`: cache `originalScale` trong `Init()` va su dung `FlipTowards(targetX)` va `RestoreOriginalScale()` de dam bao flip dung huong va restore chinh xac.
- `DamagePopupManager`: them `RectTransform popupParent` (Canvas) va spawn popup lam con cua parent, convert `WorldToScreenPoint` de hien UI dung va tu dong destroy sau animation.
- `GameManager`: EndTurn da wait cho `isAttacking` false truoc khi doi turn (chi khi itemId==4 - attack).

`GemUpdate` them field:
- `hammerPrefab`
- `hammerLocalOffset`
- `hammerEffectSeconds`

API moi:
- `HasHammerPrefab`
- `PlayUpgradeHammerEffectRoutine()`

Flow:
- spawn hammer vao cac slot dang co gem (`selectedGemLevels > 0`)
- doi `hammerEffectSeconds`
- destroy hammer instances va tiep tuc xu ly ket qua upgrade.
