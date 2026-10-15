using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RedTextScareManager : MonoBehaviour
{
    public static RedTextScareManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private RectTransform container;

    [Header("Scare Rhythm Settings")]
    [SerializeField] private string scareWord = "역겨워";
    [SerializeField] private int maxTextCount = 70;
    
    [Tooltip("초반에 느리게 나타날 글자 개수 ('띡 띡'에 해당)")]
    [SerializeField] private int slowSpawnCount = 2; 
    
    [Tooltip("초반 글자가 나타날 때의 대기 시간 (예: 0.5초)")]
    [SerializeField] private float slowDelay = 0.5f; 
    
    [Tooltip("이후 폭주하며 나타날 때의 대기 시간 ('띠리디리'에 해당, 아주 짧게)")]
    [SerializeField] private float fastDelay = 0.02f;

    private List<TMP_Text> _textPool = new List<TMP_Text>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < maxTextCount; i++)
        {
            TMP_Text txt = Instantiate(textPrefab, container);
            txt.text = scareWord;
            txt.color = new Color(0.8f, 0f, 0f, 1f); 
            txt.gameObject.SetActive(false);
            _textPool.Add(txt);
        }
    }

    public void PlayScareEffect()
    {
        StopAllCoroutines();
        StartCoroutine(FillScreenRoutine());
    }

    public void HideAll()
    {
        StopAllCoroutines();
        foreach (var txt in _textPool)
        {
            txt.gameObject.SetActive(false);
        }
    }

    private IEnumerator FillScreenRoutine()
    {
        float containerWidth = container.rect.width;
        float containerHeight = container.rect.height;

        for (int i = 0; i < _textPool.Count; i++)
        {
            var txt = _textPool[i];

            // 1. 랜덤 위치, 회전, 크기 세팅
            float randomX = Random.Range(-containerWidth / 2f, containerWidth / 2f);
            float randomY = Random.Range(-containerHeight / 2f, containerHeight / 2f);
            txt.rectTransform.anchoredPosition = new Vector2(randomX, randomY);

            float randomZ = Random.Range(-25f, 25f);
            txt.rectTransform.localRotation = Quaternion.Euler(0, 0, randomZ);

            float randomScale = Random.Range(0.8f, 3.5f);
            txt.rectTransform.localScale = Vector3.one * randomScale;

            // 2. 글자 켜기
            txt.gameObject.SetActive(true);

            // 💡 3. 리듬 분기점! (띡 띡 vs 띠리디리디리)
            if (i < slowSpawnCount)
            {
                // [띡] 사운드 재생 (예: 뼈 꺾이는 단발음)
                // SoundManager.Instance?.PlaySFX("Bone_Snap_Single");
                
                yield return new WaitForSecondsRealtime(slowDelay);
            }
            else
            {
                // [띠리디리] 사운드 재생 (예: 기괴한 연속 노이즈)
                // SoundManager.Instance?.PlaySFX("Meat_Squish_Fast");
                
                yield return new WaitForSecondsRealtime(fastDelay);
            }
        }
    }
}