using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateSceneController : MonoBehaviour
{
    [SerializeField] private GameObject updatePetPanel;
    [SerializeField] private GameObject updateGemPanel;

    void Start()
    {
        int id = PlayerPrefs.GetInt("UpdatePetId", 0); // mặc định 0 nếu chưa có

        // Nếu id = 0 -> bật UpdatePet, tắt UpdateGem
        if (id == 0)
        {
            updatePetPanel.SetActive(true);
            updateGemPanel.SetActive(false);
        }
        else if (id == 1)
        {
            updatePetPanel.SetActive(false);
            updateGemPanel.SetActive(true);
        }
    }
}
