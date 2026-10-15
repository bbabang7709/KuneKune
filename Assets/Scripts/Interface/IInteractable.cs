/// <summary>
/// 플레이어가 상호작용할 수 있는 객체가 구현하는 기본 인터페이스입니다.
/// 
/// 일반적인 상호작용은 플레이어가 상호작용 키를 한 번 눌렀을 때
/// <see cref="OnInteractPressed"/>가 호출됩니다.
/// 
/// 예:
/// - 문 열기
/// - 조사하기
/// - 텍스트 출력
/// - 일러스트 출력
/// - 특수 이벤트 실행
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 상호작용 키를 눌렀을 때 호출됩니다.
    /// </summary>
    void OnInteractPressed();
}