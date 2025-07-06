using UnityEngine;

/// <summary>
/// A custom third-person camera controller that can switch between a locked-on follow state
/// and a free-cursor state for spellcasting.
/// </summary>
public class CustomCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform the camera will follow. This should be the player.")]
    [SerializeField] private Transform target;

    [Header("Camera Settings")]
    [Tooltip("The distance the camera will be from the target.")]
    [SerializeField] private float distance = 5.0f;
    [Tooltip("How quickly the camera rotates. Higher is faster.")]
    [SerializeField] private float rotationSpeed = 120.0f;
    [Tooltip("How smoothly the camera follows the target. Lower is smoother.")]
    [SerializeField] private float smoothSpeed = 0.125f;

    [Header("Camera Limits")]
    [Tooltip("The minimum angle the camera can look down to.")]
    [SerializeField] private float minYAngle = -30.0f;
    [Tooltip("The maximum angle the camera can look up to.")]
    [SerializeField] private float maxYAngle = 80.0f;

    [Header("Camera Offsets")]
    [Tooltip("The offset from the target's position where the camera will pivot around. This is great for an over-the-shoulder view.")]
    [SerializeField] private Vector3 pivotOffset = Vector3.zero;
    [Tooltip("An additional rotation to apply to the camera. Use the Y value for a yaw offset.")]
    [SerializeField] private Vector2 angleOffset = Vector2.zero;

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private bool isLocked = true;
    
    private InputManager inputManager;

    void Start()
    {
        // Find the InputManager in the scene using the modern FindFirstObjectByType method
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
        {
            Debug.LogError("CustomCameraController: Could not find an InputManager in the scene!", this);
            enabled = false;
            return;
        }

        // Initialize camera angles
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        // Make the rigid body not change rotation
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().freezeRotation = true;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        if(isLocked)
        {
            HandleLockedState();
        }
        else
        {
            HandleUnlockedState();
        }
    }

    private void HandleLockedState()
    {
        // Get look input from the manager
        Vector2 lookInput = inputManager.Look;
        currentX += lookInput.x * rotationSpeed * Time.deltaTime;
        currentY -= lookInput.y * rotationSpeed * Time.deltaTime;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // Calculate rotation including the offset
        Quaternion rotation = Quaternion.Euler(currentY + angleOffset.y, currentX + angleOffset.x, 0);

        // Calculate the pivot point by applying the offset to the target's position and orientation
        Vector3 pivotPoint = target.position + (target.right * pivotOffset.x) + (target.up * pivotOffset.y) + (target.forward * pivotOffset.z);

        // Calculate desired position based on the pivot and rotation
        Vector3 desiredPosition = pivotPoint - (rotation * Vector3.forward * distance);
        
        // Smoothly move the camera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Set the calculated rotation directly instead of using LookAt. 
        // This is crucial for correct camera-relative movement control.
        transform.rotation = rotation;
    }
    
    private void HandleUnlockedState()
    {
        // In the unlocked state, the camera does not respond to look input.
        // It maintains its current position and rotation relative to the player.
        // The cursor is free, controlled by GestureRecorder.
    }

    /// <summary>
    /// Locks the camera to the player and enables follow/rotation logic.
    /// Hides and locks the cursor.
    /// </summary>
    public void LockCamera()
    {
        isLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Unlocks the camera, disabling follow/rotation logic.
    /// Shows and unlocks the cursor for UI interaction or gesture drawing.
    /// </summary>
    public void UnlockCamera()
    {
        isLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
} 