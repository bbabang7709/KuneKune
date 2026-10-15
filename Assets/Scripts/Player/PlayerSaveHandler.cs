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
        _movement?.SetCurrentHeat(data.currentHeat, isLoad: true);
    }
}
