using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{   
    public static UIManager Instance { get; private set; }
    [Header("Player Status")]
    [SerializeField] private Image staminaGuage;
    
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Dialog")]
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private TextMeshProUGUI dialogText;

    [Header("Full Screen Image")]
    [SerializeField] private GameObject fullScreenImageRoot;
    [SerializeField] private Image fullScreenImage;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        dialogRoot.SetActive(false);
        fullScreenImageRoot.SetActive(false);
    }

    void OnEnable()
    {
        playerMovement.OnStaminaChanged += UpdateStaminaUI;
    }

    void OnDisable()
    {
        playerMovement.OnStaminaChanged -= UpdateStaminaUI;
    }

    public void UpdateStaminaUI(float current, float max)
    {
        float ratio = current / max;
        staminaGuage.fillAmount = ratio;
    }

    public void PlayDialog(DialogData dialog)
    {
        if (dialog == null) return;

        StartCoroutine(ShowDialogRoutine(dialog));
    }

    private IEnumerator ShowDialogRoutine(DialogData dialog)
    {
        dialogRoot.SetActive(true);
        playerMovement.enabled = false;

        yield return null;

        foreach (string line in dialog.lines)
        {
            dialogText.text = line;

            yield return new WaitUntil(
                () => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)
            );
        }
        playerMovement.enabled = true;
        dialogRoot.SetActive(false);
    }

    public void PlayAcquireDialog(Sprite image, DialogData dialog, System.Action onFinished)
    {
        StartCoroutine(PlayItemAcquireRoutine(image, dialog, onFinished));
    }

    private IEnumerator PlayItemAcquireRoutine(Sprite image, DialogData dialog, System.Action onFinished)
    {
        playerMovement.enabled = false;
        if (image != null)
        {
            fullScreenImage.sprite = image;
            fullScreenImageRoot.SetActive(true);
        }

        if (dialog != null)
        {
            dialogRoot.SetActive(true);

            foreach (string line in dialog.lines)
            {
                dialogText.text = line;

                yield return new WaitUntil(
                    () => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)
                );
            }
            dialogRoot.SetActive(false);
        }
        fullScreenImageRoot.SetActive(false);
        playerMovement.enabled = true;
        onFinished?.Invoke();
    }
}
