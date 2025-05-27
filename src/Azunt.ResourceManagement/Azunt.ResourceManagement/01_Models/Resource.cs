namespace Azunt.ResourceManagement;

/// <summary>
/// Resource 기본 클래스(Resource, ResourceModel, ResourceViewModel, ResourceDto, ...)
/// ResourcesViews(Resources 테이블) 뷰와 일대일로 매핑되는 모델 클래스
/// TODO: 계층형 로직 추가
/// </summary>
public class Resource
{
    /// <summary> 
    /// 일련번호
    /// </summary>
    public int Id { get; set; }

    /// <summary> 
    /// 리소스 이름(별칭): Notice, Free, News...
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// 경로(노선)
    /// </summary>
    public string? Route { get; set; }

    /// <summary> 
    /// 리소스 제목 : 공지사항
    /// </summary>
    public string? Title { get; set; }

    /// <summary> 
    /// 리소스 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary> 
    /// 시삽 UserId: 회원제 연동시 시삽 권한 부여
    /// </summary>
    public string? SysopUserId { get; set; }

    /// <summary> 
    /// 익명사용자(1) / 회원 전용(0) 리소스 구분
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary> 
    /// 그룹으로 묶어서 관리하고자 할 때
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary> 
    /// 그룹내 순서
    /// </summary>
    public int? GroupOrder { get; set; }

    /// <summary> 
    /// 전체 또는 앱 내에서의 리소스 표시 순서
    /// </summary>
    public int? DisplayOrder { get; set; } = 0;

    /// <summary> 
    /// 게시물 작성시 메일 전송 여부(현재는 사용 안함)
    /// </summary>
    public bool MailEnable { get; set; } = false;

    /// <summary> 
    /// 전체 리소스 리스트에서 보일건지 여부(특정 리소스은 관리자만 볼 수 있도록)
    /// </summary>
    public bool ShowList { get; set; } = true;

    /// <summary>
    /// Portal 메인 페이지에 요약 리소스로 출력할 지 여부
    /// </summary>
    public bool MainShowList { get; set; } = true;

    /// <summary>
    /// 리소스 상단에 포함될 HTML 조각
    /// </summary>
    public string? HeaderHtml { get; set; }

    /// <summary>
    /// 리소스 하단에 포함될 HTML 조각
    /// </summary>
    public string? FooterHtml { get; set; }

    /// <summary>
    /// 해당 리소스가 속한 애플리케이션 이름 (예: ReportWriter, AssetTracking 등)
    /// </summary>
    public string? AppName { get; set; } = "ReportWriter";

    /// <summary>
    /// 들여쓰기 수준(계층 구조 표현용, 0 = 루트, 1 = 서브 등)
    /// </summary>
    public int Step { get; set; } = 0;

    /// <summary>
    /// 생성자 ID 또는 이름
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 생성 일시 (타임존 포함)
    /// </summary>
    public DateTimeOffset? Created { get; set; }

    /// <summary>
    /// 수정자 ID 또는 이름
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// 마지막 수정 일시 (타임존 포함)
    /// </summary>
    public DateTimeOffset? Modified { get; set; }
}
