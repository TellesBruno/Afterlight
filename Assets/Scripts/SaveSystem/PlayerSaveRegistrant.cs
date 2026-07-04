using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(WeaponManager))]
[RequireComponent(typeof(ClueInventory))]
[RequireComponent(typeof(QuestManager))]
[RequireComponent(typeof(ConsequenceTracker))]
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
        SaveManager.Instance?.RegisterQuestManager(GetComponent<QuestManager>());
        SaveManager.Instance?.RegisterConsequenceTracker(GetComponent<ConsequenceTracker>());
    }
}
