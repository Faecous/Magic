using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A singleton manager responsible for displaying spell effect text on the screen using the OnGUI system.
/// </summary>
public class SpellDisplayManager : MonoBehaviour
{
    public static SpellDisplayManager Instance { get; private set; }

    private readonly List<ActiveDisplayMessage> activeMessages = new List<ActiveDisplayMessage>();

    /// <summary>
    /// A helper class to track an active on-screen message and its properties.
    /// </summary>
    private class ActiveDisplayMessage
    {
        public string Text;
        public Vector2 Position;
        public int FontSize;
        public float Lifetime;
        public float StartTime;
    }

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: if you want it to persist between scenes
        }
    }

    /// <summary>
    /// Displays the text from a SpellEffect on the screen.
    /// </summary>
    /// <param name="effect">The SpellEffect data to display.</param>
    public void DisplaySpellEffect(SpellEffect effect)
    {
        if (effect == null) return;

        activeMessages.Add(new ActiveDisplayMessage
        {
            Text = effect.message,
            Position = effect.screenPosition,
            FontSize = effect.fontSize,
            Lifetime = effect.lifetime,
            StartTime = Time.time
        });
    }

    /// <summary>
    /// Unity's OnGUI method, called for rendering and handling GUI events.
    /// </summary>
    private void OnGUI()
    {
        // Create a copy of the list to iterate over, allowing us to remove items from the original
        for (int i = activeMessages.Count - 1; i >= 0; i--)
        {
            var msg = activeMessages[i];

            // Check if the message's lifetime has expired
            if (Time.time > msg.StartTime + msg.Lifetime)
            {
                activeMessages.RemoveAt(i);
                continue;
            }

            // Set up the GUI style for this message
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = msg.FontSize;
            style.alignment = TextAnchor.MiddleCenter;

            // Calculate the screen position and size
            Vector2 size = style.CalcSize(new GUIContent(msg.Text));
            float x = msg.Position.x * Screen.width - (size.x / 2);
            float y = (1 - msg.Position.y) * Screen.height - (size.y / 2); // Invert Y for GUI coordinates

            // Draw the label
            GUI.Label(new Rect(x, y, size.x, size.y), msg.Text, style);
        }
    }
} 