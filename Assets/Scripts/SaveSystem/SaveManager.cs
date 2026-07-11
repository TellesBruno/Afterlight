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

    /// <summary>Raised right after a save completes successfully.</summary>
    public event System.Action OnGameSaved;

    [Header("References")]
    [SerializeField] private ClueInventory clueInventory;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private ClueRegistry clueRegistry;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private ConsequenceTracker consequenceTracker;
    [SerializeField] private ConsequenceRegistry consequenceRegistry;

    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

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
            incapacitatedEnemyIds = incapacitatedIds.ToArray(),
            questStates           = questManager.GetQuestStates(),
            appliedConsequenceIds = consequenceTracker.GetAppliedIds()
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] Game saved to {SavePath}");

        OnGameSaved?.Invoke();
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

        // Suppress QuestManager's cascade evaluation for the whole restore: clues and
        // consequences are about to be reloaded and will re-fire their Added/Applied
        // events, but quest states haven't been restored yet at that point. Without this,
        // an already-Active quest could get evaluated (and even auto-complete/fail, with
        // real reward/consequence side effects) against that half-restored snapshot —
        // e.g. a quest whose Complete Condition happens to already be satisfied by the
        // reloaded clues, even though it hasn't reached its saved Active state check yet.
        questManager.BeginRestore();

        // Restore player transform. CharacterController sweeps from its own internally
        // cached position on every Move() call (see PlayerScript.HandleMovement, which
        // runs every frame); setting transform.position directly while it's enabled
        // doesn't reset that cache, so its very next Move() can sweep/resolve collisions
        // from the OLD position and shove the player back — barely noticeable over a
        // short teleport, but very visible over a long one. Disabling the component
        // forces it to fully re-sync to the new position instead of fighting it.
        var controller = playerTransform.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;

        playerTransform.position = data.playerPosition;
        Vector3 euler = playerTransform.eulerAngles;
        euler.y = data.playerRotationY;
        playerTransform.eulerAngles = euler;

        if (controller) controller.enabled = true;

        // Restore clues
        clueInventory.LoadCollectedIds(data.collectedClueIds, clueRegistry);

        // Restore hidden consequences and quest states.
        consequenceTracker.LoadAppliedIds(data.appliedConsequenceIds, consequenceRegistry);
        questManager.LoadQuestStates(data.questStates);

        // Ends the restore window and performs the one evaluation pass that matters —
        // against the now fully-restored clue/consequence/quest state — in case a
        // condition was met but never got evaluated during the session that produced
        // this save (e.g. a missed trigger).
        questManager.EndRestore();

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

    public void RegisterQuestManager(QuestManager questManager)
    {
        this.questManager = questManager;
    }

    public void RegisterConsequenceTracker(ConsequenceTracker consequenceTracker)
    {
        this.consequenceTracker = consequenceTracker;
    }

    /// <summary>
    /// Static and instance-independent on purpose: whether a save file exists on disk
    /// has nothing to do with the SaveManager singleton being alive. Callers that run
    /// before any scene containing a SaveManager has ever loaded (e.g. a "Delete Save"
    /// button on a SceneLoader in an earlier scene like Intro/MainMenu) must still be
    /// able to check/delete the save file without depending on SaveManager.Instance.
    /// </summary>
    public static bool HasSaveFile() => File.Exists(SavePath);

    public static void DeleteSave()
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
        if (clueInventory == null)       { Debug.LogError("[SaveManager] ClueInventory reference is missing.");       return false; }
        if (playerTransform == null)     { Debug.LogError("[SaveManager] PlayerTransform reference is missing.");     return false; }
        if (clueRegistry == null)        { Debug.LogError("[SaveManager] ClueRegistry reference is missing.");        return false; }
        if (playerHealth == null)        { Debug.LogError("[SaveManager] PlayerHealth reference is missing.");        return false; }
        if (weaponManager == null)       { Debug.LogError("[SaveManager] WeaponManager reference is missing.");       return false; }
        if (questManager == null)        { Debug.LogError("[SaveManager] QuestManager reference is missing.");        return false; }
        if (consequenceTracker == null)  { Debug.LogError("[SaveManager] ConsequenceTracker reference is missing.");  return false; }
        if (consequenceRegistry == null) { Debug.LogError("[SaveManager] ConsequenceRegistry reference is missing."); return false; }
        return true;
    }
}
