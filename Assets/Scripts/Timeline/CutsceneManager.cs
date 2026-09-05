using System;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    public bool IsCutscenePlaying { get; private set; }

    public event Action OnCutsceneStart;
    public event Action OnCutsceneEnd;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }   

        Instance = this;
    }

    public void StartCutscene()
    {
        IsCutscenePlaying = true;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        OnCutsceneStart?.Invoke();
    }

    public void EndCutscene()
    {
        IsCutscenePlaying = false;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }

        OnCutsceneEnd?.Invoke();
    }
}
