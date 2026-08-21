using System.Collections;
using UnityEngine;

public class Locker : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isMyLocker = false;
    [SerializeField] private DialogData defaultDialog;
    [SerializeField] private DialogData acquiredDialog;

    [SerializeField] private GameObject passwordUIRoot;
    private PasswordUI passwordUI;

    [SerializeField] private GameObject shovel;

    private Transform dropPoint;
    private bool hasAcquired = false;

    void Awake()
    {
        if (isMyLocker)
        {
            dropPoint = transform.Find("Drop Point");
            if (dropPoint == null) dropPoint = transform;
        }

        if (shovel != null)
        {
            shovel.SetActive(false);
        }

        if (passwordUIRoot != null)
        {
            passwordUI = passwordUIRoot.GetComponent<PasswordUI>();
            passwordUIRoot.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (isMyLocker && passwordUI != null)
        {
            passwordUI.OnCorrectPassword += OpenLocker;   
        }
    }
    void OnDisable()
    {
        if (isMyLocker && passwordUI != null)
        {
            passwordUI.OnCorrectPassword -= OpenLocker;
        }
    }

    public void OnInteractPressed()
    {
        if (isMyLocker)
        {
            if (hasAcquired)
            {
                DialogSystem.Instance?.PlayDialog(acquiredDialog);
            }
            else
            {
                StartCoroutine(MyLockerInteractionRoutine());    
            }
        }
        else
        {
            DialogSystem.Instance?.PlayDialog(defaultDialog);
        }
    }

    private IEnumerator MyLockerInteractionRoutine()
    {
        DialogSystem.Instance?.PlayDialog(defaultDialog);

        if (DialogSystem.Instance != null)
        {
            yield return new WaitWhile(() => DialogSystem.Instance.IsDialogPlaying);
        }

        if (passwordUIRoot != null)
        {
            passwordUIRoot.SetActive(true);
        }
    }

    private void OpenLocker()
    {
        passwordUI?.CloseUI();

        if (shovel != null && dropPoint != null)
        {
            shovel.transform.position = dropPoint.position;
            shovel.SetActive(true);
        }

        hasAcquired = true;
    }
}
