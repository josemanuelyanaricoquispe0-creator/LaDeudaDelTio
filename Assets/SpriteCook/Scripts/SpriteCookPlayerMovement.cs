using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteCookAnimationController))]
public class SpriteCookPlayerMovement : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float jumpForce = 7f;
    public LayerMask groundLayers = ~0;
    public float groundCheckDistance = 0.08f;

    private Rigidbody2D cachedBody;
    private Collider2D cachedCollider;
    private SpriteRenderer cachedRenderer;
    private SpriteCookAnimationController animationController;
    private PlayerControlLock controlLock;

    private float horizontalInput;
    private bool jumpQueued;
    private bool sprintHeld;

    private void Awake()
    {
        cachedBody = GetComponent<Rigidbody2D>();
        cachedCollider = GetComponent<Collider2D>();
        cachedRenderer = GetComponent<SpriteRenderer>();
        animationController = GetComponent<SpriteCookAnimationController>();
        controlLock = GetComponent<PlayerControlLock>();
    }

    private void OnEnable()
    {
        if (controlLock != null)
            controlLock.StateChanged += ClearPendingInput;
        ClearPendingInput();
    }

    private void OnDisable()
    {
        if (controlLock != null)
            controlLock.StateChanged -= ClearPendingInput;
        ClearPendingInput();
    }

    public void ClearPendingInput()
    {
        horizontalInput = 0f;
        jumpQueued = false;
        sprintHeld = false;
    }

    private void Update()
    {
        if (controlLock != null && controlLock.BlocksInput)
        {
            ClearPendingInput();
            return;
        }
        ReadInput();
        UpdateFacing();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        if (controlLock != null && controlLock.BlocksInput)
        {
            ClearPendingInput();
            return;
        }
        ApplyMovement();
        ApplyJump();
    }

    private void ReadInput()
    {
        bool handled = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            handled = true;
            horizontalInput = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalInput += 1f;
            sprintHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpQueued = true;
            }
        }
#endif

        if (handled)
        {
            return;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        horizontalInput = Input.GetAxisRaw("Horizontal");
        sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpQueued = true;
        }
#else
        horizontalInput = 0f;
        sprintHeld = false;
#endif
    }

    private void ApplyMovement()
    {
        float speed = Mathf.Abs(horizontalInput) > 0.01f && sprintHeld ? runSpeed : walkSpeed;
        Vector2 velocity = cachedBody.linearVelocity;
        velocity.x = horizontalInput * speed;
        cachedBody.linearVelocity = velocity;
    }

    private void ApplyJump()
    {
        if (!jumpQueued)
        {
            return;
        }

        jumpQueued = false;
        if (!IsGrounded())
        {
            return;
        }

        Vector2 velocity = cachedBody.linearVelocity;
        velocity.y = jumpForce;
        cachedBody.linearVelocity = velocity;
    }

    private void UpdateFacing()
    {
        if (horizontalInput < -0.01f)
        {
            cachedRenderer.flipX = true;
        }
        else if (horizontalInput > 0.01f)
        {
            cachedRenderer.flipX = false;
        }
    }

    private void UpdateAnimationState()
    {
        if (animationController == null)
        {
            return;
        }

        bool grounded = IsGrounded();
        if (!grounded)
        {
            if (cachedBody.linearVelocity.y > 0.05f)
            {
                animationController.SetJump();
            }
            else
            {
                animationController.SetFall();
            }
            return;
        }

        if (Mathf.Abs(horizontalInput) <= 0.01f)
        {
            animationController.SetIdle();
            return;
        }

        if (sprintHeld)
        {
            animationController.SetRun();
        }
        else
        {
            animationController.SetWalk();
        }
    }

    private bool IsGrounded()
    {
        if (cachedCollider.IsTouchingLayers(groundLayers))
        {
            return true;
        }

        Bounds bounds = cachedCollider.bounds;
        Vector2 checkSize = new Vector2(bounds.size.x * 0.9f, Mathf.Max(0.05f, bounds.size.y * 0.12f));
        Vector2 checkCenter = new Vector2(bounds.center.x, bounds.min.y - (checkSize.y * 0.5f) - groundCheckDistance);
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, groundLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit != cachedCollider && !hit.isTrigger)
            {
                return true;
            }
        }

        return false;
    }
}
