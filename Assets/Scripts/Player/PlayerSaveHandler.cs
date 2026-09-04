using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID = "Player_Main";
    private PlayerController _playerController;
    private PlayerAppearance _playerAppearance;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
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
        data.playerAge = (int)_playerAppearance.CurrentAge;
        data.playerPosition = transform.position;
    }

    public void LoadState(SaveData data)
    {
        _playerAppearance.SetAge((EPlayerAge)data.playerAge);
        transform.position = data.playerPosition;
    }
}
