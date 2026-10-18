using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Each system releases only its own lock. Never toggles movement or physics.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-300)]
public sealed class PlayerControlLock : MonoBehaviour
{
    private readonly HashSet<UnityEngine.Object> owners = new HashSet<UnityEngine.Object>();
    private Rigidbody2D body;
    private int releasedFrame = -1;

    public bool IsLocked => owners.Count > 0;
    // Consume the entire closing frame, regardless of the order of component Updates.
    public bool BlocksInput => IsLocked || Time.frameCount <= releasedFrame;
    public event Action StateChanged;

    private void Awake() => body = GetComponent<Rigidbody2D>();

    public bool Acquire(UnityEngine.Object owner)
    {
        if (owner == null || !owners.Add(owner))
            return false;

        StopHorizontalMotion();
        StateChanged?.Invoke();
        return true;
    }

    public bool Release(UnityEngine.Object owner)
    {
        // ReferenceEquals also permits cleanup of an owner already destroyed by Unity.
        if (ReferenceEquals(owner, null) || !owners.Remove(owner))
            return false;

        NotifyRelease();
        return true;
    }

    private void FixedUpdate()
    {
        if (BlocksInput)
            StopHorizontalMotion();
    }

    private void LateUpdate()
    {
        if (owners.RemoveWhere(owner => owner == null) > 0)
            NotifyRelease();
    }

    private void NotifyRelease()
    {
        if (!IsLocked)
            releasedFrame = Time.frameCount;
        StopHorizontalMotion();
        StateChanged?.Invoke();
    }

    private void StopHorizontalMotion()
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }
}
