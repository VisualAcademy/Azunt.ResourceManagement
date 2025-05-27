using Azunt.Models.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Azunt.ResourceManagement;

/// <summary>
/// Resource 테이블을 위한 ADO.NET 기반 리포지토리 클래스입니다.
/// - 기본 CRUD
/// - 검색 및 페이징
/// - AppName별 조회 지원
/// </summary>
public class ResourceRepositoryAdoNet : IResourceRepository
{
    private readonly string _defaultConnectionString;
    private readonly ILogger<ResourceRepositoryAdoNet> _logger;

    public ResourceRepositoryAdoNet(string defaultConnectionString, ILoggerFactory loggerFactory)
    {
        _defaultConnectionString = defaultConnectionString;
        _logger = loggerFactory.CreateLogger<ResourceRepositoryAdoNet>();
    }

    private SqlConnection GetConnection(string? connectionString)
    {
        return new SqlConnection(connectionString ?? _defaultConnectionString);
    }

    public async Task<Resource> AddAsync(Resource model, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Resources 
                (Alias, Route, Title, Description, SysopUserId, IsPublic, GroupName, GroupOrder, MailEnable, ShowList, MainShowList, HeaderHtml, FooterHtml, AppName, Step)
            OUTPUT INSERTED.Id
            VALUES 
                (@Alias, @Route, @Title, @Description, @SysopUserId, @IsPublic, @GroupName, @GroupOrder, @MailEnable, @ShowList, @MainShowList, @HeaderHtml, @FooterHtml, @AppName, @Step)";

        cmd.Parameters.AddWithValue("@Alias", model.Alias ?? string.Empty);
        cmd.Parameters.AddWithValue("@Route", model.Route ?? string.Empty);
        cmd.Parameters.AddWithValue("@Title", model.Title ?? string.Empty);
        cmd.Parameters.AddWithValue("@Description", model.Description ?? string.Empty);
        cmd.Parameters.AddWithValue("@SysopUserId", model.SysopUserId ?? string.Empty);
        cmd.Parameters.AddWithValue("@IsPublic", model.IsPublic);
        cmd.Parameters.AddWithValue("@GroupName", model.GroupName ?? string.Empty);
        cmd.Parameters.AddWithValue("@GroupOrder", model.GroupOrder ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MailEnable", model.MailEnable);
        cmd.Parameters.AddWithValue("@ShowList", model.ShowList);
        cmd.Parameters.AddWithValue("@MainShowList", model.MainShowList);
        cmd.Parameters.AddWithValue("@HeaderHtml", model.HeaderHtml ?? string.Empty);
        cmd.Parameters.AddWithValue("@FooterHtml", model.FooterHtml ?? string.Empty);
        cmd.Parameters.AddWithValue("@AppName", model.AppName ?? string.Empty);
        cmd.Parameters.AddWithValue("@Step", model.Step);

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        model.Id = result != null ? Convert.ToInt32(result) : 0;
        return model;
    }

    public async Task<List<Resource>> GetAllAsync(string? connectionString = null)
    {
        var result = new List<Resource>();
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Resources ORDER BY Id DESC";

        await conn.OpenAsync();
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(Map(reader));
        }
        return result;
    }

    public async Task<Resource> GetByIdAsync(int id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Resources WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Map(reader);
        }
        return new Resource();
    }

    public async Task<bool> UpdateAsync(Resource model, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
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

        cmd.Parameters.AddWithValue("@Alias", model.Alias ?? string.Empty);
        cmd.Parameters.AddWithValue("@Route", model.Route ?? string.Empty);
        cmd.Parameters.AddWithValue("@Title", model.Title ?? string.Empty);
        cmd.Parameters.AddWithValue("@Description", model.Description ?? string.Empty);
        cmd.Parameters.AddWithValue("@SysopUserId", model.SysopUserId ?? string.Empty);
        cmd.Parameters.AddWithValue("@IsPublic", model.IsPublic);
        cmd.Parameters.AddWithValue("@GroupName", model.GroupName ?? string.Empty);
        cmd.Parameters.AddWithValue("@GroupOrder", model.GroupOrder ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MailEnable", model.MailEnable);
        cmd.Parameters.AddWithValue("@ShowList", model.ShowList);
        cmd.Parameters.AddWithValue("@MainShowList", model.MainShowList);
        cmd.Parameters.AddWithValue("@HeaderHtml", model.HeaderHtml ?? string.Empty);
        cmd.Parameters.AddWithValue("@FooterHtml", model.FooterHtml ?? string.Empty);
        cmd.Parameters.AddWithValue("@AppName", model.AppName ?? string.Empty);
        cmd.Parameters.AddWithValue("@Step", model.Step);
        cmd.Parameters.AddWithValue("@Id", model.Id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Resources WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
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
        var result = new List<Resource>();
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Resources WHERE AppName = @AppName ORDER BY GroupOrder, Alias";
        cmd.Parameters.AddWithValue("@AppName", appName);

        await conn.OpenAsync();
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(Map(reader));
        }
        return result;
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

    private Resource Map(SqlDataReader reader)
    {
        return new Resource
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Alias = reader["Alias"]?.ToString() ?? string.Empty,
            Route = reader["Route"]?.ToString() ?? string.Empty,
            Title = reader["Title"]?.ToString() ?? string.Empty,
            Description = reader["Description"]?.ToString() ?? string.Empty,
            SysopUserId = reader["SysopUserId"]?.ToString() ?? string.Empty,
            IsPublic = reader["IsPublic"] != DBNull.Value && (bool)reader["IsPublic"],
            GroupName = reader["GroupName"]?.ToString() ?? string.Empty,
            GroupOrder = reader["GroupOrder"] != DBNull.Value ? (int?)reader["GroupOrder"] : null,
            MailEnable = reader["MailEnable"] != DBNull.Value && (bool)reader["MailEnable"],
            ShowList = reader["ShowList"] != DBNull.Value && (bool)reader["ShowList"],
            MainShowList = reader["MainShowList"] != DBNull.Value && (bool)reader["MainShowList"],
            HeaderHtml = reader["HeaderHtml"]?.ToString() ?? string.Empty,
            FooterHtml = reader["FooterHtml"]?.ToString() ?? string.Empty,
            AppName = reader["AppName"]?.ToString() ?? "ReportWriter",
            Step = reader["Step"] != DBNull.Value ? (int)reader["Step"] : 0
        };
    }

    public async Task<List<Resource>> GetAllByAppNameAsync(string appName, string? connectionString = null)
    {
        var result = new List<Resource>();
        var conn = GetConnection(connectionString);
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT * 
        FROM Resources 
        WHERE AppName = @AppName
        ORDER BY GroupOrder ASC, Alias ASC"; // AppName 별로, 그룹정렬/별칭정렬

        cmd.Parameters.AddWithValue("@AppName", appName);

        await conn.OpenAsync();
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task<bool> MoveUpAsync(int id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        await conn.OpenAsync();

        // 1. 현재 항목 조회
        var getCurrentCmd = conn.CreateCommand();
        getCurrentCmd.CommandText = "SELECT TOP 1 * FROM Resources WHERE Id = @Id";
        getCurrentCmd.Parameters.AddWithValue("@Id", id);
        var currentReader = await getCurrentCmd.ExecuteReaderAsync();
        if (!await currentReader.ReadAsync()) return false;

        var currentAppName = currentReader["AppName"].ToString() ?? "";
        var currentOrder = currentReader["DisplayOrder"] != DBNull.Value ? (int)currentReader["DisplayOrder"] : 0;
        currentReader.Close();

        // 2. 위쪽 항목 조회
        var getUpperCmd = conn.CreateCommand();
        getUpperCmd.CommandText = @"
        SELECT TOP 1 * FROM Resources
        WHERE AppName = @AppName AND DisplayOrder < @CurrentOrder
        ORDER BY DisplayOrder DESC";
        getUpperCmd.Parameters.AddWithValue("@AppName", currentAppName);
        getUpperCmd.Parameters.AddWithValue("@CurrentOrder", currentOrder);

        var upperReader = await getUpperCmd.ExecuteReaderAsync();
        if (!await upperReader.ReadAsync()) return false;

        var upperId = (int)upperReader["Id"];
        var upperOrder = (int)upperReader["DisplayOrder"];
        upperReader.Close();

        // 3. Swap
        var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = @"
        UPDATE Resources SET DisplayOrder = @UpperOrder WHERE Id = @CurrentId;
        UPDATE Resources SET DisplayOrder = @CurrentOrder WHERE Id = @UpperId;";
        updateCmd.Parameters.AddWithValue("@CurrentId", id);
        updateCmd.Parameters.AddWithValue("@UpperId", upperId);
        updateCmd.Parameters.AddWithValue("@CurrentOrder", currentOrder);
        updateCmd.Parameters.AddWithValue("@UpperOrder", upperOrder);

        return await updateCmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> MoveDownAsync(int id, string? connectionString = null)
    {
        var conn = GetConnection(connectionString);
        await conn.OpenAsync();

        // 1. 현재 항목 조회
        var getCurrentCmd = conn.CreateCommand();
        getCurrentCmd.CommandText = "SELECT TOP 1 * FROM Resources WHERE Id = @Id";
        getCurrentCmd.Parameters.AddWithValue("@Id", id);
        var currentReader = await getCurrentCmd.ExecuteReaderAsync();
        if (!await currentReader.ReadAsync()) return false;

        var currentAppName = currentReader["AppName"].ToString() ?? "";
        var currentOrder = currentReader["DisplayOrder"] != DBNull.Value ? (int)currentReader["DisplayOrder"] : 0;
        currentReader.Close();

        // 2. 아래쪽 항목 조회
        var getLowerCmd = conn.CreateCommand();
        getLowerCmd.CommandText = @"
        SELECT TOP 1 * FROM Resources
        WHERE AppName = @AppName AND DisplayOrder > @CurrentOrder
        ORDER BY DisplayOrder ASC";
        getLowerCmd.Parameters.AddWithValue("@AppName", currentAppName);
        getLowerCmd.Parameters.AddWithValue("@CurrentOrder", currentOrder);

        var lowerReader = await getLowerCmd.ExecuteReaderAsync();
        if (!await lowerReader.ReadAsync()) return false;

        var lowerId = (int)lowerReader["Id"];
        var lowerOrder = (int)lowerReader["DisplayOrder"];
        lowerReader.Close();

        // 3. Swap
        var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = @"
        UPDATE Resources SET DisplayOrder = @LowerOrder WHERE Id = @CurrentId;
        UPDATE Resources SET DisplayOrder = @CurrentOrder WHERE Id = @LowerId;";
        updateCmd.Parameters.AddWithValue("@CurrentId", id);
        updateCmd.Parameters.AddWithValue("@LowerId", lowerId);
        updateCmd.Parameters.AddWithValue("@CurrentOrder", currentOrder);
        updateCmd.Parameters.AddWithValue("@LowerOrder", lowerOrder);

        return await updateCmd.ExecuteNonQueryAsync() > 0;
    }
}
