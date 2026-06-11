using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(WeaponManager))]
[RequireComponent(typeof(ClueInventory))]
public class PlayerSaveRegistrant : MonoBehaviour
{
    void Awake()
    {
        SaveManager.Instance?.RegisterPlayer(
            transform,
            GetComponent<PlayerHealth>(),
            GetComponent<WeaponManager>()
        );
        SaveManager.Instance?.RegisterClueInventory(GetComponent<ClueInventory>());
    }
}
