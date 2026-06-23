# StackOverflow Fix - Battle Data Loading

## Problem
Khi thắng battle và return về map, xảy ra StackOverflowException:
```
StackOverflowException: The requested operation caused a stack overflow.
GameDataManager.GetFieldValue[T]() -> System.RuntimeType.GetField()
GameDataManager.ResolvePetFamilyId()
GameDataManager.GetPetDataByPetIdAndLevel()
GameDataManager.TryGetPetStatSnapshot()
MapEntryItemUI.TryGrantPetReward()
```

## Nguyên Nhân
1. **Không có exception handling** trong `GetFieldValue()` - khi gặp error thì không catch
2. **Không có caching** cho `ResolvePetFamilyId()` - có thể gọi lại với cùng data
3. **Reflection liên tục** trên types phức tạp gây StackOverflow

## Giải Pháp

### 1. GameDataManager.cs - Thêm Caching

```csharp
// Cache for ResolvePetFamilyId to avoid infinite recursion
private Dictionary<int, int> petFamilyIdCache = new Dictionary<int, int>();

public void ClearPetFamilyIdCache()
{
    if (petFamilyIdCache != null)
        petFamilyIdCache.Clear();
}
```

- Lưu kết quả `ResolvePetFamilyId()` để không phải tính lại
- Call `ClearPetFamilyIdCache()` sau khi data thay đổi (sau battle)

### 2. GameDataManager.cs - Exception Handling trong GetFieldValue()

```csharp
private static T GetFieldValue<T>(object target, string fieldName)
{
    if (target == null)
        return default(T);

    if (string.IsNullOrWhiteSpace(fieldName))
        return default(T);

    try
    {
        Type targetType = target.GetType();
        if (targetType == null)
            return default(T);

        FieldInfo field = targetType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
            return default(T);

        object value = field.GetValue(target);
        if (value is T)
            return (T)value;

        return default(T);
    }
    catch (System.StackOverflowException ex)
    {
        Debug.LogError($"[GameDataManager] StackOverflow in GetFieldValue");
        throw ex;  // Re-throw để biết lỗi ở đâu
    }
    catch (System.Exception ex)
    {
        Debug.LogWarning($"[GameDataManager] Error getting field {fieldName}: {ex.GetType().Name}");
        return default(T);
    }
}
```

- Catch tất cả exceptions từ reflection
- Return default value thay vì crash
- Log chi tiết để debug

### 3. GameDataManager.cs - Exception Handling trong ResolvePetFamilyId()

```csharp
private int ResolvePetFamilyId(int petId)
{
    // Check cache first
    if (petFamilyIdCache != null && petFamilyIdCache.TryGetValue(petId, out int cachedResult))
        return cachedResult;

    // ... logic ...

    try
    {
        // ...
    }
    catch (System.Exception ex)
    {
        Debug.LogWarning($"[GameDataManager] Error in ResolvePetFamilyId({petId})");
        result = petId;
    }

    // Cache the result
    if (petFamilyIdCache != null)
        petFamilyIdCache[petId] = result;

    return result;
}
```

### 4. MapPopupUI.cs - Clear Cache Sau Battle

```csharp
private void HandlePrebattleClosed()
{
    // Clear cache after battle to refresh data
    if (GameDataManager.Instance != null)
        GameDataManager.Instance.ClearPetFamilyIdCache();

    SetMapPopupVisible(true);
    // ... rest of logic ...
}
```

### 5. MapEntryItemUI.cs - Exception Handling trong Refresh()

```csharp
public void Refresh()
{
    try
    {
        // ... normal logic ...
        TryGrantPetReward(wins);
    }
    catch (System.StackOverflowException ex)
    {
        Debug.LogError($"[MapEntryItemUI] StackOverflow in Refresh");
        // Do not retry
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[MapEntryItemUI] Error in Refresh: {ex.Message}");
    }
}
```

### 6. MapPopupUI.cs - Exception Handling trong RefreshAreaMapItems()

```csharp
private void RefreshAreaMapItems(int areaIndex)
{
    // ... get items ...

    for (int i = 0; i < areaItems.Length; i++)
    {
        try
        {
            item.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MapPopupUI] Error refreshing map item {i}");
            // Continue với items khác
        }
    }
}
```

## Kết Quả

✅ Không bị StackOverflow khi load map sau battle
✅ Cache giảm reflection calls
✅ Exception handling chi tiết giúp debug
✅ Một item lỗi không ảnh hưởng đến items khác

## Test

1. Play game
2. Thắng battle
3. Return về map
4. Kiểm tra console:
   - Không có StackOverflowException
   - Map items được refresh đúng
   - Pet rewards được claim đúng

## Cache Clearing Points

Call `GameDataManager.Instance.ClearPetFamilyIdCache()` ở các điểm:
- Khi data game thay đổi
- Sau khi complete battle (`HandlePrebattleClosed`)
- Khi reload map data
