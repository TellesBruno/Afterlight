using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks hidden consequences applied to the player. Never surfaced in any
/// player-facing UI — used only to gate dialog options and quest conditions.
/// </summary>
public class ConsequenceTracker : MonoBehaviour
{
    /// <summary>Fired when a new consequence is successfully applied.</summary>
    public event Action<ConsequenceData> OnConsequenceApplied;

    private readonly HashSet<string> _appliedIds = new HashSet<string>();
    private readonly List<ConsequenceData> _appliedConsequences = new List<ConsequenceData>();

    public IReadOnlyList<ConsequenceData> AppliedConsequences => _appliedConsequences;

    /// <summary>Applies a consequence. Does nothing if already applied.</summary>
    public void ApplyConsequence(ConsequenceData consequence)
    {
        if (consequence == null) return;
        if (!_appliedIds.Add(consequence.id)) return; // already applied

        _appliedConsequences.Add(consequence);
        OnConsequenceApplied?.Invoke(consequence);
        Debug.Log($"[ConsequenceTracker] Consequence applied: {consequence.id}");
    }

    /// <summary>Returns true if the consequence with the given id has been applied.</summary>
    public bool HasConsequence(string id) => _appliedIds.Contains(id);

    // ── Save / Load ──────────────────────────────────────────────────────────

    /// <summary>Returns all applied consequence ids for serialization.</summary>
    public string[] GetAppliedIds()
    {
        string[] ids = new string[_appliedIds.Count];
        _appliedIds.CopyTo(ids);
        return ids;
    }

    /// <summary>Restores applied consequences from saved ids. Fires OnConsequenceApplied for each restored entry.</summary>
    public void LoadAppliedIds(string[] ids, ConsequenceRegistry registry)
    {
        if (ids == null || registry == null) return;

        _appliedIds.Clear();
        _appliedConsequences.Clear();

        foreach (var id in ids)
        {
            var consequence = registry.GetById(id);
            if (consequence == null)
            {
                Debug.LogWarning($"[ConsequenceTracker] Saved consequence id '{id}' not found in registry. Skipping.");
                continue;
            }
            _appliedIds.Add(id);
            _appliedConsequences.Add(consequence);
            OnConsequenceApplied?.Invoke(consequence);
        }
    }
}
