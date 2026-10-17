using UnityEngine;

public class SpriteCookAnimationController : MonoBehaviour
{
    private const string IdleState = "Idle";
    private const string WalkState = "Walk";
    private const string RunState = "Run";
    private const string JumpState = "Jump";
    private const string FallState = "Fall";
    private const string IdleDownState = "IdleDown";
    private const string IdleUpState = "IdleUp";
    private const string IdleRightState = "IdleRight";
    private const string WalkDownState = "WalkDown";
    private const string WalkUpState = "WalkUp";
    private const string WalkRightState = "WalkRight";
    private const string RunDownState = "RunDown";
    private const string RunUpState = "RunUp";
    private const string RunRightState = "RunRight";

    private Animator cachedAnimator;
    private string currentState = string.Empty;

    private void Awake()
    {
        cachedAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        SetIdle();
    }

    public void SetIdle() => PlayState(IdleState);
    public void SetWalk() => PlayState(WalkState);
    public void SetRun() => PlayState(RunState);
    public void SetJump() => PlayState(JumpState);
    public void SetFall() => PlayState(FallState);
    public void SetIdleDown() => PlayState(IdleDownState);
    public void SetIdleUp() => PlayState(IdleUpState);
    public void SetIdleRight() => PlayState(IdleRightState);
    public void SetWalkDown() => PlayState(WalkDownState);
    public void SetWalkUp() => PlayState(WalkUpState);
    public void SetWalkRight() => PlayState(WalkRightState);
    public void SetRunDown() => PlayState(RunDownState);
    public void SetRunUp() => PlayState(RunUpState);
    public void SetRunRight() => PlayState(RunRightState);

    private void PlayState(string stateName)
    {
        if (cachedAnimator == null || currentState == stateName)
        {
            return;
        }

        cachedAnimator.Play(stateName, 0, 0f);
        currentState = stateName;
    }
}
