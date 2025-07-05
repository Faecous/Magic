using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that holds a list of all available spell incantations in the game.
/// This serves as a single source of truth for voice recognition and UI population.
/// </summary>
[CreateAssetMenu(fileName = "IncantationList", menuName = "Spellcaster/Incantation List")]
public class IncantationList : ScriptableObject
{
    [Tooltip("The complete list of spell incantations recognized by the game.")]
    public List<string> incantations = new List<string>();
} 