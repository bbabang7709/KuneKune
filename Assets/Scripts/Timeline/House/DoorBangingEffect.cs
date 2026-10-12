using System.Collections;
using UnityEngine;

public class DoorBangingEffect : MonoBehaviour
{
    [Header("Knock Settings")]
    [Tooltip("문이 한 번 칠 때 흔들리는 강도")]
    public float shakeIntensity = 0.2f;
    [Tooltip("한 번 치는 액션이 유지되는 짧은 시간")]
    public float knockDuration = 0.05f;
    
    [Header("Rhythm Settings")]
    [Tooltip("쾅! 과 쾅! 사이의 간격 (연타 내부 속도)")]
    public float timeBetweenKnocks = 0.15f;
    [Tooltip("쾅쾅쾅! 이후 다음 쾅쾅쾅! 까지의 대기 시간")]
    public float timeBetweenBursts = 1.2f;

    [Header("Audio")]
    [Tooltip("문 치는 사운드 ID")]
    public string singleBangSound = "Door_Banging";

    private Vector3 _originalPos;
    private bool _isBanging = false;

    private void Awake()
    {
        _originalPos = transform.localPosition;
    }

    public void StartBanging()
    {
        // 중복 실행 방지
        if (_isBanging) return;
        
        _isBanging = true;
        StartCoroutine(BangingRoutine());
    }

    public void StopBanging()
    {
        _isBanging = false;
    }

    private IEnumerator BangingRoutine()
    {
        while (_isBanging)
        {
            // 1. 사운드 재생 (정확한 싱크)
            SoundManager.Instance?.PlaySFX(singleBangSound);
            if (!_isBanging) break;
            
            for (int i = 0; i < 4; i++)
            {
                // 2. 강렬한 타격 이동
                // 누군가 밀치는 느낌을 주기 위해 x축으로 강하게, y축으로 살짝 틀어줍니다.
                float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
                float offsetY = Random.Range(-shakeIntensity * 0.2f, shakeIntensity * 0.2f);
                transform.localPosition = _originalPos + new Vector3(offsetX, offsetY, 0f);

                // 타격 지속 시간
                yield return new WaitForSeconds(knockDuration);

                // 3. 원위치 복구
                transform.localPosition = _originalPos;

                // 마지막 타격이 아니면 다음 타격 전까지 짧은 대기
                if (i < 3)
                {
                    yield return new WaitForSeconds(timeBetweenKnocks);
                }
            }

            // 쾅쾅쾅 3연타가 끝난 후 소름 돋는 정적(Term)
            yield return new WaitForSeconds(timeBetweenBursts);
        }
        
        // 완전히 종료될 때 확실하게 원위치
        transform.localPosition = _originalPos;
    }
}