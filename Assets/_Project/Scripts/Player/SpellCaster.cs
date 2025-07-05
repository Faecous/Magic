using UnityEngine;
using Magic.Gestures;

/// <summary>
/// Manages the spellcasting state machine and validation by orchestrating the
/// voice and gesture systems. This is the central conductor for the player's magic abilities.
/// </summary>
[RequireComponent(typeof(GestureRecorder))]
public class SpellCaster : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("How closely a drawn gesture must match the pattern to be considered successful (0-1 scale).")]
    [SerializeField] private float gestureMatchThreshold = 0.8f;
    
    [Header("Debug")]
    [Tooltip("Check this to print detailed gesture recognition scores to the console.")]
    [SerializeField] private bool debugGestureScore;

    // System References
    private InputManager inputManager;
    private VoiceSystemWrapper voiceSystem;
    private SpellbookManager spellbookManager;
    private GestureRecorder gestureRecorder;
    private Camera mainCamera;
    
    private void Awake()
    {
        // Component on self
        gestureRecorder = GetComponent<GestureRecorder>();
        
        // Find required systems
        inputManager = FindObjectOfType<InputManager>();
        voiceSystem = FindObjectOfType<VoiceSystemWrapper>();
        spellbookManager = FindObjectOfType<SpellbookManager>();
        mainCamera = Camera.main;

        // Validate that all systems were found
        if (inputManager == null || voiceSystem == null || spellbookManager == null)
        {
            Debug.LogError("SpellCaster could not find one or more required systems (InputManager, VoiceSystemWrapper, SpellbookManager) in the scene! Disabling component.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // The SpellCaster only needs to know when to start/stop the gesture part
        inputManager.OnStartRecording += gestureRecorder.StartRecording;
        inputManager.OnStopRecording += gestureRecorder.StopRecording;
        
        // The real magic happens when we get a result from the voice system
        voiceSystem.OnIncantationRecognized += HandleIncantationRecognized;
        voiceSystem.OnIncantationFailed += HandleIncantationFailed;
    }

    private void OnDisable()
    {
        inputManager.OnStartRecording -= gestureRecorder.StartRecording;
        inputManager.OnStopRecording -= gestureRecorder.StopRecording;

        voiceSystem.OnIncantationRecognized -= HandleIncantationRecognized;
        voiceSystem.OnIncantationFailed -= HandleIncantationFailed;
    }

    /// <summary>
    /// Called when the VoiceSystem successfully recognizes a known incantation.
    /// This is where the final gesture validation happens.
    /// </summary>
    private void HandleIncantationRecognized(string incantation)
    {
        Debug.Log($"SpellCaster: Voice recognized '{incantation}'. Now validating gesture...");
        
        // 1. Look up the spell in our database
        if (spellbookManager.SpellDatabase.TryGetValue(incantation, out SpellData spellData))
        {
            // 2. Recognize the recorded gesture against the spell's pattern
            float score = GestureRecognizer.Recognize(gestureRecorder.RecordedPath, spellData.gesturePattern, mainCamera, debugGestureScore);
            
            // 3. Check if the gesture was accurate enough
            if (score >= gestureMatchThreshold)
            {
                Debug.Log($"<color=green>SUCCESS: Spell '{spellData.incantation}' cast with gesture score {score:F2}!</color>");
                // TODO: Chamber the spell

                // Trigger the visual effect
                if (SpellDisplayManager.Instance != null && spellData.primaryEffect != null)
                {
                    SpellDisplayManager.Instance.DisplaySpellEffect(spellData.primaryEffect);
                }
            }
            else
            {
                Debug.Log($"<color=red>FIZZLE: Gesture for '{incantation}' failed with score {score:F2}.</color>");
                // TODO: Play fizzle effect
            }
        }
        else
        {
            Debug.LogError($"SpellCaster: Recognized incantation '{incantation}' but could not find it in the SpellbookManager!");
        }
    }

    /// <summary>
    /// Called when the VoiceSystem fails to recognize any known incantation.
    /// </summary>
    private void HandleIncantationFailed()
    {
        Debug.Log("<color=red>FIZZLE: Voice not recognized.</color>");
        // TODO: Play fizzle effect
    }
} 