using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using Pv;
using System; // For Action event

/// <summary>
/// Handles voice recording and real-time speech-to-text transcription using Picovoice Cheetah.
/// It captures audio, processes it via Cheetah, and attempts to match the transcribed text
/// to a known list of spells.
/// </summary>
public class VoiceSystemWrapper : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The maximum possible duration of a recording in seconds. This defines the size of the audio buffer.")]
    [SerializeField] private int maxRecordingSeconds = 10;
    [Tooltip("The sample rate for the recording. Cheetah requires 16000.")]
    [SerializeField] private int sampleRate = 16000;
    [Tooltip("Flag to enable automatic punctuation in the transcription.")]
    [SerializeField] private bool enableAutomaticPunctuation = true;

    [Header("Picovoice Model")]
    [Tooltip("The name of the Cheetah model file (e.g., cheetah_params.pv) located in the StreamingAssets folder.")]
    [SerializeField] private string modelFileName = "cheetah_params.pv";

    [Header("References")]
    [Tooltip("The InputManager instance to get input events from. Should be on the same GameObject.")]
    [SerializeField] private InputManager inputManager;
    [Tooltip("The ScriptableObject containing the list of all available spell incantations.")]
    [SerializeField] private IncantationList availableIncantations;

    // Picovoice
    private const string accessKey = "vq+xFG+dokKVeGnvlLaL8NYVC7WXY6/y0Pg9v1XtCMTLV6uqJa9Geg=="; // [[memory:2182885]]
    private Cheetah cheetah;
    private Coroutine processAudioCoroutine;
    private readonly StringBuilder transcribedTextBuilder = new StringBuilder();

    // Events for other systems to subscribe to
    public event Action<string> OnIncantationRecognized;
    public event Action OnIncantationFailed;

    // Microphone
    private string microphoneDevice;
    private AudioClip recordedClip;
    
    // Spell Logic is now driven by the IncantationList asset
    // private List<string> spellList;

    /// <summary>
    /// Unity's Awake method, called when the script instance is being loaded.
    /// We use this to get our component references, find a microphone, and set up our spell list.
    /// </summary>
    private void Awake()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            UnityEngine.Debug.Log($"VoiceSystem: Using microphone '{microphoneDevice}'.");
        }
        else
        {
            UnityEngine.Debug.LogError("VoiceSystem: No microphone found! Voice recording will not work.");
            enabled = false; // Disable this component if no mic is available
            return;
        }
        
        // InitializeSpellList(); // No longer needed, list comes from the asset
        InitializeCheetah();
    }

    /// <summary>
    /// Populates the list of known spells.
    /// In a production scenario, this would likely be loaded from a ScriptableObject or data file.
    /// </summary>
    // private void InitializeSpellList() ... This is now handled by the IncantationList asset

    /// <summary>
    /// Creates and initializes the Cheetah instance. This is a heavy operation and should only be done once.
    /// </summary>
    private void InitializeCheetah()
    {
        UnityEngine.Debug.Log("VoiceSystem: Initializing Cheetah...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        try
        {
            string modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);
            cheetah = Cheetah.Create(
                accessKey,
                modelPath: modelPath,
                enableAutomaticPunctuation: enableAutomaticPunctuation);
            
            stopwatch.Stop();
            UnityEngine.Debug.Log($"<color=orange>VoiceSystem: Cheetah initialized in {stopwatch.ElapsedMilliseconds}ms.</color>");
            UnityEngine.Debug.Log($"VoiceSystem: Cheetah version {cheetah.Version} initialized.");
        }
        catch (CheetahException ex)
        {
            UnityEngine.Debug.LogError($"VoiceSystem: Failed to create Cheetah instance: {ex.Message}");
            enabled = false;
        }
    }

    /// <summary>
    /// Unity's OnEnable method, called when the component becomes active.
    /// We subscribe to our input events here.
    /// </summary>
    private void OnEnable()
    {
        if (inputManager != null)
        {
            UnityEngine.Debug.Log("VoiceSystem: Subscribing to InputManager events.");
            inputManager.OnStartRecording += StartRecording;
            inputManager.OnStopRecording += StopRecordingAndProcess;
        }
        else
        {
            UnityEngine.Debug.LogError("VoiceSystem: InputManager not assigned in the Inspector! Disabling component.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Unity's OnDisable method, called when the component becomes inactive.
    /// We must unsubscribe from events here to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnStartRecording -= StartRecording;
            inputManager.OnStopRecording -= StopRecordingAndProcess;
        }
        
        // Ensure we stop and clean up if the object is disabled mid-recording
        if (Microphone.IsRecording(microphoneDevice))
        {
            StopRecordingAndProcess();
        }
        else if (cheetah != null)
        {
            // Cleanup if somehow the component is disabled after cheetah is created but before recording starts
            cheetah.Dispose();
            cheetah = null;
        }
    }

    /// <summary>
    /// Unity's OnDestroy method, called when the component is being destroyed.
    /// Ensures that we clean up the Cheetah instance to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (cheetah != null)
        {
            cheetah.Dispose();
            cheetah = null;
        }
    }

    /// <summary>
    /// Starts recording audio and initializes the Cheetah engine for transcription.
    /// Called by the OnStartRecording event from the InputManager.
    /// </summary>
    private void StartRecording()
    {
        if (cheetah == null)
        {
            UnityEngine.Debug.LogError("VoiceSystem: StartRecording called, but Cheetah instance is not ready.");
            return;
        }
        if (Microphone.IsRecording(microphoneDevice)) return;

        UnityEngine.Debug.Log("VoiceSystem: Started recording...");

        transcribedTextBuilder.Clear();
        recordedClip = Microphone.Start(microphoneDevice, true, maxRecordingSeconds, sampleRate);
        
        processAudioCoroutine = StartCoroutine(ProcessAudio());
    }

    /// <summary>
    /// Stops the microphone recording, finalizes the transcription, and checks for a spell match.
    /// Called by the OnStopRecording event from the InputManager.
    /// </summary>
    private void StopRecordingAndProcess()
    {
        if (!Microphone.IsRecording(microphoneDevice))
        {
            if (cheetah != null)
            {
                 UnityEngine.Debug.LogWarning("VoiceSystem: Stop requested, but microphone wasn't recording.");
            }
            return;
        }

        UnityEngine.Debug.Log("VoiceSystem: Stopping recording and processing final transcript...");

        if(processAudioCoroutine != null)
        {
            StopCoroutine(processAudioCoroutine);
            processAudioCoroutine = null;
        }

        Microphone.End(microphoneDevice);
        
        if (cheetah != null)
        {
            CheetahTranscript finalTranscriptObj = cheetah.Flush();
            if (!string.IsNullOrEmpty(finalTranscriptObj.Transcript))
            {
                transcribedTextBuilder.Append(finalTranscriptObj.Transcript);
            }

            string finalTranscript = transcribedTextBuilder.ToString().Trim();
            UnityEngine.Debug.Log($"VoiceSystem: Final Transcript: '{finalTranscript}'");
            
            CheckForSpellMatch(finalTranscript);
        }
        
        // No longer disposing cheetah here, just clearing the text and clip
        transcribedTextBuilder.Clear();
        recordedClip = null;
    }

    /// <summary>
    /// Coroutine that continuously processes audio from the microphone and feeds it to Cheetah.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator ProcessAudio()
    {
        int lastPosition = 0;
        int frameLength = cheetah.FrameLength;
        List<short> pcmBuffer = new List<short>();

        while (Microphone.IsRecording(microphoneDevice))
        {
            int currentPosition = Microphone.GetPosition(microphoneDevice);
            if (currentPosition < lastPosition)
            {
                // The microphone loop has wrapped around the buffer.
                // Process the first part from lastPosition to the end of the clip.
                ProcessChunk(lastPosition, recordedClip.samples, pcmBuffer, frameLength);
                lastPosition = 0;
            }

            if (currentPosition > lastPosition)
            {
                // Process the part from the last position to the current position.
                ProcessChunk(lastPosition, currentPosition, pcmBuffer, frameLength);
                lastPosition = currentPosition;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Extracts a chunk of audio data, converts it to PCM, and processes it with Cheetah.
    /// </summary>
    /// <param name="startPosition">The starting sample position in the audio clip.</param>
    /// <param name="endPosition">The ending sample position in the audio clip.</param>
    /// <param name="pcmBuffer">A buffer to store unprocessed PCM data between calls.</param>
    /// <param name="frameLength">The required frame length for Cheetah.</param>
    private void ProcessChunk(int startPosition, int endPosition, List<short> pcmBuffer, int frameLength)
    {
        int length = endPosition - startPosition;
        if (length <= 0) return;

        float[] samples = new float[length * recordedClip.channels];
        recordedClip.GetData(samples, startPosition);

        // Convert float samples to 16-bit PCM
        for (int i = 0; i < samples.Length; i++)
        {
            pcmBuffer.Add((short)(samples[i] * 32767));
        }

        // Process full frames of PCM data
        while (pcmBuffer.Count >= frameLength)
        {
            short[] frame = pcmBuffer.GetRange(0, frameLength).ToArray();
            pcmBuffer.RemoveRange(0, frameLength);

            try
            {
                CheetahTranscript transcriptObj = cheetah.Process(frame);
                if (!string.IsNullOrEmpty(transcriptObj.Transcript))
                {
                    transcribedTextBuilder.Append(transcriptObj.Transcript);
                    UnityEngine.Debug.Log($"VoiceSystem: Partial transcript: '{transcribedTextBuilder.ToString()}'");
                }
            }
            catch (CheetahException ex)
            {
                UnityEngine.Debug.LogError($"VoiceSystem: Cheetah processing error: {ex.Message}");
                // Stop the coroutine if a processing error occurs
                if (processAudioCoroutine != null) StopCoroutine(processAudioCoroutine);
            }
        }
    }

    private void CheckForSpellMatch(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            UnityEngine.Debug.Log("VoiceSystem: Transcript is empty, no spell matched.");
            return;
        }

        // Using OrdinalIgnoreCase for a case-insensitive comparison
        foreach (var spell in availableIncantations.incantations)
        {
            if (string.Equals(transcript, spell, System.StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Debug.Log($"<color=cyan>VoiceSystem: Matched Spell: {spell}!</color>");
                OnIncantationRecognized?.Invoke(spell);
                return;
            }
        }
        
        UnityEngine.Debug.Log($"VoiceSystem: No spell matched for transcript '{transcript}'.");
        OnIncantationFailed?.Invoke();
    }
} 