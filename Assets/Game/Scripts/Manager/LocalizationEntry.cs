using System;

[Serializable]
public class LocalizationEntry
{
    public string id;
    public string en;
    public string jp;
    public string kr;
    public string cn;
    public string vn;

    public string GetByLanguage(int languageIndex)
    {
        switch (languageIndex)
        {
            case 1: return jp;
            case 2: return kr;
            case 3: return cn;
            case 4: return vn;
            default: return en;
        }
    }

    public LocalizationEntry Clone()
    {
        return new LocalizationEntry
        {
            id = id,
            en = en,
            jp = jp,
            kr = kr,
            cn = cn,
            vn = vn
        };
    }
}
