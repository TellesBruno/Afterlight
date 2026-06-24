using UnityEngine;

/// <summary>
/// Marks a zone as restricted. While the player is inside, enemies with a
/// line-of-sight to the player will become hostile.
///
/// Setup: add to any GameObject with a BoxCollider (Is Trigger: true).
/// Place a CombatManager in the scene.
/// </summary>
public class RestrictedArea : MonoBehaviour
{
    void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<PlayerInputHandler>() != null && CombatManager.Instance != null)
            CombatManager.Instance.PlayerInRestrictedArea = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInputHandler>() != null && CombatManager.Instance != null)
            CombatManager.Instance.PlayerInRestrictedArea = false;
    }
}
