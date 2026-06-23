using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Battle/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    [SerializeField] private List<SkillData> skills = new List<SkillData>();

    private readonly Dictionary<int, SkillData> skillById = new Dictionary<int, SkillData>();
    private bool cacheDirty = true;

    public IReadOnlyList<SkillData> Skills => skills;

    public SkillData GetSkillById(int skillId)
    {
        if (skillId <= 0)
            return null;

        EnsureCache();
        return skillById.TryGetValue(skillId, out SkillData skill) ? skill : null;
    }

    public void SetSkills(List<SkillData> source)
    {
        skills.Clear();

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SkillData skill = source[i];
                if (skill == null)
                    continue;

                skills.Add(skill);
            }
        }

        cacheDirty = true;
    }

    public void MarkDirty()
    {
        cacheDirty = true;
    }

    private void OnEnable()
    {
        cacheDirty = true;
    }

    private void OnValidate()
    {
        cacheDirty = true;
    }

    private void EnsureCache()
    {
        if (!cacheDirty)
            return;

        skillById.Clear();

        for (int i = 0; i < skills.Count; i++)
        {
            SkillData skill = skills[i];
            if (skill == null || skill.skillId <= 0)
                continue;

            skillById[skill.skillId] = skill;
        }

        cacheDirty = false;
    }
}
