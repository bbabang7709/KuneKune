using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID = "Player_Main";
    private PlayerAppearance _appearance;
    private PlayerMovement _movement;

    void Awake()
    {
        _appearance = GetComponent<PlayerAppearance>();
        _movement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        SaveManager.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.Unregister(this);
    }

    public string GetUniqueID() => uniqueID;

    public void SaveState(SaveData data)
    {
        data.playerAge = _appearance.CurrentAge;
        data.currentHeat = _movement.CurrentHeat;
    }

    public void LoadState(SaveData data)
    {
        _appearance?.SetAge(data.playerAge);

        float heat = data.currentHeat;
        if (MapManager.Instance != null && MapManager.Instance.CurrentMapType == EMapType.House_Hallway)
            heat = Mathf.Min(heat, 50f);

        _movement?.SetCurrentHeat(heat, isLoad: true);
    }
}
