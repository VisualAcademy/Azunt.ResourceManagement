namespace Azunt.ResourceManagement;

/// <summary>
/// ResourceSeeder 동작을 제어하기 위한 구성 옵션
/// </summary>
public class ResourceSeederOptions
{
    /// <summary>
    /// Seeder 실행 여부 (기본값: true)
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// 시드할 AppName 목록 (null 또는 빈 배열이면 전체 시드)
    /// </summary>
    public List<string>? AppNames { get; set; }
}
