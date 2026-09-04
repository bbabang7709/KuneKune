
public interface ISaveable
{
    string GetUniqueID();
    void SaveState(SaveData data);
    void LoadState(SaveData data);    
}
