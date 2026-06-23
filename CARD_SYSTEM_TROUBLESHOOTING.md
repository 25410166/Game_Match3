# Card System Troubleshooting Guide

## Problem: Card Click Not Working

Khi click vào card trên UI nhưng không có gì xảy ra, hãy kiểm tra theo các bước dưới đây.

---

## Step 1: Kiểm tra Debug Logs

1. **Mở Unity Console** (Window > TextMesh Pro > Console hoặc `Ctrl+Shift+C`)
2. **Play game và click card**
3. **Tìm các log messages:**
   - `[CardManager] Start() called` → CardManager đã khởi tạo
   - `[CardSlotUI] SetCard() called` → Card slot đã được setup
   - `[CardSlotUI] Button clicked!` → Button click được nhận
   - `[CardManager] UseCard() called` → CardManager.UseCard() được gọi
   - `[CardManager] Attack card detected` → Card type được nhận dạng đúng

### Nếu không thấy log nào:
- **CardManager không khởi tạo** → Kiểm tra Step 2
- **Sự kiện click UI không được nhận** → Kiểm tra Step 3
- **UseCard() không được gọi** → Kiểm tra Step 4

---

## Step 2: Kiểm tra CardManager References

1. **Tìm scene object có component CardManager**
2. **Kiểm tra Inspector:**
   - ✓ **PlayerStats** → Phải gán PlayerStats từ scene
   - ✓ **AIStats** → Phải gán AIStats từ scene
   - ✓ **Card Slots** → Array phải có 4 CardSlotUI components

3. **Nếu references null:**
   - Drag PlayerStats object vào field "Player Stats"
   - Drag AIStats object vào field "AI Stats"
   - Kéo 4 card slot UI objects vào Card Slots array

### Hoặc chạy validation từ Console:
```
// Right-click CardManager component → "Validate Setup"
```

Sẽ in ra:
```
========== [CardManager] SETUP VALIDATION ==========
Database: ✓ OK
PlayerStats: ✓ OK
AIStats: ✓ OK
Card Slots: ✓ OK (4 slots)
GameManager: ✓ OK
GameDataManager: ✓ OK
====================================================
```

---

## Step 3: Kiểm tra UI Button Setup

1. **Tìm Card Slot UI objects trong scene**
2. **Mở Inspector của một Card Slot**
3. **Kiểm tra:**
   - ✓ **Icon** field → Phải là Image component
   - ✓ **Card Name Text** field → Phải là TextMeshProUGUI component
   - ✓ **Use Button** field → Phải là Button component

4. **Kiểm tra CardSlotUI script gán đúng:**
   ```csharp
   public Image icon;
   public TextMeshProUGUI cardNameText;
   public Button useButton;  // Đây là cái phải click
   ```

---

## Step 4: Kiểm tra Card Data

1. **Kiểm tra CardDatabase:**
   - Phải có ít nhất 1 card trong danh sách
   - Card type phải là **Attack**
   - Card phải có level data (3 levels)

2. **Kiểm tra Skill ID 100:**
   - Mở **GameDataManager** → SkillDatabase
   - Tìm skill với ID = **100**
   - Nếu không có → **Tạo skill ID 100** hoặc thay đổi ID trong CardManager

---

## Step 5: Kiểm tra Game Manager & Players

1. **Chắc rằng có GameManager trong scene**
2. **GameManager phải có:**
   - PlayerStats gán (auto-find)
   - AIStats gán (auto-find)
   - CardManager gán (kiểm tra Inspector)

3. **Chắc rằng:**
   - Player object có PlayerStats component
   - AI object có AIStats component
   - Cả hai đều được Init() trong Start()

---

## Step 6: Full Debug Output

Khi click card, bạn sẽ thấy chuỗi log như này:

```
[CardSlotUI] SetCard() called - Card: FireBall, Level: 2
[CardSlotUI] Button listener registered for card: FireBall
[CardSlotUI] Button clicked! Calling manager.UseCard(FireBall, 2)
[CardManager] UseCard() called with card: FireBall level: 2
[CardManager] Using card: FireBall (Lv 2), Type: Attack
[CardManager] Attack card detected, executing async attack...
[CardManager] ExecuteCardAttackAsync() started
[CardManager] Calling GameManager.OnCardActionStart()
[CardManager] Base damage: 50
[CardManager] Skill 100 found: Card Attack
[CardManager] Skill data cloned, now queuing in PlayerStats
[CardManager] Calling playerStats.Attack()...
[CardManager] Waiting for attack to complete...
[CardManager] Card attack completed: FireBall
[CardManager] Card attack in progress: FALSE
[CardManager] Ending turn via GameManager
```

---

## Các Vấn Đề Thường Gặp

### ❌ "PlayerStats is NULL"
- Giải pháp: Gán PlayerStats vào CardManager Inspector hoặc đảm bảo nó tồn tại trong scene

### ❌ "Skill id 100 not found"
- Giải pháp: Kiểm tra GameDataManager có SkillDatabase không, tạo skill ID 100

### ❌ "Button clicked" nhưng "UseCard() không được gọi"
- Giải pháp: Manager reference bị null, kiểm tra SetCard() được gọi đúng không

### ❌ "UseCard() được gọi" nhưng "không có animation"
- Giải pháp: Kiểm tra Skill ID 100 configuration, PlayerStats.Attack() có chạy không

---

## Cách Test

1. **Play game**
2. **Mở Console: Ctrl+Shift+C**
3. **Click card trên UI**
4. **Xem logs để debug**

Nếu vẫn không hoạt động sau khi kiểm tra tất cả các bước trên, hãy gửi toàn bộ Console output.
