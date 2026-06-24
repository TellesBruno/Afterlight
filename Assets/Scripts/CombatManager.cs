using UnityEngine;

/// <summary>
/// Scene-level singleton that tracks global combat state.
/// Place on a dedicated "CombatManager" GameObject in each gameplay scene.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    /// <summary>True while the player is inside any RestrictedArea trigger.</summary>
    public bool PlayerInRestrictedArea { get; set; }

    /// <summary>True while at least one enemy is in the Hostile state.</summary>
    public bool IsInCombat => _hostileCount > 0;

    private int _hostileCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Called by EnemyAI when an enemy transitions to the Hostile state.</summary>
    public void RegisterHostile() => _hostileCount++;

    /// <summary>Called by EnemyAI when a hostile enemy is incapacitated.</summary>
    public void UnregisterHostile() => _hostileCount = Mathf.Max(0, _hostileCount - 1);
}
