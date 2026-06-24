using System.IO;
using UnityEngine;

/// <summary>
/// Singleton that persists across scenes. Handles saving and loading
/// game state (player position + collected clues) to/from a JSON file.
///
/// Place this on a dedicated "SaveManager" GameObject in your first scene.
/// Assign the references in the Inspector.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ClueInventory clueInventory;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private ClueRegistry clueRegistry;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponManager weaponManager;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Save()
    {
        if (!ValidateReferences()) return;

        var ammos = new System.Collections.Generic.List<WeaponAmmoSaveData>();
        for (int i = 0; i < weaponManager.SlotCount; i++)
        {
            var w = weaponManager.GetWeaponAtSlotIndex(i);
            if (w == null) continue;
            ammos.Add(new WeaponAmmoSaveData { slotIndex = i, currentAmmo = w.currentAmmo, totalAmmo = w.totalAmmo });
        }

        var allEnemies      = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var incapacitatedIds = new System.Collections.Generic.List<string>();
        foreach (var e in allEnemies)
            if (e.IsIncapacitated && !string.IsNullOrEmpty(e.EnemyId))
                incapacitatedIds.Add(e.EnemyId);

        var data = new SaveData
        {
            collectedClueIds      = clueInventory.GetCollectedIds(),
            playerPosition        = playerTransform.position,
            playerRotationY       = playerTransform.eulerAngles.y,
            playerHealth          = playerHealth.GetHealth(),
            weaponAmmos           = ammos.ToArray(),
            incapacitatedEnemyIds = incapacitatedIds.ToArray()
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] Game saved to {SavePath}");
    }

    public void Load()
    {
        if (!ValidateReferences()) return;

        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save file found. Starting fresh.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            Debug.LogWarning("[SaveManager] Save file could not be parsed.");
            return;
        }

        // Restore player transform
        playerTransform.position = data.playerPosition;
        Vector3 euler = playerTransform.eulerAngles;
        euler.y = data.playerRotationY;
        playerTransform.eulerAngles = euler;

        // Restore clues
        clueInventory.LoadCollectedIds(data.collectedClueIds, clueRegistry);

        // Sync ClueObject world state (enable/disable based on collected ids)
        SyncClueWorldState(data.collectedClueIds);

        // Restore incapacitated enemies
        SyncEnemyWorldState(data.incapacitatedEnemyIds);

        // Restore health
        playerHealth.LoadHealth(data.playerHealth);

        // Restore weapon ammo
        if (data.weaponAmmos != null)
        {
            foreach (var entry in data.weaponAmmos)
            {
                var w = weaponManager.GetWeaponAtSlotIndex(entry.slotIndex);
                if (w == null) continue;
                w.currentAmmo = entry.currentAmmo;
                w.totalAmmo   = entry.totalAmmo;
            }
        }

        Debug.Log($"[SaveManager] Game loaded from {SavePath}");
    }

    public void RegisterPlayer(Transform playerTransform, PlayerHealth playerHealth, WeaponManager weaponManager)
    {
        this.playerTransform = playerTransform;
        this.playerHealth    = playerHealth;
        this.weaponManager   = weaponManager;
    }

    public void RegisterClueInventory(ClueInventory clueInventory)
    {
        this.clueInventory = clueInventory;
    }

    public bool HasSaveFile() => File.Exists(SavePath);

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Debug.Log("[SaveManager] Save file deleted.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds every ClueTrigger in the scene (including disabled ones) and sets
    /// the parent ClueObject active or inactive to match the saved collected state.
    /// </summary>
    private void SyncClueWorldState(string[] collectedIds)
    {
        var collectedSet = new System.Collections.Generic.HashSet<string>(collectedIds ?? System.Array.Empty<string>());

        var allTriggers = FindObjectsByType<ClueTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var trigger in allTriggers)
        {
            if (trigger.transform.parent == null) continue;

            bool wasCollected = collectedSet.Contains(trigger.ClueId);
            trigger.transform.parent.gameObject.SetActive(!wasCollected);
        }

        Debug.Log($"[SaveManager] Synced {allTriggers.Length} ClueObject(s) to world state.");
    }

    /// <summary>
    /// Finds every EnemyHealth in the scene (including disabled ones) and calls
    /// ForceIncapacitate() on those whose ID appears in the saved list.
    /// Mirrors the SyncClueWorldState pattern.
    /// </summary>
    private void SyncEnemyWorldState(string[] incapacitatedIds)
    {
        var idSet      = new System.Collections.Generic.HashSet<string>(
            incapacitatedIds ?? System.Array.Empty<string>());
        var allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var e in allEnemies)
            if (idSet.Contains(e.EnemyId))
                e.ForceIncapacitate();

        Debug.Log($"[SaveManager] Synced {allEnemies.Length} enemy(s) to world state.");
    }

    private bool ValidateReferences()
    {
        if (clueInventory == null)   { Debug.LogError("[SaveManager] ClueInventory reference is missing.");   return false; }
        if (playerTransform == null) { Debug.LogError("[SaveManager] PlayerTransform reference is missing."); return false; }
        if (clueRegistry == null)    { Debug.LogError("[SaveManager] ClueRegistry reference is missing.");    return false; }
        if (playerHealth == null)    { Debug.LogError("[SaveManager] PlayerHealth reference is missing.");    return false; }
        if (weaponManager == null)   { Debug.LogError("[SaveManager] WeaponManager reference is missing.");   return false; }
        return true;
    }
}
