using UnityEngine;

[CreateAssetMenu(fileName = "Match3Resource", menuName = "Match3/Sprite Resource")]
public class Match3Resource : ScriptableObject
{
    [System.Serializable]
    public class SpriteItem
    {
        public int id;
        public Sprite sprite;
    }

    public SpriteItem[] spriteItems = new SpriteItem[6];
}