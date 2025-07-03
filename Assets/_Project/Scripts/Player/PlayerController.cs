using UnityEngine;

/// <summary>
/// Handles character locomotion and orientation.
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(InputManager))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2.0f;
    public float sprintSpeed = 5.0f;
    public float rotationSpeed = 4.0f;

    [Header("Animation")]
    public Animator animator;

    [Header("References")]
    private InputManager inputManager;
    private CharacterController characterController;
    private Transform mainCameraTransform;

    private float verticalVelocity;
    private readonly float gravity = -9.81f;

    private void Start()
    {
        // Get references to all the necessary components
        inputManager = GetComponent<InputManager>();
        characterController = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleAnimation();
        HandleGravity();
    }

    private void HandleMovement()
    {
        float targetSpeed = inputManager.IsSprinting ? sprintSpeed : moveSpeed;
        Vector3 moveDirection = new Vector3(inputManager.Move.x, 0, inputManager.Move.y);

        if (moveDirection == Vector3.zero)
        {
            targetSpeed = 0; // No input, no speed
        }

        // Move relative to the camera's direction
        Vector3 relativeMove = mainCameraTransform.forward * moveDirection.z + mainCameraTransform.right * moveDirection.x;
        relativeMove.y = 0; // Don't move up/down with camera angle

        characterController.Move(relativeMove.normalized * (targetSpeed * Time.deltaTime));
    }
    
    private void HandleRotation()
    {
        Vector3 moveDirection = new Vector3(inputManager.Move.x, 0, inputManager.Move.y);
        if (moveDirection == Vector3.zero) return;

        Vector3 relativeMove = mainCameraTransform.forward * moveDirection.z + mainCameraTransform.right * moveDirection.x;
        relativeMove.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(relativeMove);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleAnimation()
    {
        float speed = new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
        float animationSpeed = inputManager.IsSprinting ? speed / sprintSpeed : speed / moveSpeed;
        animator.SetFloat("Speed", animationSpeed);
        animator.SetBool("Grounded", characterController.isGrounded);
    }
    
    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = -2f; // A small downward force to keep it grounded
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }
}