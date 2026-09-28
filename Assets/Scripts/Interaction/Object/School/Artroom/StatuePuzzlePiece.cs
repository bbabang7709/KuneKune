using System;
using UnityEngine;

public class StatuePuzzlePiece : InteractableBase
{
    public enum EDirection { Front, Left, Back, Right }

    [Header("Initial Direction Setting")]
    [Tooltip("시작 시 조각상 방향 설정")]
    public EDirection CurrentDirection;

    [Tooltip("방향별 스프라이트")]
    [SerializeField] private Sprite[] sprites;

    [Tooltip("회전 딜레이")]
    [SerializeField] private float rotationDelay = 1f;
    private float _timer = 0f;

    public Action OnRotated;

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        
        if (sprites.Length < 4) return;
        _sr.sprite = sprites[(int)CurrentDirection];
    }

    private void Update()
    {
        if (_timer <= 0f) return;

        _timer -= Time.deltaTime;
    }

    public override void OnInteractPressed()
    {
        RotateStatue();
    }   

    private void RotateStatue()
    {
        if (sprites.Length < 4) return;
        if (_timer > 0f) return;

        CurrentDirection = (EDirection)(((int)CurrentDirection + 1) % 4);
        _sr.sprite = sprites[(int)CurrentDirection];
        
        OnRotated?.Invoke();
        _timer = rotationDelay;
    }
}
