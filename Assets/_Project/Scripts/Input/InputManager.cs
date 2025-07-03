using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Centralizes all raw player input and broadcasts game-specific events.
/// This script receives messages from the PlayerInput component.
/// </summary>
public class InputManager : MonoBehaviour
{
    // Public properties that other scripts can read from
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool IsSprinting { get; private set; }

    // Events for push-to-talk recording
    public event Action OnStartRecording;
    public event Action OnStopRecording;

    // These function names MUST match the names in the Input Actions asset
    // (e.g., "Move", "Look", "Sprint")

    private void OnRecord(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("InputManager: 'Record' button pressed. Firing OnStartRecording event.");
            OnStartRecording?.Invoke();
        }
        else
        {
            Debug.Log("InputManager: 'Record' button released. Firing OnStopRecording event.");
            OnStopRecording?.Invoke();
        }
    }

    private void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    private void OnLook(InputValue value)
    {
        Look = value.Get<Vector2>();
    }

    private void OnSprint(InputValue value)
    {
        IsSprinting = value.isPressed;
    }
}