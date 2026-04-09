using UnityEngine;

public class PetSetupPopup : MonoBehaviour
{
    private GameObject currentPet;

    public void SpawnPet(GameObject petPrefab, Transform spawnPoint)
    {
        if (currentPet != null)
        {
            Destroy(currentPet);
        }

        currentPet = Instantiate(petPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    public void ClosePopup()
    {
        if (currentPet != null)
        {
            Destroy(currentPet);
        }

        gameObject.SetActive(false);
    }
}
