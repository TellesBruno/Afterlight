using System.Collections.Generic;
using UnityEngine;

public class DummySpawner : MonoBehaviour
{
    [Tooltip("Prefab to instantiate at each spawn point")]
    public GameObject dummyPrefab;

    [Tooltip("Positions where dummies will be spawned")]
    public List<Transform> spawnPoints = new List<Transform>();

    private PlayerInputHandler _playerInput;

    void OnTriggerEnter(Collider other)
    {
        var handler = other.GetComponent<PlayerInputHandler>();
        if (handler != null)
            _playerInput = handler;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInputHandler>() != null)
            _playerInput = null;
    }

    void Update()
    {
        if (_playerInput == null || !_playerInput.GetInteractDown()) return;

        SpawnAll();
    }

    void SpawnAll()
    {
        if (dummyPrefab == null) return;

        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            Instantiate(dummyPrefab, point.position, point.rotation);
        }
    }
}
