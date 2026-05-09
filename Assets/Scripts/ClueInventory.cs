using System;
using System.Collections.Generic;
using UnityEngine;

public class ClueInventory : MonoBehaviour
{
    /// <summary>Fired when a new clue is successfully added to the inventory.</summary>
    public event Action<ClueData> OnClueAdded;

    private readonly HashSet<string> _collectedIds = new HashSet<string>();
    private readonly List<ClueData> _collectedClues = new List<ClueData>();

    public IReadOnlyList<ClueData> CollectedClues => _collectedClues;

    /// <summary>Adds a clue to the inventory. Does nothing if already collected.</summary>
    public void AddClue(ClueData clue)
    {
        if (clue == null) return;
        if (!_collectedIds.Add(clue.id)) return; // already collected

        _collectedClues.Add(clue);
        OnClueAdded?.Invoke(clue);
        Debug.Log($"[ClueInventory] Clue collected: {clue.id} — {clue.title}");
    }

    /// <summary>Returns true if the clue with the given id has been collected.</summary>
    public bool IsCollected(string id) => _collectedIds.Contains(id);

    // ── Save / Load ──────────────────────────────────────────────────────────

    /// <summary>Returns all collected clue ids for serialization.</summary>
    public string[] GetCollectedIds()
    {
        string[] ids = new string[_collectedIds.Count];
        _collectedIds.CopyTo(ids);
        return ids;
    }

    /// <summary>Restores inventory from saved ids. Fires OnClueAdded for each restored clue.</summary>
    public void LoadCollectedIds(string[] ids, ClueRegistry registry)
    {
        if (ids == null || registry == null) return;

        _collectedIds.Clear();
        _collectedClues.Clear();

        foreach (var id in ids)
        {
            var clue = registry.GetById(id);
            if (clue == null)
            {
                Debug.LogWarning($"[ClueInventory] Saved clue id '{id}' not found in registry. Skipping.");
                continue;
            }
            _collectedIds.Add(id);
            _collectedClues.Add(clue);
            OnClueAdded?.Invoke(clue);
        }
    }
}
