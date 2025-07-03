using UnityEngine;
using System.Collections;

/// <summary>
/// Handles push-to-talk voice recording and playback for testing purposes.
/// This will eventually be replaced by the Picovoice implementation.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VoiceSystemWrapper : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The maximum possible duration of a recording in seconds. This defines the size of the audio buffer.")]
    [SerializeField] private int maxRecordingSeconds = 10;
    [Tooltip("The sample rate for the recording. 16000 is common for voice recognition.")]
    [SerializeField] private int sampleRate = 16000;

    [Header("References")]
    [Tooltip("The InputManager instance to get input events from. Should be on the same GameObject.")]
    [SerializeField] private InputManager inputManager;

    private AudioSource audioSource;
    private string microphoneDevice;
    private AudioClip recordedClip;

    /// <summary>
    /// Unity's Awake method, called when the script instance is being loaded.
    /// We use this to get our component references and find a microphone.
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"VoiceSystem: Using microphone '{microphoneDevice}'.");
        }
        else
        {
            Debug.LogError("VoiceSystem: No microphone found! Voice recording will not work.");
            enabled = false; // Disable this component if no mic is available
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
            Debug.Log("VoiceSystem: Subscribing to InputManager events.");
            inputManager.OnStartRecording += StartRecording;
            inputManager.OnStopRecording += StopRecordingAndPlayback;
        }
        else
        {
            Debug.LogError("VoiceSystem: InputManager not assigned in the Inspector! Disabling component.", this);
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
            inputManager.OnStopRecording -= StopRecordingAndPlayback;
        }
    }

    /// <summary>
    /// Starts recording audio from the default microphone.
    /// Called by the OnStartRecording event from the InputManager.
    /// </summary>
    private void StartRecording()
    {
        if (Microphone.IsRecording(microphoneDevice)) return;

        Debug.Log("VoiceSystem: Started recording...");
        // Start recording. We use 'true' for loop so it doesn't stop after the max duration.
        // We will stop it manually when the button is released.
        recordedClip = Microphone.Start(microphoneDevice, true, maxRecordingSeconds, sampleRate);
    }

    /// <summary>
    /// Stops the microphone recording and plays the captured audio clip back.
    /// Called by the OnStopRecording event from the InputManager.
    /// </summary>
    private void StopRecordingAndPlayback()
    {
        Debug.Log("VoiceSystem: StopRecordingAndPlayback method entered.");

        if (!Microphone.IsRecording(microphoneDevice))
        {
            Debug.LogWarning("VoiceSystem: Stop requested, but microphone wasn't recording.", this);
            return;
        }

        Debug.Log("VoiceSystem: Microphone is recording, proceeding to stop.");

        // Capture the position before we end the recording
        int position = Microphone.GetPosition(microphoneDevice);
        Debug.Log($"VoiceSystem: Captured audio position at {position} samples.");

        Microphone.End(microphoneDevice);
        Debug.Log("VoiceSystem: Microphone.End() called. Recording has officially stopped.");

        if (position <= 0)
        {
            Debug.LogWarning("VoiceSystem: No audio was recorded (position is 0). Nothing to play.", this);
            return;
        }

        if (recordedClip != null)
        {
            Debug.Log("VoiceSystem: Preparing trimmed clip for playback.");
            // Create a new audio clip with the correct length to trim silence
            float[] soundData = new float[position * recordedClip.channels];
            recordedClip.GetData(soundData, 0);

            AudioClip trimmedClip = AudioClip.Create("RecordedSample", position, recordedClip.channels, sampleRate, false);
            trimmedClip.SetData(soundData, 0);

            audioSource.clip = trimmedClip;
            
            Debug.Log("VoiceSystem: Starting playback...");
            audioSource.Play();
            StartCoroutine(LogWhenPlaybackFinished(trimmedClip.length));
        }
        else
        {
            Debug.LogWarning("VoiceSystem: Recorded clip was null, nothing to play back.", this);
        }
    }

    /// <summary>
    /// A small coroutine that waits for the length of the audio clip and then logs a message.
    /// </summary>
    /// <param name="clipLength">The duration of the clip in seconds.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator LogWhenPlaybackFinished(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        Debug.Log("VoiceSystem: Playback finished.");
    }
} 