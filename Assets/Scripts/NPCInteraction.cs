using UnityEngine;

/// <summary>
/// Place on the root GameObject of an NPC that has a Trigger Collider.
/// When the player enters the trigger and presses Interact (E), the NPC's
/// body pivot rotates to face the player and the dialog screen opens.
///
/// Mirrors the interaction pattern of ClueTrigger.cs.
/// </summary>
public class NPCInteraction : MonoBehaviour
{
    [Tooltip("Dialog data asset for this NPC.")]
    [SerializeField] private NPCData npcData;

    [Tooltip("The transform that will rotate to face the player (e.g. the NPC body pivot). " +
             "If left empty, the NPC root transform is used.")]
    [SerializeField] private Transform bodyPivot;

    [Tooltip("ClueInventory on the player. Used to check required clues and grant new ones.")]
    [SerializeField] private ClueInventory clueInventory;

    [Tooltip("Direct reference to the DialogUI in the scene. Avoids tag search and works even when the object is inactive.")]
    [SerializeField] private DialogUI dialogUI;

    [Tooltip("Rotation offset (degrees) applied on top of LookRotation. Adjust if the NPC model's forward axis is not +Z. Common values: 0, 90, 180, -90.")]
    [SerializeField] private float bodyFacingOffset = 0f;

    private PlayerInputHandler _playerInput;
    private Transform _playerTransform;

    void OnTriggerEnter(Collider other)
    {
        var handler = other.GetComponent<PlayerInputHandler>();
        if (handler == null) return;

        _playerInput     = handler;
        _playerTransform = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInputHandler>() == null) return;

        _playerInput     = null;
        _playerTransform = null;
    }

    void Update()
    {
        if (_playerInput == null || !_playerInput.GetInteractDown()) return;

        StartDialog();
    }

    void StartDialog()
    {
        if (npcData == null)
        {
            Debug.LogWarning($"[NPCInteraction] No NPCData assigned on {gameObject.name}.");
            return;
        }

        RotateTowardPlayer();

        if (dialogUI == null)
        {
            Debug.LogWarning($"[NPCInteraction] DialogUI reference not assigned on {gameObject.name}.");
            return;
        }

        dialogUI.Open(npcData, clueInventory);
    }

    /// <summary>
    /// Rotates the body pivot to face the player on the XZ plane only,
    /// preserving the NPC's existing Y position and keeping it level.
    /// </summary>
    void RotateTowardPlayer()
    {
        if (_playerTransform == null) return;

        Transform pivot = bodyPivot != null ? bodyPivot : transform;

        Vector3 direction = _playerTransform.position - pivot.position;
        direction.y = 0f; // stay level — only rotate on Y axis

        if (direction.sqrMagnitude < 0.001f) return;

        pivot.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, bodyFacingOffset, 0f);
    }
}
