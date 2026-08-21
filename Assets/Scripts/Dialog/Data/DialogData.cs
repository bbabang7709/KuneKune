using UnityEngine;

[CreateAssetMenu(fileName = "New Dialog Data", menuName = "KuneKune/Dialog/Dialog Data")]
public class DialogData : ScriptableObject
{
    [TextArea(2, 5)]
    public string[] lines;
}
