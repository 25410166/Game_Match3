using UnityEditor;

[CustomEditor(typeof(SkillData))]
public class SkillDataEditor : Editor
{
    private SerializedProperty skillIdProp;
    private SerializedProperty skillNameProp;
    private SerializedProperty desSkillProp;
    private SerializedProperty attackTypeProp;
    private SerializedProperty rangeTypeProp;
    private SerializedProperty typeSkillProp;
    private SerializedProperty hitCountProp;
    private SerializedProperty hitDelayProp;
    private SerializedProperty damageMultiplierProp;
    private SerializedProperty auditionSequenceLengthMinProp;
    private SerializedProperty auditionSequenceLengthMaxProp;
    private SerializedProperty auditionRoundDurationProp;
    private SerializedProperty auditionPerfectZoneStartProp;
    private SerializedProperty auditionPerfectZoneEndProp;
    private SerializedProperty auditionPerfectMultiplierProp;
    private SerializedProperty auditionGreatMultiplierProp;
    private SerializedProperty auditionMissMultiplierProp;
    private SerializedProperty effectProp;
    private SerializedProperty roundEffectProp;
    private SerializedProperty animationPlayCountProp;
    private SerializedProperty fxPlayCountProp;
    private SerializedProperty skillSpriteProp;
    private SerializedProperty fxPrefabProp;
    private SerializedProperty projectilePrefabProp;
    private SerializedProperty manaCostProp;
    private SerializedProperty rageCostProp;
    private SerializedProperty hpCostPercentProp;
    private SerializedProperty gemTypesAffectedProp;
    private SerializedProperty boardEffectTypeProp;
    private SerializedProperty animationDurationProp;

    private void OnEnable()
    {
        skillIdProp = serializedObject.FindProperty("skillId");
        skillNameProp = serializedObject.FindProperty("skillName");
        desSkillProp = serializedObject.FindProperty("desSkill");
        attackTypeProp = serializedObject.FindProperty("attackType");
        rangeTypeProp = serializedObject.FindProperty("rangeType");
        typeSkillProp = serializedObject.FindProperty("typeSkill");
        hitCountProp = serializedObject.FindProperty("hitCount");
        hitDelayProp = serializedObject.FindProperty("hitDelay");
        damageMultiplierProp = serializedObject.FindProperty("damageMultiplier");
        auditionSequenceLengthMinProp = serializedObject.FindProperty("auditionSequenceLengthMin");
        auditionSequenceLengthMaxProp = serializedObject.FindProperty("auditionSequenceLengthMax");
        auditionRoundDurationProp = serializedObject.FindProperty("auditionRoundDuration");
        auditionPerfectZoneStartProp = serializedObject.FindProperty("auditionPerfectZoneStart");
        auditionPerfectZoneEndProp = serializedObject.FindProperty("auditionPerfectZoneEnd");
        auditionPerfectMultiplierProp = serializedObject.FindProperty("auditionPerfectMultiplier");
        auditionGreatMultiplierProp = serializedObject.FindProperty("auditionGreatMultiplier");
        auditionMissMultiplierProp = serializedObject.FindProperty("auditionMissMultiplier");
        effectProp = serializedObject.FindProperty("effect");
        roundEffectProp = serializedObject.FindProperty("roundEffect");
        animationPlayCountProp = serializedObject.FindProperty("animationPlayCount");
        fxPlayCountProp = serializedObject.FindProperty("fxPlayCount");
        skillSpriteProp = serializedObject.FindProperty("skillSprite");
        fxPrefabProp = serializedObject.FindProperty("fxPrefab");
        projectilePrefabProp = serializedObject.FindProperty("projectilePrefab");
        manaCostProp = serializedObject.FindProperty("manaCost");
        rageCostProp = serializedObject.FindProperty("rageCost");
        hpCostPercentProp = serializedObject.FindProperty("hpCostPercent");
        gemTypesAffectedProp = serializedObject.FindProperty("gemTypesAffected");
        boardEffectTypeProp = serializedObject.FindProperty("boardEffectType");
        animationDurationProp = serializedObject.FindProperty("animationDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawIdentity();
        DrawDelivery();
        DrawDamage();
        DrawAudition();
        DrawStatus();
        DrawPlays();
        DrawVisual();
        DrawCost();
        DrawBoard();
        DrawTiming();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentity()
    {
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(skillIdProp);
        EditorGUILayout.PropertyField(skillNameProp);
        EditorGUILayout.PropertyField(desSkillProp);
        EditorGUILayout.Space();
    }

    private void DrawDelivery()
    {
        EditorGUILayout.LabelField("Delivery", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(attackTypeProp);
        EditorGUILayout.PropertyField(rangeTypeProp);
        EditorGUILayout.PropertyField(typeSkillProp);
        EditorGUILayout.Space();
    }

    private void DrawDamage()
    {
        EditorGUILayout.LabelField("Damage", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(hitCountProp);
        EditorGUILayout.PropertyField(hitDelayProp);
        EditorGUILayout.PropertyField(damageMultiplierProp);
        EditorGUILayout.Space();
    }

    private void DrawAudition()
    {
        SkillType typeSkill = (SkillType)typeSkillProp.enumValueIndex;
        if (typeSkill != SkillType.Audition)
            return;

        EditorGUILayout.LabelField("Audition", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(auditionSequenceLengthMinProp);
        EditorGUILayout.PropertyField(auditionSequenceLengthMaxProp);
        EditorGUILayout.PropertyField(auditionRoundDurationProp);
        EditorGUILayout.Slider(auditionPerfectZoneStartProp, 0f, 1f);
        EditorGUILayout.Slider(auditionPerfectZoneEndProp, 0f, 1f);
        EditorGUILayout.PropertyField(auditionPerfectMultiplierProp);
        EditorGUILayout.PropertyField(auditionGreatMultiplierProp);
        EditorGUILayout.PropertyField(auditionMissMultiplierProp);
        EditorGUILayout.Space();
    }

    private void DrawStatus()
    {
        EditorGUILayout.LabelField("Status Effect", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effectProp);
        EditorGUILayout.PropertyField(roundEffectProp);
        EditorGUILayout.Space();
    }

    private void DrawPlays()
    {
        EditorGUILayout.LabelField("Plays", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(animationPlayCountProp);
        EditorGUILayout.PropertyField(fxPlayCountProp);
        EditorGUILayout.Space();
    }

    private void DrawVisual()
    {
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(skillSpriteProp);
        EditorGUILayout.PropertyField(fxPrefabProp);
        EditorGUILayout.PropertyField(projectilePrefabProp);
        EditorGUILayout.Space();
    }

    private void DrawCost()
    {
        EditorGUILayout.LabelField("Cost", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(manaCostProp);
        EditorGUILayout.PropertyField(rageCostProp);
        EditorGUILayout.PropertyField(hpCostPercentProp);
        EditorGUILayout.Space();
    }

    private void DrawBoard()
    {
        EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(gemTypesAffectedProp, true);
        EditorGUILayout.PropertyField(boardEffectTypeProp);
        EditorGUILayout.Space();
    }

    private void DrawTiming()
    {
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(animationDurationProp);
    }
}
