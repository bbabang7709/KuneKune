using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    [Tooltip("텔레포트 시 찾아올 고유 ID")]
    public string EntryID;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
