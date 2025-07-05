using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that defines the normalized sequence of points for a magical gesture.
/// The points should be centered around (0,0) and typically fit within a -0.5 to 0.5 range on both axes.
/// </summary>
[CreateAssetMenu(fileName = "New GesturePattern", menuName = "Spellcaster/Gesture Pattern Data")]
public class GesturePatternData : ScriptableObject
{
    [Tooltip("The ordered list of normalized 2D points that define the gesture's shape.")]
    public List<Vector2> waypoints = new List<Vector2>();
} 