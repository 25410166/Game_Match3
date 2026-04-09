using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    private const string SCENE_HOME = "SceneHome";
    private const string SCENE_BATTLE = "SceneBattle";
    private const string SCENE_MAP = "SceneMap";
    private const string SCENE_SHOP = "SceneShop";
    private const string SCENE_UPDATE = "SceneUpdate";

    public void LoadHome() { LoadScene(SCENE_HOME); }
    public void LoadBattle() { LoadScene(SCENE_BATTLE); }
    public void LoadMap() { LoadScene(SCENE_MAP); }
    public void LoadShop() { LoadScene(SCENE_SHOP); }

    public void LoadUpdatePetWithId(int id)
    {
        PlayerPrefs.SetInt("UpdatePetId", id);
        PlayerPrefs.Save();
        LoadScene(SCENE_UPDATE);
    }

    private void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void Awake()
    {
       
    }
}
