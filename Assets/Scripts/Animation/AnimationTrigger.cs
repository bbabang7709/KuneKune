using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class AnimationTrigger : MonoBehaviour
{
    [SerializeField] private string _triggerName;
    private bool _hasTriggered;
    private Animator _anim;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasTriggered) return;

        if (collision.CompareTag("Player"))
        {
            _hasTriggered = true;
            if (_anim != null) _anim.SetTrigger(_triggerName);
        }
    }
}
