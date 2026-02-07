using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public class InputReader : MonoBehaviour
{
    [Header("Input Setup")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string actionMapName = "Player";

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsJumpHeld { get; private set; }

    public bool JumpPressedThisFrame { get; private set; }
    public bool JumpAltPressedThisFrame { get; private set; }

    public event Action JumpPressed;
    public event Action JumpAltPressed;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction jumpAltAction;
    private InputAction sprintAction;
    private bool jumpAltTriggeredThisPress;

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        CacheActions();
    }

    private void OnEnable()
    {
        BindActions();
    }

    private void OnDisable()
    {
        UnbindActions();
        ResetInputState();
    }

    private void LateUpdate()
    {
        // One-frame signals for state machine transitions.
        JumpPressedThisFrame = false;
        JumpAltPressedThisFrame = false;
    }

    private void CacheActions()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError($"{nameof(InputReader)}: Missing PlayerInput or Actions asset.", this);
            return;
        }

        InputActionMap map = playerInput.actions.FindActionMap(actionMapName, false);
        if (map == null)
        {
            Debug.LogError($"{nameof(InputReader)}: Action map '{actionMapName}' not found.", this);
            return;
        }

        moveAction = map.FindAction("Move", false);
        lookAction = map.FindAction("Look", false);
        jumpAction = map.FindAction("Jump", false);
        jumpAltAction = map.FindAction("JumpAlt", false);
        sprintAction = map.FindAction("Sprint", false);

        if (moveAction == null || lookAction == null || jumpAction == null || jumpAltAction == null || sprintAction == null)
        {
            Debug.LogError($"{nameof(InputReader)}: One or more required actions are missing in map '{actionMapName}'.", this);
        }
    }

    private void BindActions()
    {
        if (moveAction == null || lookAction == null || jumpAction == null || jumpAltAction == null || sprintAction == null)
        {
            CacheActions();
        }

        if (moveAction == null)
        {
            return;
        }

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;

        lookAction.performed += OnLookPerformed;
        lookAction.canceled += OnLookCanceled;

        jumpAction.started += OnJumpStarted;
        jumpAction.canceled += OnJumpCanceled;
        jumpAltAction.performed += OnJumpAltPerformed;

        sprintAction.performed += OnSprintPerformed;
        sprintAction.canceled += OnSprintCanceled;
    }

    private void UnbindActions()
    {
        if (moveAction == null)
        {
            return;
        }

        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMoveCanceled;

        lookAction.performed -= OnLookPerformed;
        lookAction.canceled -= OnLookCanceled;

        jumpAction.started -= OnJumpStarted;
        jumpAction.canceled -= OnJumpCanceled;
        jumpAltAction.performed -= OnJumpAltPerformed;

        sprintAction.performed -= OnSprintPerformed;
        sprintAction.canceled -= OnSprintCanceled;
    }

    private void ResetInputState()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;
        IsSprinting = false;
        IsJumpHeld = false;
        jumpAltTriggeredThisPress = false;
        JumpPressedThisFrame = false;
        JumpAltPressedThisFrame = false;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        Move = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        Look = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        Look = Vector2.zero;
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        IsJumpHeld = true;
        jumpAltTriggeredThisPress = false;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        IsJumpHeld = false;

        if (!jumpAltTriggeredThisPress)
        {
            JumpPressedThisFrame = true;
            JumpPressed?.Invoke();
        }

        jumpAltTriggeredThisPress = false;
    }

    private void OnJumpAltPerformed(InputAction.CallbackContext context)
    {
        jumpAltTriggeredThisPress = true;
        JumpAltPressedThisFrame = true;
        JumpAltPressed?.Invoke();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        IsSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        IsSprinting = false;
    }
}
