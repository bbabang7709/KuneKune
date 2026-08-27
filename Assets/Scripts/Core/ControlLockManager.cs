using System.Collections.Generic;
using UnityEngine;

public class ControlLockManager : MonoBehaviour
{
    public static ControlLockManager Instance { get; private set; }

    private HashSet<object> _movementLocks = new HashSet<object>();
    private HashSet<object> _inputLocks = new HashSet<object>();

    public bool IsMovementLocked => _movementLocks.Count > 0;
    public bool IsInputLocked => _inputLocks.Count > 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddMovementLock(object source)
    {
        _movementLocks.Add(source);
    }

    public void RemoveMovementLock(object source)
    {
        _movementLocks.Remove(source);
    }

    public void AddInputLock(object source)
    {
        _inputLocks.Add(source);
    }

    public void RemoveInputLock(object source)
    {
        _inputLocks.Remove(source);
    }
}
