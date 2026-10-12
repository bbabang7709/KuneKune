using System.Collections.Generic;
using UnityEngine;

public class ControlLockManager : MonoBehaviour
{
    public static ControlLockManager Instance { get; private set; }

    private HashSet<object> _movementLocks = new HashSet<object>();
    private HashSet<object> _inputLocks = new HashSet<object>();

    public bool IsMovementLocked
    {
        get
        {
            CleanUpDeadLocks();
            return _movementLocks.Count > 0;
        }
    }
    public bool IsInputLocked
    {
        get
        {
            CleanUpDeadLocks();
            return _inputLocks.Count > 0;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 새로운 잠금이 추가된 즉시 호출됩니다. 다음 프레임의 갱신을 기다리지 않고 조작을 끊을 때 사용합니다.
    /// </summary>
    public event System.Action OnLockAdded;

    public void AddMovementLock(object source)
    {
        if (_movementLocks.Add(source))
            OnLockAdded?.Invoke();
    }

    public void RemoveMovementLock(object source)
    {
        _movementLocks.Remove(source);
    }

    public void AddInputLock(object source)
    {
        if (_inputLocks.Add(source))
            OnLockAdded?.Invoke();
    }

    public void RemoveInputLock(object source)
    {
        _inputLocks.Remove(source);
    }

    public void ClearAllLocks()
    {
        _inputLocks.Clear();
        _movementLocks.Clear();
    }

    private void CleanUpDeadLocks()
    {
        _movementLocks.RemoveWhere(IsDeadObject);
        _inputLocks.RemoveWhere(IsDeadObject);
    }

    private bool IsDeadObject(object source)
    {
        if (source is Object uObj)
        {
            return uObj == null;
        }

        return source == null;
    }

    private void OnGUI()
    {
        CleanUpDeadLocks();
        
        // 화면 좌측 상단에 빨간색 글씨로 표시
        GUILayout.BeginArea(new Rect(10, 10, 400, 400));
        GUI.color = Color.red;

        // (변수명 '_inputLocks'는 유저님이 쓰시는 List나 HashSet 이름으로 맞춰주세요!)
        GUILayout.Label($"[ 현재 묶여있는 조작 락 개수 : {_inputLocks.Count} ]");
        
        foreach(var locker in _inputLocks)
        {
            if (locker != null)
            {
                // 화면에 "범인: KuneKune" 또는 "범인: CutscenePlayerSwapper" 등으로 적나라하게 뜹니다.
                GUILayout.Label($"범인: {locker.GetType().Name}"); 
            }
        }
        
        GUILayout.EndArea();
    }
}
