using UnityEngine;

public class KuneSpawn : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float chaseSpeed = 3f;
    private bool _hasSpawned = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasSpawned) return;

        if (collision.CompareTag("Player"))
        {
            _hasSpawned = true;
            PlayerAppearance player = collision.GetComponent<PlayerAppearance>();
            if (player != null) player.SetAge(EPlayerAge.Child);
            
            KuneKune kune = MapManager.Instance?.KuneKune;
            if (kune != null) 
            {
                kune.SetMoveSpeed(chaseSpeed);
                kune.Spawn(spawnPoint.position, true);
            }
        }
    }
}
