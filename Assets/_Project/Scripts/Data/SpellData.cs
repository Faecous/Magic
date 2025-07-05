using UnityEngine;

/// <summary>
/// A ScriptableObject that defines all properties of a single spell,
/// linking the incantation, gesture, and resulting effects.
/// </summary>
[CreateAssetMenu(fileName = "New SpellData", menuName = "Spellcaster/Spell Data")]
public class SpellData : ScriptableObject
{
    [Tooltip("The voice command required to cast this spell. Must match an entry in VoiceSystemWrapper's spell list.")]
    public string incantation;

    [Tooltip("The gesture pattern required to cast this spell.")]
    public GesturePatternData gesturePattern;

    [Tooltip("The primary effect to apply when this spell is successfully cast.")]
    public SpellEffect primaryEffect;

    // TODO: Add fields for spell stats like cooldown, damage, speed, etc.
}

/// <summary>
/// Defines a text-based effect that can be displayed on screen.
/// </summary>
[System.Serializable]
public class SpellEffect
{
    [Tooltip("The text to display on screen when the spell is cast.")]
    public string message = "Magic!";
    
    [Tooltip("The font size of the message.")]
    public int fontSize = 24;
    
    [Tooltip("The duration in seconds the message will stay on screen.")]
    public float lifetime = 2.0f;

    [Tooltip("The position on the screen to display the message. (0,0) is bottom-left, (1,1) is top-right.")]
    public Vector2 screenPosition = new Vector2(0.5f, 0.5f);
} 