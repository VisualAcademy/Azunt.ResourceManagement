using Azunt.Models.Common;

namespace Azunt.ResourceManagement;

/// <summary>
/// 리소스 리포지토리 인터페이스
/// - 기본 CRUD 및 페이징/검색 기능 제공
/// - 특정 AppName에 속한 리소스 목록 조회 지원
/// </summary>
public interface IResourceRepository
{
    /// <summary>
    /// 리소스 추가
    /// </summary>
    Task<Resource> AddAsync(Resource model, string? connectionString = null);

    /// <summary>
    /// 모든 리소스 조회
    /// </summary>
    Task<List<Resource>> GetAllAsync(string? connectionString = null);

    /// <summary>
    /// 특정 리소스 조회
    /// </summary>
    Task<Resource> GetByIdAsync(int id, string? connectionString = null);

    /// <summary>
    /// 리소스 수정
    /// </summary>
    Task<bool> UpdateAsync(Resource model, string? connectionString = null);

    /// <summary>
    /// 리소스 삭제
    /// </summary>
    Task<bool> DeleteAsync(int id, string? connectionString = null);

    /// <summary>
    /// 리소스 검색 및 페이징 처리된 결과 조회
    /// </summary>
    Task<ArticleSet<Resource, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex, int pageSize,
        string searchField, string searchQuery,
        string sortOrder, TParentIdentifier parentIdentifier,
        string? connectionString = null);

    /// <summary>
    /// AppName에 속한 리소스 목록 조회
    /// </summary>
    Task<List<Resource>> GetByAppNameAsync(string appName, string? connectionString = null);

    /// <summary>
    /// AppName에 속한 리소스 검색 및 페이징 처리된 결과 조회
    /// </summary>
    Task<ArticleSet<Resource, int>> GetByAppNameAsync<TParentIdentifier>(
        string appName,
        int pageIndex, int pageSize,
        string searchField, string searchQuery,
        string sortOrder, TParentIdentifier parentIdentifier,
        string? connectionString = null);

    /// <summary>
    /// AppName 기준으로 정렬되지 않은 전체 리소스 목록
    /// </summary>
    Task<List<Resource>> GetAllByAppNameAsync(string appName, string? connectionString = null);

    /// <summary>
    /// DisplayOrder 상향 이동
    /// </summary>
    Task<bool> MoveUpAsync(int id, string? connectionString = null);

    /// <summary>
    /// DisplayOrder 하향 이동
    /// </summary>
    Task<bool> MoveDownAsync(int id, string? connectionString = null);
}
