using UnityEngine;

/// <summary>
/// 현재 더위 게이지를 화면의 아지랑이 강도로 전달합니다.
/// 쿠네쿠네가 등장할 때 사용하는 글리치와는 별도로 동작합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class HeatScreenEffect : MonoBehaviour
{
    private static readonly int HeatId = Shader.PropertyToID("_Heat");

    [Header("Heat Screen Effect")]
    [SerializeField] private Material heatHazeMaterial;

    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        playerMovement.OnHeatChanged += UpdateHeatEffect;
        SetHeat(playerMovement.HeatRatio);
    }

    private void OnDisable()
    {
        playerMovement.OnHeatChanged -= UpdateHeatEffect;
        SetHeat(0f);
    }

    private void UpdateHeatEffect(float currentHeat, float maxHeat)
    {
        float ratio = maxHeat > 0f ? currentHeat / maxHeat : 0f;
        SetHeat(ratio);
    }

    private void SetHeat(float ratio)
    {
        if (heatHazeMaterial == null)
            return;

        heatHazeMaterial.SetFloat(HeatId, Mathf.Clamp01(ratio));
    }
}
