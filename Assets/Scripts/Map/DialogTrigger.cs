using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class DialogTrigger : MonoBehaviour
{
    [SerializeField] private DialogData dialog;
    private bool hasPlayed = false;

    [Tooltip("(선택) 지정하면 이 애니메이션이 끝난 뒤에 다이얼로그를 출력한다")]
    [SerializeField] private AnimationDialogSequence preAnimation;

    public UnityEvent OnDialogFinished;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasPlayed) return;

        if (collision.CompareTag("Player"))
        {
            hasPlayed = true;

            if (preAnimation != null)
                preAnimation.PlayAnimation(PlayDialog);
            else
                PlayDialog();
        }
    }

    private void PlayDialog()
    {
        if (DialogSystem.Instance != null)
        {
            DialogSystem.Instance.PlayDialog(dialog, () => OnDialogFinished?.Invoke());
        }
    }
}
