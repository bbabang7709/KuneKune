/// <summary>
/// 상호작용 키를 일정 시간 동안 유지해야 하는 객체가 구현하는 인터페이스입니다.
/// 
/// 기본적인 <see cref="IInteractable"/>의 상호작용 기능을 포함하며,
/// 키를 유지하는 동안의 진행 상태와 키를 놓았을 때의 처리를 추가로 제공합니다.
/// 
/// 예:
/// - 아이템 획득
/// - 문을 일정 시간 동안 밀기
/// - 특정 물체를 조사하기 위해 버튼을 길게 누르기
/// </summary>
public interface IHoldInteractable : IInteractable
{
    /// <summary>
    /// 상호작용 키를 유지해야 하는 총 시간입니다.
    /// 단위는 초(second)입니다.
    /// </summary>
    float HoldDuration { get; }

    /// <summary>
    /// 현재 Hold 진행률입니다.
    /// 0부터 1 사이의 값을 사용합니다.
    /// 
    /// 0   = 시작하지 않음
    /// 0.5 = 50% 진행
    /// 1   = 완료
    /// </summary>
    float HoldProgress { get; }

    /// <summary>
    /// 상호작용 키를 누르고 있는 동안 호출됩니다.
    /// </summary>
    void OnInteractHeld();

    /// <summary>
    /// 상호작용 키를 놓았을 때 호출됩니다.
    /// 
    /// Hold가 완료되지 않았다면 일반적으로 진행률을 초기화하는 데 사용합니다.
    /// </summary>
    void OnInteractReleased();
}