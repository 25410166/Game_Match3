# Game Match3 - Skill System Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture & Components](#architecture--components)
3. [Skill Types & Mechanics](#skill-types--mechanics)
4. [Setup & Configuration Guide](#setup--configuration-guide)
5. [Creating New Skills](#creating-new-skills)
6. [Skill Execution Flow](#skill-execution-flow)
7. [Advanced Features](#advanced-features)
8. [Troubleshooting](#troubleshooting)

---

## System Overview

The Skill System is a **modular, async-based architecture** that manages pet abilities in battle. It separates data configuration (what a skill does) from execution logic (how it does it), enabling flexible skill creation and modification.

### Key Features
- **Delivery Types**: Melee (close-range), Projectile (ranged with visual travel), DirectFX (instant effects)
- **Cost System**: Support for Mana, Rage, and HP-based costs
- **Board Effects**: Integration with the board system for gem-based bonuses
- **Hit Mechanics**: Configurable hit counts with delays between hits
- **Visual/Audio**: Flexible FX, projectile, and sound support
- **Async Execution**: Uses UniTask for smooth, non-blocking skill playback

---

## Architecture & Components

### 1. SkillData (Data Container)
**File**: `Assets/Game/Scripts/Battle/Skills/Data/SkillData.cs`

Defines what a skill does. Created as ScriptableObjects and stored in the SkillDatabase.

**Key Properties**:
```csharp
// Identity
int skillId;                          // Unique ID for the skill
string skillName;                     // Display name

// Delivery Method
SkillAttackType attackType;           // Melee or Range
SkillRangeType rangeType;             // Projectile or DirectFX

// Damage
int hitCount;                         // Number of hits (default 1)
float hitDelay;                       // Delay between hits (seconds)
float damageMultiplier;               // Base attack * multiplier

// Visual
GameObject fxPrefab;                  // Effect spawned at target
GameObject projectilePrefab;          // Projectile prefab (if Projectile rangeType)

// Cost
int manaCost;                         // Mana consumed
int rageCost;                         // Rage consumed
float hpCostPercent;                  // HP consumed as % of max (0-100)

// Board Interaction
int[] gemTypesAffected;               // Which gem types trigger board effect
SkillBoardEffectType boardEffectType; // None, Row, Column, Swap, Match

// Timing
float animationDuration;              // How long the attack animation plays
```

#### Enums

**SkillAttackType**:
- `Melee` (0): Pet walks to target, attacks, returns
- `Range` (1): Pet stays in place, uses ranged animation

**SkillRangeType**:
- `Projectile` (0): Spawns a visual projectile that travels to target
- `DirectFX` (1): Instant effect at target position (no travel)

**SkillBoardEffectType**:
- `None` (0): No board effect
- `Row` (1): Removes all gems in target's row
- `Column` (2): Removes all gems in target's column
- `Swap` (3): Swaps gems in a cross pattern
- `Match` (4): Calculates damage bonus from board matches

---

### 2. SkillDatabase (Lookup Container)
**File**: `Assets/Game/Scripts/Battle/Skills/Data/SkillDatabase.cs`

Centralized registry for all skills, providing fast lookup by ID.

**Methods**:
```csharp
// Get skill by ID (returns null if not found)
SkillData GetSkillById(int skillId)

// Set all skills at once (used by importers)
void SetSkills(List<SkillData> source)

// Mark cache dirty (called after skill modifications)
void MarkDirty()
```

**Implementation Notes**:
- Uses a `Dictionary<int, SkillData>` cache for O(1) lookup
- Cache is lazily rebuilt on demand
- Automatically marked dirty when assets change

---

### 3. SkillContext (Execution Context)
**File**: `Assets/Game/Scripts/Battle/Skills/Core/SkillContext.cs`

Provides all data and callbacks needed to execute a skill in context.

**Key Fields**:
```csharp
// Transforms
Transform AttackerTransform;          // Pet attacking
Transform TargetTransform;            // Enemy being attacked
Transform ProjectileSpawnPoint;       // Where projectiles spawn

// Stats Queries
Func<int> GetBaseAttack;              // Current attack stat
Func<int> GetCurrentMana;             // Current mana
Func<int> GetCurrentRage;             // Current rage
Func<int> GetCurrentHp;               // Current HP
Func<int> GetMaxHp;                   // Max HP (for cost calculation)

// Resource Spending
Action<int> SpendMana;                // Consume mana
Action<int> SpendRage;                // Consume rage
Action<int> SpendHp;                  // Consume HP

// Damage Application
Func<int, int> ApplyDamage;           // Deal damage, returns actual damage

// Animation Control
Action<string, bool> SetAnimation;    // Play animation (name, loop)
Action<float> FlipTowards;            // Flip sprite towards X position
bool ShouldFlip;                       // Whether to flip at all

// Movement
float MoveSpeed;                      // Speed of attack walk (units/sec)
float MeleeOffsetX;                   // Distance from target during melee
float MeleeAttackMoveX;               // Custom melee attack offset

// Animation Names
string IdleAnimation;                 // Default: "Idle"
string WalkAnimation;                 // Default: "Walk"
string MeleeAnimation;                // Default: "Attack"
string RangedAnimation;               // Default: "Shoot"

// VFX/Audio
GameObject DamagePopupPrefab;         // Floating damage number
GameObject HitFxPrefab;               // Impact effect at target
AudioSource HitAudioSource;           // Sound player
AudioClip HitSfx;                     // Skill hit sound

// Board System
Board Board;                          // Board reference for board effects

// Pet Configuration
GameObject ProjectilePrefabOverride;  // Pet's custom bullet (overrides skill's)
float MeleeAttackMoveX;              // Pet-specific melee offset

// Lifecycle
Action OnSkillStart;                  // Called when skill begins
Action OnSkillEnd;                    // Called when skill completes
CancellationToken CancellationToken;  // For async cancellation
```

---

### 4. SkillExecutor (Main Execution Logic)
**File**: `Assets/Game/Scripts/Battle/Skills/Executors/SkillExecutor.cs`

Main entry point for skill execution. Routes to appropriate executor (Melee/Projectile/DirectFX).

**Main Method**:
```csharp
static async UniTask<SkillResult> PlaySkillAsync(SkillData skill, SkillContext context)
```

**Execution Flow**:
1. Validate skill and context
2. Check if cost can be paid (mana, rage, HP)
3. Spend resource costs
4. Apply board effects and calculate bonuses
5. Route to appropriate skill executor:
   - If `Melee` → MeleeSkill.ExecuteAsync()
   - If `Projectile` → ProjectileSkill.ExecuteAsync()
   - Otherwise → ExecuteDirectFxAsync()

**Key Internal Methods**:
```csharp
// Check if resource costs can be paid
static bool CanPayCost(SkillData skill, SkillContext context)

// Consume resources
static void SpendCost(SkillData skill, SkillContext context)

// Calculate base damage for each hit
static int ComputeHitDamage(SkillData skill, SkillContext context, int boardValue)

// Apply damage to target and get actual damage dealt
static int ApplyHitDamage(SkillData skill, SkillContext context, int hitBaseDamage)

// Handle board-based damage bonuses
static int ApplyBoardEffect(SkillData skill, SkillContext context)

// Delay for timing
static async UniTask WaitSeconds(float seconds, SkillContext context)
```

---

### 5. Skill Executors (Type-Specific Logic)

#### MeleeSkill
**File**: `Assets/Game/Scripts/Battle/Skills/Executors/MeleeSkill.cs`

Handles close-range, physical attacks with movement.

**Sequence**:
1. Fire `OnSkillStart` event
2. Flip towards target (if configured)
3. Play walk animation
4. Move attacker to position (offset from target)
5. Play melee attack animation (duration from SkillData)
6. For each hit:
   - Spawn FX at target
   - Apply damage
   - Wait `hitDelay` before next hit
7. Flip back towards original position
8. Play walk animation
9. Move back to original position
10. Reset to idle
11. Fire `OnSkillEnd` event

**Returns**: `SkillResult` with total damage and hit count

---

#### ProjectileSkill
**File**: `Assets/Game/Scripts/Battle/Skills/Executors/ProjectileSkill.cs`

Handles ranged attacks with traveling projectiles.

**Sequence**:
1. Fire `OnSkillStart` event
2. Play ranged attack animation (duration from SkillData)
3. For each hit:
   - Spawn projectile at attacker (or custom spawn point)
   - Move projectile towards target at `MoveSpeed`
   - When reached target, destroy projectile
   - Apply damage
   - Spawn FX at target
   - Wait `hitDelay` before next hit
4. Return to idle animation
5. Fire `OnSkillEnd` event

**Returns**: `SkillResult` with total damage and hit count

**Note**: Uses custom projectile override if `ProjectilePrefabOverride` is set (pet-specific bullets)

---

#### DirectFX (Instant)
**File**: `Assets/Game/Scripts/Battle/Skills/Executors/SkillExecutor.cs`

Instant effects with no travel time or movement.

**Sequence**:
1. Fire `OnSkillStart` event
2. Spawn FX immediately at target
3. Apply damage once
4. Fire `OnSkillEnd` event

---

### 6. SkillResult (Execution Result)
**File**: `Assets/Game/Scripts/Battle/Skills/Core/SkillResult.cs`

Immutable result struct returned after skill execution.

```csharp
public readonly struct SkillResult
{
    public readonly bool Executed;      // Whether skill succeeded
    public readonly int TotalDamage;    // Total damage dealt
    public readonly int HitCount;       // Number of hits landed

    // Predefined failure result
    public static SkillResult Failed => new SkillResult(false, 0, 0);
}
```

---

## Skill Types & Mechanics

### Type 1: Melee Skills (Close Combat)

**Characteristics**:
- Pet walks to target
- Attacks in close range
- Returns to original position
- Best for high-damage, physical abilities

**Configuration Example**:
```
skillId: 101
skillName: "Slash Attack"
attackType: Melee
rangeType: DirectFX
hitCount: 1
hitDelay: 0
damageMultiplier: 1.5
animationDuration: 1.2
manaCost: 20
rageCost: 0
hpCostPercent: 0
```

**Visual Flow**:
```
Original Pos ────→ Walk ────→ Target Pos ────→ Attack ────→ Walk ────→ Original Pos
   (Idle)        (animation)   (offset)     (animation)   (animation)    (Idle)
```

---

### Type 2: Projectile Skills (Ranged Combat)

**Characteristics**:
- Pet stays in place
- Fires projectile at target
- Projectile travels visually
- Best for ranged, magical abilities

**Configuration Example**:
```
skillId: 102
skillName: "Fireball"
attackType: Range
rangeType: Projectile
hitCount: 1
hitDelay: 0
damageMultiplier: 1.2
projectilePrefab: Assets/Game/VFX/Projectiles/Fireball.prefab
fxPrefab: Assets/Game/VFX/Explosions/FireExplosion.prefab
animationDuration: 1.0
manaCost: 30
rageCost: 0
hpCostPercent: 0
```

**Visual Flow**:
```
Attacker (Idle)
    ↓ (play Shoot animation)
    ├─ Spawn Projectile
    ├─ Projectile travels to target
    └─ Spawn FX at target → Apply Damage
```

---

### Type 3: Instant/DirectFX Skills (Instant Effects)

**Characteristics**:
- Instant effect at target
- No movement or travel time
- Can be combined with melee or ranged animation
- Best for utility abilities, buffs, or instant damage

**Configuration Example**:
```
skillId: 103
skillName: "Energy Pulse"
attackType: Range
rangeType: DirectFX
hitCount: 1
damageMultiplier: 2.0
fxPrefab: Assets/Game/VFX/Effects/EnergyPulse.prefab
animationDuration: 0.8
manaCost: 15
rageCost: 10
hpCostPercent: 0
```

---

### Board Effect Types

Board effects trigger special interactions with the gem board.

#### None (Default)
- No board interaction
- Damage calculation: `baseAttack * damageMultiplier`

#### Row Effect
- Removes all gems in target's row
- Bonus calculated from matches created

#### Column Effect
- Removes all gems in target's column
- Bonus calculated from matches created

#### Swap Effect
- Swaps gems in a cross pattern around target
- Creates chain reactions

#### Match Effect
- Calculates damage bonus from board matches
- Damage = `baseAttack * damageMultiplier + boardMatchValue`
- Higher gem counts = higher bonus

---

### Cost System

#### Mana Cost
- Consumed from attacker's mana pool
- Skill fails if insufficient mana
- Prevents spamming high-cost abilities

#### Rage Cost
- Consumed from attacker's rage pool
- Builds up during combat
- Reserved for ultimate abilities

#### HP Cost (Percentage-based)
- Calculated as: `maxHp * hpCostPercent / 100`
- Rounded up with `Mathf.CeilToInt()`
- Skill fails if HP would drop to 0 or below
- Self-sacrifice mechanic for powerful abilities

**Cost Validation**:
All costs are checked before skill execution. Skill fails if ANY cost cannot be paid.

```csharp
bool CanPayCost(SkillData skill, SkillContext context)
{
    // Checks: mana >= cost, rage >= cost, hp > hpCost
}
```

---

### Hit Mechanics

#### Hit Count
- Number of times damage is applied
- Default: 1
- Multi-hit skills (e.g., 3-hit combo): set to 3

#### Hit Delay
- Time between hits (seconds)
- Allows visual spacing between damage applications
- Example: 0.5 = half-second between hits

#### Example: 3-Hit Combo with 0.3s Delay
```
hitCount: 3
hitDelay: 0.3

Timeline:
T=0.0s   → 1st Hit Damage
T=0.3s   → 2nd Hit Damage
T=0.6s   → 3rd Hit Damage
```

---

### Damage Calculation

**Formula**:
```
hitBaseDamage = max(1, baseAttack + boardBonus)

If boardEffectType == Match and boardValue > 0:
    hitBaseDamage += boardValue

finalDamage = hitBaseDamage * damageMultiplier

totalSkillDamage = Sum of all hit damages
```

**Example**:
```
Skill Config:
- hitCount: 2
- damageMultiplier: 1.5
- boardEffectType: None

Combat Stats:
- baseAttack: 100

Calculation:
hitBaseDamage = 100
each hit damage = 100 * 1.5 = 150
totalSkillDamage = 150 * 2 = 300
```

---

## Setup & Configuration Guide

### Step 1: Create the Skill Database

1. Right-click in `Assets/Game/Data/Skills/` folder
2. Select **Create → Battle → Skill Database**
3. Name it `SkillDatabase.asset`
4. This will store all skills in your game

### Step 2: Create Individual Skill Assets

**Option A: Manual Creation**

1. Right-click in `Assets/Game/Data/Skills/`
2. Select **Create → Battle → Skill Data**
3. Name it `Skill_[ID].asset` (e.g., `Skill_101.asset`)
4. Configure properties in Inspector

**Option B: Google Sheets Import (Automated)

1. Create a Google Sheet with columns:
   ```
   skillId | skillName | attackType | rangeType | hitCount | hitDelay | 
   damageMultiplier | manaCost | rageCost | hpCostPercent | 
   boardEffectType | animationDuration | fxPrefab | projectilePrefab
   ```

2. Share sheet with public link (CSV export URL)

3. Create importer:
   - Right-click → Create → Battle → Skill Data Importer
   - Name: `SkillDataImporter.asset`

4. In Inspector:
   - Set **Google Sheet CSV Url**
   - Set **Target Skill Database**

5. Click button **Import Skills From Google Sheet**
   - Creates all skills automatically
   - Updates existing skills if already present

---

### Step 3: Configure Skill Properties

#### For Melee Skills:
```
attackType: Melee
rangeType: DirectFX (usually) or Projectile
damageMultiplier: 1.2 - 2.0 (close combat does more damage)
animationDuration: 1.0 - 1.5s
manaCost: 15 - 30
Set meleeOffsetX in SkillContext for position
```

#### For Projectile Skills:
```
attackType: Range
rangeType: Projectile
damageMultiplier: 0.8 - 1.5
projectilePrefab: REQUIRED (actual projectile model)
fxPrefab: REQUIRED (impact effect)
animationDuration: 0.8 - 1.2s
Set MoveSpeed in SkillContext for projectile speed
```

#### For Instant Effects:
```
attackType: Range or Melee (for animation choice)
rangeType: DirectFX
damageMultiplier: Varies
fxPrefab: REQUIRED (visual effect)
animationDuration: 0.5 - 1.0s (quick cast)
```

---

### Step 4: Assign Skills to Pets

In [PetLevelData](../Game/Data/Pets/) or your pet configuration:
```csharp
petData.skillId = 101;  // References Skill_101.asset
```

When pet enters battle, the skill is loaded via:
```csharp
SkillData skill = GameDataManager.Instance.SkillDatabase.GetSkillById(petData.skillId);
```

---

### Step 5: Configure SkillContext in Battle

When battle starts, SkillContext must be populated:

```csharp
SkillContext context = new SkillContext
{
    AttackerTransform = attackingPet.transform,
    TargetTransform = defendingPet.transform,
    ProjectileSpawnPoint = attackingPet.bulletSpawner,

    // Stat queries
    GetBaseAttack = () => attackingPet.stats.attack,
    GetCurrentMana = () => attackingPet.stats.mana,
    GetCurrentRage = () => attackingPet.stats.rage,
    GetCurrentHp = () => attackingPet.stats.hp,
    GetMaxHp = () => attackingPet.stats.maxHp,

    // Resource spending
    SpendMana = (cost) => attackingPet.stats.mana -= cost,
    SpendRage = (cost) => attackingPet.stats.rage -= cost,
    SpendHp = (cost) => attackingPet.stats.hp -= cost,

    // Damage application
    ApplyDamage = (damage) => defendingPet.TakeDamage(damage),

    // Animation control
    SetAnimation = (name, loop) => attackingPet.skeleton.state.SetAnimation(0, name, loop),
    FlipTowards = (xPos) => attackingPet.sprite.flipX = (xPos < attackingPet.transform.position.x),

    // Movement
    MoveSpeed = 15f,
    MeleeOffsetX = -1.2f,
    MeleeAttackMoveX = -1.2f,

    // Animation names
    IdleAnimation = "Idle",
    WalkAnimation = "Walk",
    MeleeAnimation = "Attack",
    RangedAnimation = "Shoot",

    // VFX/Audio
    DamagePopupPrefab = damagePopupPrefab,
    HitFxPrefab = hitEffectPrefab,
    HitAudioSource = audioSource,
    HitSfx = skillHitSound,

    // Board system
    Board = battleBoard,

    // Pet overrides
    ProjectilePrefabOverride = attackingPet.customBulletPrefab,

    // Lifecycle
    OnSkillStart = () => Debug.Log("Skill started"),
    OnSkillEnd = () => Debug.Log("Skill finished"),

    CancellationToken = cancellationToken
};

// Execute skill
SkillResult result = await SkillExecutor.PlaySkillAsync(skillData, context);
if (result.Executed)
{
    Debug.Log($"Skill dealt {result.TotalDamage} damage in {result.HitCount} hits");
}
```

---

## Creating New Skills

### Example: "Lightning Strike" (Melee + Multi-hit)

**Requirements**:
- Electric attack animation
- Lightning projectile prefab
- Impact effect prefab

**Steps**:

1. **Create Asset**
   - Right-click Assets/Game/Data/Skills/
   - Create → Battle → Skill Data
   - Name: `Skill_201.asset`

2. **Configure Properties**:
   - **Identity**
     - skillId: 201
     - skillName: "Lightning Strike"
   
   - **Delivery**
     - attackType: Melee
     - rangeType: DirectFX
   
   - **Damage**
     - hitCount: 3 (3 lightning strikes)
     - hitDelay: 0.2 (0.2s between hits)
     - damageMultiplier: 1.2
   
   - **Visual**
     - fxPrefab: Assets/Game/VFX/LightningImpact.prefab
   
   - **Cost**
     - manaCost: 40
     - rageCost: 0
     - hpCostPercent: 0
   
   - **Timing**
     - animationDuration: 1.5

3. **Add to Database**
   - Select SkillDatabase.asset
   - In Inspector, add to Skills list
   - Drag Skill_201 into the list

4. **Test**
   - Assign skillId: 201 to a test pet
   - Run battle
   - Pet should execute 3 lightning strikes with 0.2s spacing

---

### Example: "Meteor Rain" (Projectile with Delay)

**Requirements**:
- Projectile prefab
- Meteor effect
- Impact explosion effect

**Steps**:

1. **Create Asset**: `Skill_202.asset`

2. **Configure**:
   - **Delivery**
     - attackType: Range
     - rangeType: Projectile
   
   - **Damage**
     - hitCount: 1
     - damageMultiplier: 2.5 (high damage for ranged)
   
   - **Visual**
     - projectilePrefab: Assets/Game/VFX/Meteor.prefab
     - fxPrefab: Assets/Game/VFX/ExplosionLarge.prefab
   
   - **Cost**
     - manaCost: 50
     - rageCost: 0
   
   - **Timing**
     - animationDuration: 1.0

3. **In SkillContext** (battle initialization):
   - Set MoveSpeed higher (25f) for faster meteor travel
   - Set custom projectile if needed: `context.ProjectilePrefabOverride = customMeteor`

---

### Example: "Sacrifice Blow" (HP Cost + High Damage)

**Concept**: Self-damage ability for high damage burst

**Steps**:

1. **Create Asset**: `Skill_203.asset`

2. **Configure**:
   - **Damage**
     - hitCount: 1
     - damageMultiplier: 3.0 (very high damage)
   
   - **Cost**
     - manaCost: 0
     - rageCost: 0
     - hpCostPercent: 25 (costs 25% max HP)
   
   - **Timing**
     - animationDuration: 1.2

3. **Validation**:
   - System checks: `hp > hpCost` before execution
   - If pet has 100 HP max and 50 current, attack FAILS (needs 75 HP available for 25% cost)

---

## Skill Execution Flow

### Complete Execution Timeline

```
[1] PlaySkillAsync() called
    ↓
[2] Validate skill + context not null
    ↓
[3] Validate transforms exist
    ↓
[4] Check if cost can be paid → CanPayCost()
    ├─ mana >= cost? ✓
    ├─ rage >= cost? ✓
    ├─ hp > hpCost? ✓
    ↓ (All pass)
[5] Spend resources → SpendCost()
    ├─ Reduce mana
    ├─ Reduce rage
    ├─ Reduce HP
    ↓
[6] Apply board effects → ApplyBoardEffect()
    ├─ Check board for gem matches
    ├─ Calculate bonus value
    ↓
[7] Route by attackType
    ├─ If Melee → MeleeSkill.ExecuteAsync()
    ├─ If Range + Projectile → ProjectileSkill.ExecuteAsync()
    └─ If Range + DirectFX → ExecuteDirectFxAsync()
    ↓
[8] In Executor:
    ├─ OnSkillStart?.Invoke()
    ├─ Setup animations + position
    ├─ For each hit (i = 0 to hitCount-1):
    │  ├─ If i > 0: Wait hitDelay
    │  ├─ ComputeHitDamage() → base attack + board bonus
    │  ├─ ApplyHitDamage() → actual damage applied
    │  ├─ Spawn FX at target
    │  └─ Total damage += actual damage
    ├─ Cleanup (animations, movement)
    └─ OnSkillEnd?.Invoke()
    ↓
[9] Return SkillResult
    ├─ Executed: true
    ├─ TotalDamage: sum of all hits
    ├─ HitCount: number of hits
    ↓
[10] Caller receives result
     └─ Update UI, logs, state, etc.
```

### Failure Points (Returns SkillResult.Failed)

1. **Skill is null**
2. **Context is null**
3. **AttackerTransform is null**
4. **TargetTransform is null**
5. **Insufficient mana** (mana < skill.manaCost)
6. **Insufficient rage** (rage < skill.rageCost)
7. **Insufficient HP** (hp <= hpCost)

---

## Advanced Features

### 1. Multi-Hit Damage Scaling

Create abilities that deal more damage the more they hit:

```csharp
// In your damage calculation override
int totalHits = skill.hitCount;
float hitScaling = 1f + (totalHits - 1) * 0.15f; // 15% increase per extra hit

int adjustedDamage = baseHitDamage * hitScaling;
```

---

### 2. Board-Based Damage Bonuses

Skills with `boardEffectType: Match` get bonus from board state:

```
Example: Gem Attack on 5-gem match
baseAttack: 100
damageMultiplier: 1.0
boardBonus: 150 (from 5-gem match)

finalDamage = (100 + 150) * 1.0 = 250
```

---

### 3. Pet-Specific Skill Variations

Use `ProjectilePrefabOverride` and `MeleeAttackMoveX` from SkillContext:

```csharp
// Pet "FireDragon" uses custom fireball
context.ProjectilePrefabOverride = firedragonCustomBullet;
context.MeleeAttackMoveX = -2.0f; // Larger melee range

// Pet "Goblin" uses default projectile but short melee range
context.ProjectilePrefabOverride = null; // Use skill's projectile
context.MeleeAttackMoveX = -0.5f; // Tiny melee range
```

---

### 4. Chained Skill Execution

Execute multiple skills in sequence:

```csharp
// First skill (setup)
SkillResult setup = await SkillExecutor.PlaySkillAsync(setupSkill, context);

if (setup.Executed)
{
    // Second skill (execute based on first result)
    SkillResult followup = await SkillExecutor.PlaySkillAsync(followupSkill, context);
    
    int totalDamage = setup.TotalDamage + followup.TotalDamage;
}
```

---

### 5. Conditional Skill Effects

Add logic based on battle state:

```csharp
// Rage-based skill that changes behavior
if (context.GetCurrentRage() >= 100)
{
    // Unleash "Ultimate" version (3x damage)
    skillData.damageMultiplier = 3.0f;
    context.SpendRage(100);
}
else
{
    // Normal attack
    skillData.damageMultiplier = 1.0f;
}

SkillResult result = await SkillExecutor.PlaySkillAsync(skillData, context);
```

---

## Troubleshooting

### Issue: Skill Fails Silently (SkillResult.Failed)

**Diagnosis**:
```csharp
SkillResult result = await SkillExecutor.PlaySkillAsync(skill, context);
if (!result.Executed)
{
    Debug.Log("Skill failed execution");
}
```

**Common Causes**:
1. **Null Skill**: `SkillData GetSkillById()` returned null
   - Check skillId is correct
   - Verify skill exists in SkillDatabase
   - Call `skillDatabase.MarkDirty()` after adding skills

2. **Null Context**: SkillContext was not created properly
   - Ensure all required callbacks are set
   - Check transforms are valid

3. **Insufficient Resources**:
   ```csharp
   // Check before calling
   if (skillData.manaCost > currentMana)
       return false; // Skill will fail
   ```

4. **Invalid Transforms**:
   ```csharp
   if (context.AttackerTransform == null)
       return false; // Skill will fail
   ```

---

### Issue: Projectile Not Appearing

**Cause**: `projectilePrefab` is null

**Fix**:
1. Check SkillData has `projectilePrefab` assigned
2. Verify prefab path is correct
3. Check projectile prefab itself is valid (not corrupted)

**Debug**:
```csharp
if (skill.projectilePrefab == null)
    Debug.LogError($"Skill {skill.skillName} missing projectile");
```

---

### Issue: Animation Not Playing

**Cause**: Animation names don't match Spine skeleton setup

**Fix**:
```csharp
// In SkillContext initialization
context.IdleAnimation = "Idle";         // Must exist in skeleton
context.WalkAnimation = "Walk";         // Must exist in skeleton
context.MeleeAnimation = "Attack";      // Must exist in skeleton
context.RangedAnimation = "Shoot";      // Must exist in skeleton
```

**Verify**:
- Open Spine project
- Check animation names match exactly (case-sensitive)
- Ensure animations are in animation list

---

### Issue: Import From Google Sheet Not Working

**Cause**: CSV URL format or permissions

**Fix**:
1. Share Google Sheet publicly (Anyone with link can view)
2. Get CSV export URL:
   - Share button → Copy link → Change `/edit` to `/export?format=csv`
   - Example: `https://docs.google.com/spreadsheets/d/.../export?format=csv`
3. Paste full URL in SkillDataImporter
4. Check column names exactly match (case-sensitive, no spaces)

**Debug**:
```csharp
// Check import logs
Debug.Log("[SkillDataImporter] Import complete. Created: X, Updated: Y, Skipped: Z");
```

---

### Issue: Damage Not Scaling With Stats

**Cause**: `ComputeHitDamage()` not getting base attack

**Fix**:
```csharp
// Ensure GetBaseAttack is set
context.GetBaseAttack = () =>
{
    int attack = pet.stats.attack;
    Debug.Log($"Base attack: {attack}");
    return attack;
};
```

**Check Formula**:
```
hitBaseDamage = max(1, baseAttack + boardBonus)
finalDamage = hitBaseDamage * damageMultiplier

If getting 1 damage: baseAttack is probably 0
If consistent damage: multiplier might be wrong
```

---

## Performance Optimization

### Skill Caching
```csharp
// Cache loaded skills instead of looking up every time
SkillData cachedSkill = skillDatabase.GetSkillById(skillId);
// Reuse cachedSkill in battle
```

### Async Cancellation
```csharp
// Stop ongoing skill execution
cancellationTokenSource.Cancel();

// In context
context.CancellationToken = cancellationTokenSource.Token;
```

### Object Pooling for Projectiles
```csharp
// Instead of Instantiate/Destroy
public class ProjectilePool
{
    private Queue<GameObject> available = new Queue<GameObject>();
    
    public GameObject Get() => available.Count > 0 
        ? available.Dequeue() 
        : Instantiate(prefab);
    
    public void Return(GameObject go) 
    {
        go.SetActive(false);
        available.Enqueue(go);
    }
}
```

---

## Summary

The Skill System provides:
- ✅ Flexible, modular skill architecture
- ✅ Multiple delivery types (Melee, Projectile, Instant)
- ✅ Cost system (Mana, Rage, HP)
- ✅ Board integration for combo bonuses
- ✅ Async execution for smooth gameplay
- ✅ Extensible design for custom effects

**Key Files**:
- `SkillData.cs` - Skill configuration
- `SkillDatabase.cs` - Skill registry
- `SkillExecutor.cs` - Main execution logic
- `MeleeSkill.cs` - Melee execution
- `ProjectileSkill.cs` - Ranged execution
- `SkillContext.cs` - Execution context
- `SkillResult.cs` - Execution result

**Setup Checklist**:
- [ ] Create SkillDatabase.asset
- [ ] Create individual Skill_[ID].asset files
- [ ] Configure each skill's properties
- [ ] Assign skills to pets
- [ ] Create SkillContext in battle system
- [ ] Call SkillExecutor.PlaySkillAsync()
- [ ] Handle SkillResult

For questions or issues, check Troubleshooting section or review example configurations above.
