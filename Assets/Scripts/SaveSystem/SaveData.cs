using System;
using UnityEngine;

[Serializable]
public class WeaponAmmoSaveData
{
    public int slotIndex;
    public int currentAmmo;
    public int totalAmmo;
}

[Serializable]
public class QuestSaveData
{
    public string id;
    public int state; // cast to/from QuestState
}

[Serializable]
public class SaveData
{
    public string[] collectedClueIds = Array.Empty<string>();
    public Vector3 playerPosition;
    public float playerRotationY;
    public float playerHealth = 100f;
    public WeaponAmmoSaveData[] weaponAmmos = Array.Empty<WeaponAmmoSaveData>();
    public string[] incapacitatedEnemyIds   = Array.Empty<string>();
    public QuestSaveData[] questStates      = Array.Empty<QuestSaveData>();
    public string[] appliedConsequenceIds   = Array.Empty<string>();
}
