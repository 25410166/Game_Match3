# SkillRangeType Documentation

## Overview
`SkillRangeType` định nghĩa cách skill được truyền tải (delivery method) từ attacker đến target.

---

## Types

### 1. **Projectile** (Đạn bay)
```csharp
rangeType = SkillRangeType.Projectile
```
- **Mô tả**: Một projectile (đạn/năng lượng) bay từ attacker tới target
- **Execution**: `ProjectileSkill.ExecuteAsync()`
- **Hiệu ứng**:
  - FX tại vị trí spawn (attacker area)
  - Projectile bay tới target
  - FX tại vị trí target khi trúng
  - Damage áp dụng tại target
- **Use Case**: Fireball, Lightning, Beam attack, Bow shot
- **Visual**: Projectile object di chuyển từ player → enemy

---

### 2. **DirectFX** (Hiệu ứng tức thì)
```csharp
rangeType = SkillRangeType.DirectFX
```
- **Mô tả**: Hiệu ứng tác động tức thì tại vị trí target
- **Execution**: `ExecuteDirectFxAsync()`
- **Hiệu ứng**:
  - FX spawn tại target position
  - Không có projectile bay
  - Damage áp dụng tức thì
- **Use Case**: Energy burst, Area explosion, Magic circle
- **Visual**: FX xuất hiện ngay tại target, không có di chuyển

---

### 3. **DirectImpact** (Chưởng lực) ⭐ NEW
```csharp
rangeType = SkillRangeType.DirectImpact
```
- **Mô tả**: Charged attack - Chưởng lực tác động tại vị trí attacker, không bay
- **Execution**: `ExecuteDirectImpactAsync()`
- **Hiệu ứng**:
  - FX spawn tại attacker position (không tại target)
  - Không có projectile bay
  - Damage áp dụng tại target dù FX ở attacker
  - Hiệu ứng aura/energy tại người tấn công
- **Use Case**: Charged punch, Melee energy burst, Shockwave from stance
- **Visual**: FX hiệu ứng ở attacker position, damage đến target từ xa
- **Lý do dùng**: Hiệu ứng chưởng lực có sức mạnh tức thì, không cần projectile

---

## Comparison Table

| Aspect | Projectile | DirectFX | DirectImpact |
|--------|-----------|----------|--------------|
| **FX Position** | Target | Target | Attacker |
| **Has Projectile** | ✓ Yes | ✗ No | ✗ No |
| **Projectile Movement** | ✓ Yes | - | - |
| **Damage Application** | After hit | Instant | Instant |
| **Use For** | Ranged attacks | Instant spells | Charged attacks |

---

## Card Attack Configuration

Card attacks (Skill ID 100) sử dụng **DirectImpact** type:

```csharp
// CardManager.cs - ExecuteCardAttackAsync()
cardSkill.rangeType = SkillRangeType.DirectImpact;  // Charged attack effect
```

**Lý do**: 
- Tạo cảm giác chưởng lực mạnh mẽ từ card
- FX hiệu ứng ở vị trí card player (attacker)
- Damage vẫn tác động đến enemy (target)
- Phù hợp với cảm giác "card activate" như charging power

---

## Creating Skills with Different Types

### Example 1: Projectile Skill (Fireball)
```csharp
skill.rangeType = SkillRangeType.Projectile;
skill.projectilePrefab = fireballPrefab;  // Must have
skill.fxPrefab = fireExplosionFX;         // Hit effect at target
skill.attackType = SkillAttackType.Range;
```

### Example 2: DirectFX Skill (Magic Circle)
```csharp
skill.rangeType = SkillRangeType.DirectFX;
skill.fxPrefab = magicCircleFX;           // Appears at target instantly
skill.projectilePrefab = null;            // Not used
skill.attackType = SkillAttackType.Range;
```

### Example 3: DirectImpact Skill (Charged Attack)
```csharp
skill.rangeType = SkillRangeType.DirectImpact;
skill.fxPrefab = chargedAuraFX;           // Appears at ATTACKER
skill.projectilePrefab = null;            // Not used
skill.attackType = SkillAttackType.Range; // or Melee
```

---

## Related Code

**SkillData.cs**:
```csharp
public enum SkillRangeType
{
    Projectile = 0,      // Đạn bay qua phía đối thủ
    DirectFX = 1,        // FX hiệu ứng tại vị trí mục tiêu
    DirectImpact = 2     // Chưởng lực - FX tác động tại vị trí hiện tại, không bay
}
```

**SkillExecutor.cs** - PlaySkillAsync():
```csharp
if (skill.rangeType == SkillRangeType.Projectile)
    return await ProjectileSkill.ExecuteAsync(skill, context, boardValue);

if (skill.rangeType == SkillRangeType.DirectImpact)
    return await ExecuteDirectImpactAsync(skill, context, boardValue);

return await ExecuteDirectFxAsync(skill, context, boardValue);  // Default: DirectFX
```
