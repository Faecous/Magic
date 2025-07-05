using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Loads all SpellData assets from the project into a central database at runtime.
/// This creates a global, searchable database of all available spells, keyed by their incantation.
/// </summary>
public class SpellbookManager : MonoBehaviour
{
    public static SpellbookManager Instance { get; private set; }
    
    public Dictionary<string, SpellData> SpellDatabase { get; private set; } = new Dictionary<string, SpellData>();

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: if you want it to persist between scenes

        LoadAllSpells();
    }

    private void LoadAllSpells()
    {
        // Find all assets of type SpellData in the project
        var allSpells = Resources.FindObjectsOfTypeAll<SpellData>();
        
        foreach (var spell in allSpells)
        {
            if (!SpellDatabase.ContainsKey(spell.incantation))
            {
                SpellDatabase.Add(spell.incantation, spell);
            }
            else
            {
                Debug.LogWarning($"SpellbookManager: Duplicate incantation found for '{spell.incantation}'. The spell '{spell.name}' will be ignored.");
            }
        }
        
        Debug.Log($"SpellbookManager: Loaded {SpellDatabase.Count} spells into the database.");
    }
} 