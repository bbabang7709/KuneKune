using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID = "Player_Main";
    private PlayerAppearance _playerAppearance;

    void Awake()
    {
        _playerAppearance = GetComponent<PlayerAppearance>();
    }

    void Start()
    {
        SaveManager.Instance?.Register(this);
    }

    private void OnDestory()
    {
        SaveManager.Instance?.Unregister(this);
    }

    public string GetUniqueID() => uniqueID;

    public void SaveState(SaveData data)
    {
        data.playerAge = _playerAppearance.CurrentAge;
    }

    public void LoadState(SaveData data)
    {
        _playerAppearance?.SetAge(data.playerAge);
    }
}
