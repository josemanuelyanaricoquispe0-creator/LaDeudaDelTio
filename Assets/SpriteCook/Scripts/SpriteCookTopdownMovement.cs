using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteCookAnimationController))]
public class SpriteCookTopdownMovement : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float runSpeed = 6f;

    private Rigidbody cachedBody;
    private SpriteRenderer cachedRenderer;
    private SpriteCookAnimationController animationController;
    private Vector2 moveInput;
    private bool sprintHeld;

    private void Awake()
    {
        cachedBody = GetComponent<Rigidbody>();
        cachedRenderer = GetComponent<SpriteRenderer>();
        animationController = GetComponent<SpriteCookAnimationController>();
    }

    private void Update()
    {
        ReadInput();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        Vector3 velocity = new Vector3(moveInput.x, 0f, moveInput.y);
        if (velocity.sqrMagnitude > 1f)
        {
            velocity.Normalize();
        }
        cachedBody.linearVelocity = velocity * (sprintHeld ? runSpeed : walkSpeed);
    }

    private void ReadInput()
    {
        bool handled = false;
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            handled = true;
            moveInput = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
            sprintHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
#endif
        if (handled) return;
#if ENABLE_LEGACY_INPUT_MANAGER
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        moveInput = Vector2.zero;
        sprintHeld = false;
#endif
    }

    private void UpdateAnimationState()
    {
        if (animationController == null)
        {
            return;
        }

        if (moveInput.sqrMagnitude <= 0.01f)
        {
            if (moveInput.y > 0.1f) animationController.SetIdleUp();
            else if (moveInput.x != 0f) animationController.SetIdleRight();
            else animationController.SetIdleDown();
            return;
        }

        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            cachedRenderer.flipX = moveInput.x < 0f;
            if (sprintHeld) animationController.SetRunRight();
            else animationController.SetWalkRight();
        }
        else if (moveInput.y > 0f)
        {
            cachedRenderer.flipX = false;
            if (sprintHeld) animationController.SetRunUp();
            else animationController.SetWalkUp();
        }
        else
        {
            cachedRenderer.flipX = false;
            if (sprintHeld) animationController.SetRunDown();
            else animationController.SetWalkDown();
        }
    }
}
