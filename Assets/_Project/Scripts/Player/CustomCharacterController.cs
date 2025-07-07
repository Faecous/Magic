using UnityEngine;

/// <summary>
/// Handles character locomotion and orientation based on player input
/// and camera direction. This is a custom controller to replace the StarterAssets one.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(InputManager))]
public class CustomCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The character's regular movement speed.")]
    [SerializeField] private float moveSpeed = 4.0f;
    [Tooltip("The character's speed when sprinting.")]
    [SerializeField] private float sprintSpeed = 6.0f;
    [Tooltip("How quickly the character rotates to face the movement direction. Higher is faster.")]
    [SerializeField] private float rotationSpeed = 10.0f;

    [Header("Movement Smoothing")]
    [Tooltip("How quickly the character speeds up while walking. A higher value creates a snappier response.")]
    [SerializeField] private float walkAcceleration = 10f;
    [Tooltip("How quickly the character speeds up while sprinting. A higher value creates a snappier response.")]
    [SerializeField] private float sprintAcceleration = 15f;
    [Tooltip("How quickly the character slows down. A higher value means a faster stop.")]
    [SerializeField] private float deceleration = 20f;

    [Header("Jumping")]
    [Tooltip("The height the character can jump in meters.")]
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Animation")]
    [Tooltip("The Animator component for the character model.")]
    [SerializeField] private Animator animator;

    // --- Component References ---
    private InputManager inputManager;
    private CharacterController characterController;
    private Transform mainCameraTransform;

    // --- Private State ---
    private float verticalVelocity;
    private Vector3 currentHorizontalVelocity;
    private bool wasGrounded;
    private bool isAwaitingJumpForce;
    private readonly float gravity = -9.81f;
    
    // --- Animator Hashes ---
    // Caching these improves performance
    private static readonly int AnimIDGrounded = Animator.StringToHash("Grounded");
    private static readonly int AnimIDForwardSpeed = Animator.StringToHash("ForwardSpeed");
    private static readonly int AnimIDRightSpeed = Animator.StringToHash("RightSpeed");
    private static readonly int AnimIDJump = Animator.StringToHash("Jump");
    private static readonly int AnimIDIsMoving = Animator.StringToHash("IsMoving");

    private void Start()
    {
        // Get references to all the necessary components
        inputManager = GetComponent<InputManager>();
        characterController = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // --- Handle Rotation FIRST ---
        // We align the character's rotation with the camera's Y-axis rotation.
        // This makes the character always face where the camera is looking, enabling strafing.
        float cameraYaw = mainCameraTransform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // --- Calculate Horizontal Movement SECOND ---
        // Movement is now relative to the character's own orientation, which is aligned with the camera.
        float targetSpeed = inputManager.IsSprinting ? sprintSpeed : moveSpeed;
        Vector3 moveInput = new Vector3(inputManager.Move.x, 0f, inputManager.Move.y); // (strafe, forward/backward)
        Vector3 targetHorizontalVelocity = Vector3.zero;
        
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // Convert the local input vector (relative to player) into a world-space direction.
            Vector3 worldMoveDirection = transform.forward * moveInput.z + transform.right * moveInput.x;
            targetHorizontalVelocity = worldMoveDirection.normalized * targetSpeed;
        }

        // --- Smooth The Horizontal Velocity (Acceleration/Deceleration) ---
        float smoothRate;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // Player is giving input, so we accelerate.
            smoothRate = inputManager.IsSprinting ? sprintAcceleration : walkAcceleration;
        }
        else
        {
            // Player has no input, so we decelerate.
            smoothRate = deceleration;
        }
        currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetHorizontalVelocity, Time.deltaTime * smoothRate);

        // --- Handle Gravity and Jumping ---
        // This now happens before the final Move call.
        HandleGravityAndJump();
        Vector3 verticalVelocityVector = new Vector3(0, verticalVelocity, 0);

        // --- Apply Combined Movement ---
        // We combine horizontal and vertical velocities and apply them in a single .Move() call.
        characterController.Move((currentHorizontalVelocity + verticalVelocityVector) * Time.deltaTime);

        // --- Update Animation ---
        // We transform the world-space velocity into the character's local space
        // to get forward and right speed components for a 2D blend tree.
        Vector3 localVelocity = transform.InverseTransformDirection(currentHorizontalVelocity);
        bool isMoving = localVelocity.sqrMagnitude > 0.01f;
        HandleAnimation(localVelocity.z, localVelocity.x, isMoving);
        
        // Track our grounded state for the next frame's logic.
        wasGrounded = characterController.isGrounded;
    }

    /// <summary>
    /// Updates the Animator with the character's current state using local forward and right speeds.
    /// </summary>
    private void HandleAnimation(float forwardSpeed, float rightSpeed, bool isMoving)
    {
        if (animator == null) return;
        
        // Send values to the animator
        animator.SetFloat(AnimIDForwardSpeed, forwardSpeed);
        animator.SetFloat(AnimIDRightSpeed, rightSpeed);
        animator.SetBool(AnimIDGrounded, characterController.isGrounded);
        animator.SetBool(AnimIDIsMoving, isMoving);
    }
    
    /// <summary>
    /// Manages both gravity and the jump action. This consolidation prevents bugs where
    /// a jump is triggered incorrectly on the same frame that the character lands.
    /// </summary>
    private void HandleGravityAndJump()
    {
        if (characterController.isGrounded)
        {
            // When on the ground, our default vertical velocity is slightly negative to keep us "stuck".
            // We only change this when we jump.
            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            // Check for jump input. We only process this if we are NOT already waiting for a jump animation event.
            if (inputManager.JumpStartedThisFrame && !isAwaitingJumpForce)
            {
                // Trigger the animation and set the flag. The animation will call ApplyJumpForce() at the correct frame.
                animator.SetTrigger(AnimIDJump);
                isAwaitingJumpForce = true;
            }
        }
        else
        {
            // If we are in the air for any reason (jump, fall), we are no longer waiting for a jump event.
            // This is a safety reset in case the character walks off a ledge while a jump was pending.
            isAwaitingJumpForce = false;
            
            // We are in the air, apply gravity.
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
    
    /// <summary>
    /// This public method is called by an Animation Event at the precise moment
    /// the character should lift off the ground during a jump animation.
    /// </summary>
    public void ApplyJumpForce()
    {
        // A flag ensures this logic only runs when a jump has been initiated.
        if (!isAwaitingJumpForce) return;

        // The formula to reach a specific height is: sqrt(height * -2 * gravity)
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        
        // Reset the flag so this isn't called again accidentally.
        isAwaitingJumpForce = false;
    }
} 