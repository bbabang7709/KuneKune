using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 대화 텍스트의 출력과 타이핑 연출을 담당한다.
/// 대화 진행 입력을 감지하여 타이핑 스킵 기능을 제공한다.
/// </summary>
public class DialogTextSystem : MonoBehaviour
{
    [Header("References")]

    /// <summary>
    /// 대화 텍스트를 표시할 TextMeshPro 컴포넌트.
    /// </summary>
    [SerializeField] private TextMeshProUGUI dialogText;

    /// <summary>
    /// 플레이어 입력을 관리하는 컴포넌트.
    /// </summary>
    [SerializeField] private PlayerInputHandler playerInput;

    [Header("Typing Settings")]

    /// <summary>
    /// 일반 문자의 타이핑 간격.
    /// </summary>
    [SerializeField] private float textSpeed = 0.03f;

    /// <summary>
    /// 쉼표 뒤에 적용할 추가 대기 시간.
    /// </summary>
    [SerializeField] private float commaDelay = 0.08f;

    /// <summary>
    /// 마침표, 느낌표, 물음표 뒤에 적용할 추가 대기 시간.
    /// </summary>
    [SerializeField] private float punctuationDelay = 0.2f;

    /// <summary>
    /// 말줄임표 뒤에 적용할 추가 대기 시간.
    /// </summary>
    [SerializeField] private float ellipsisDelay = 0.3f;

    /// <summary>
    /// 현재 텍스트를 타이핑 중인지 반환한다.
    /// </summary>
    public bool IsTyping { get; private set; }

    /// <summary>
    /// 현재 표시 중인 텍스트를 초기화한다.
    /// 새로운 대화가 시작될 때 이전 대사가 남아있지 않도록 한다.
    /// </summary>
    public void ClearText()
    {
        IsTyping = false;
        dialogText.text = string.Empty;
    }

    /// <summary>
    /// 주어진 대사를 한 글자씩 출력한다.
    /// 타이핑 중 Interact 입력이 들어오면 전체 문장을 즉시 출력한다.
    /// </summary>
    /// <param name="text">출력할 대사.</param>
    /// <returns>코루틴 Enumerator.</returns>
    public IEnumerator TypeText(string text)
    {
        ClearText();

        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        IsTyping = true;

        dialogText.text = text;

        dialogText.maxVisibleCharacters = 0;

        dialogText.ForceMeshUpdate();

        int totalVisibleCharacters = dialogText.textInfo.characterCount;
        int currentVisible = 0;

        while (currentVisible < totalVisibleCharacters)
        {
            if (playerInput.IsDialogAdvancePressed)
            {
                dialogText.maxVisibleCharacters = totalVisibleCharacters;
                IsTyping = false;

                yield return WaitForInputRelease();
                yield break;
            }

            currentVisible++;
            dialogText.maxVisibleCharacters = currentVisible;

            char lastCharacter = dialogText.textInfo.characterInfo[currentVisible - 1].character;

            yield return new WaitForSeconds(GetCharacterDelay(lastCharacter));
        }

        IsTyping = false;
    }

    /// <summary>
    /// 문자의 종류에 따라 다음 문자까지의 대기 시간을 결정한다.
    /// </summary>
    /// <param name="character">현재 출력한 문자.</param>
    /// <returns>다음 문자까지의 대기 시간.</returns>
    private float GetCharacterDelay(char character)
    {
        switch (character)
        {
            case ',':
                return commaDelay;

            case '.':
            case '!':
            case '?':
                return punctuationDelay;

            case '…':
                return ellipsisDelay;

            default:
                return textSpeed;
        }
    }

    /// <summary>
    /// Interact 버튼을 놓을 때까지 기다린다.
    /// 같은 버튼 입력이 다음 대화 진행으로 이어지는 것을 방지한다.
    /// </summary>
    private IEnumerator WaitForInputRelease()
    {
        yield return new WaitUntil(
            () => !playerInput.IsDialogAdvanceHeld
        );
    }
}