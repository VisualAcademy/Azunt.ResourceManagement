using Azunt.Models.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.ResourceManagement;

/// <summary>
/// Resource 테이블을 위한 Dapper 기반 리포지토리 클래스입니다.
/// - 기본 CRUD
/// - 검색 및 페이징
/// - AppName별 조회 지원
/// </summary>
public class ResourceRepositoryDapper : IResourceRepository
{
    private readonly string _defaultConnectionString;
    private readonly ILogger<ResourceRepositoryDapper> _logger;

    public ResourceRepositoryDapper(string defaultConnectionString, ILoggerFactory loggerFactory)
    {
        _defaultConnectionString = defaultConnectionString;
        _logger = loggerFactory.CreateLogger<ResourceRepositoryDapper>();
    }

    private SqlConnection GetConnection(string? connectionString)
    {
        return new SqlConnection(connectionString ?? _defaultConnectionString);
    }

    public async Task<Resource> AddAsync(Resource model, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        var sql = @"
            INSERT INTO Resources 
                (Alias, Route, Title, Description, SysopUserId, IsPublic, GroupName, GroupOrder, MailEnable, ShowList, MainShowList, HeaderHtml, FooterHtml, AppName, Step)
            OUTPUT INSERTED.Id
            VALUES 
                (@Alias, @Route, @Title, @Description, @SysopUserId, @IsPublic, @GroupName, @GroupOrder, @MailEnable, @ShowList, @MainShowList, @HeaderHtml, @FooterHtml, @AppName, @Step)";

        var result = await conn.ExecuteScalarAsync(sql, model);
        model.Id = result != null ? Convert.ToInt32(result) : 0;

        return model;
    }

    public async Task<List<Resource>> GetAllAsync(string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        var sql = "SELECT * FROM Resources ORDER BY Id DESC";
        var list = await conn.QueryAsync<Resource>(sql);
        return list.ToList();
    }

    public async Task<Resource> GetByIdAsync(int id, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        var sql = "SELECT * FROM Resources WHERE Id = @Id";
        var model = await conn.QuerySingleOrDefaultAsync<Resource>(sql, new { Id = id });
        return model ?? new Resource();
    }

    public async Task<bool> UpdateAsync(Resource model, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        var sql = @"
            UPDATE Resources SET 
                Alias = @Alias,
                Route = @Route,
                Title = @Title,
                Description = @Description,
                SysopUserId = @SysopUserId,
                IsPublic = @IsPublic,
                GroupName = @GroupName,
                GroupOrder = @GroupOrder,
                MailEnable = @MailEnable,
                ShowList = @ShowList,
                MainShowList = @MainShowList,
                HeaderHtml = @HeaderHtml,
                FooterHtml = @FooterHtml,
                AppName = @AppName,
                Step = @Step
            WHERE Id = @Id";

        var rows = await conn.ExecuteAsync(sql, model);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        var sql = "DELETE FROM Resources WHERE Id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<ArticleSet<Resource, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex, int pageSize,
        string searchField, string searchQuery,
        string sortOrder, TParentIdentifier parentIdentifier,
        string? connectionString = null)
    {
        var all = await GetAllAsync(connectionString);
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? all
            : all.Where(r => (r.Title?.Contains(searchQuery) ?? false) || (r.Description?.Contains(searchQuery) ?? false)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Resource, int>(paged, filtered.Count);
    }

    public async Task<List<Resource>> GetByAppNameAsync(string appName, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        var sql = "SELECT * FROM Resources WHERE AppName = @AppName ORDER BY GroupOrder, Alias";
        var list = await conn.QueryAsync<Resource>(sql, new { AppName = appName });
        return list.ToList();
    }

    public async Task<ArticleSet<Resource, int>> GetByAppNameAsync<TParentIdentifier>(
        string appName,
        int pageIndex, int pageSize,
        string searchField, string searchQuery,
        string sortOrder, TParentIdentifier parentIdentifier,
        string? connectionString = null)
    {
        var all = await GetByAppNameAsync(appName, connectionString);
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? all
            : all.Where(r => (r.Title?.Contains(searchQuery) ?? false) || (r.Description?.Contains(searchQuery) ?? false)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Resource, int>(paged, filtered.Count);
    }

    public async Task<List<Resource>> GetAllByAppNameAsync(string appName, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        var sql = @"
        SELECT * 
        FROM Resources 
        WHERE AppName = @AppName
        ORDER BY GroupOrder ASC, Alias ASC";

        var list = await conn.QueryAsync<Resource>(sql, new { AppName = appName });
        return list.ToList();
    }

    public async Task<bool> MoveUpAsync(int id, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        await conn.OpenAsync();

        var current = await conn.QuerySingleOrDefaultAsync<Resource>(
            "SELECT * FROM Resources WHERE Id = @Id", new { Id = id });

        if (current == null || current.DisplayOrder == null) return false;

        var upper = await conn.QueryFirstOrDefaultAsync<Resource>(
            @"SELECT TOP 1 * FROM Resources
          WHERE AppName = @AppName AND DisplayOrder IS NOT NULL AND DisplayOrder < @DisplayOrder
          ORDER BY DisplayOrder DESC",
            new { AppName = current.AppName, DisplayOrder = current.DisplayOrder });

        if (upper == null || upper.DisplayOrder == null) return false;

        var swapSql = @"
        UPDATE Resources SET DisplayOrder = @UpperOrder WHERE Id = @CurrentId;
        UPDATE Resources SET DisplayOrder = @CurrentOrder WHERE Id = @UpperId;";

        var affected = await conn.ExecuteAsync(swapSql, new
        {
            CurrentId = current.Id,
            UpperId = upper.Id,
            CurrentOrder = current.DisplayOrder,
            UpperOrder = upper.DisplayOrder
        });

        return affected > 0;
    }

    public async Task<bool> MoveDownAsync(int id, string? connectionString = null)
    {
        await using var conn = GetConnection(connectionString);
        await conn.OpenAsync();

        var current = await conn.QuerySingleOrDefaultAsync<Resource>(
            "SELECT * FROM Resources WHERE Id = @Id", new { Id = id });

        if (current == null || current.DisplayOrder == null) return false;

        var lower = await conn.QueryFirstOrDefaultAsync<Resource>(
            @"SELECT TOP 1 * FROM Resources
          WHERE AppName = @AppName AND DisplayOrder IS NOT NULL AND DisplayOrder > @DisplayOrder
          ORDER BY DisplayOrder ASC",
            new { AppName = current.AppName, DisplayOrder = current.DisplayOrder });

        if (lower == null || lower.DisplayOrder == null) return false;

        var swapSql = @"
        UPDATE Resources SET DisplayOrder = @LowerOrder WHERE Id = @CurrentId;
        UPDATE Resources SET DisplayOrder = @CurrentOrder WHERE Id = @LowerId;";

        var affected = await conn.ExecuteAsync(swapSql, new
        {
            CurrentId = current.Id,
            LowerId = lower.Id,
            CurrentOrder = current.DisplayOrder,
            LowerOrder = lower.DisplayOrder
        });

        return affected > 0;
    }
}
