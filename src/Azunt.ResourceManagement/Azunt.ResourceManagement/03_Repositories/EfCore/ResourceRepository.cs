using Azunt.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azunt.ResourceManagement;

/// <summary>
/// Resource 테이블을 위한 EF Core 기반 리포지토리 클래스입니다.
/// - 기본 CRUD
/// - 검색 및 페이징
/// - AppName별 필터링 지원
/// </summary>
public class ResourceRepository : IResourceRepository
{
    private readonly ResourceAppDbContextFactory _factory;
    private readonly ILogger<ResourceRepository> _logger;

    public ResourceRepository(
        ResourceAppDbContextFactory factory,
        ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<ResourceRepository>();
    }

    private ResourceAppDbContext CreateContext(string? connectionString)
    {
        return string.IsNullOrEmpty(connectionString)
            ? _factory.CreateDbContext()
            : _factory.CreateDbContext(connectionString);
    }

    public async Task<Resource> AddAsync(Resource model, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        context.Resources.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<List<Resource>> GetAllAsync(string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        return await context.Resources
            .OrderByDescending(r => r.Id)
            .ToListAsync();
    }

    public async Task<Resource> GetByIdAsync(int id, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        return await context.Resources
                   .SingleOrDefaultAsync(r => r.Id == id)
               ?? new Resource();
    }

    public async Task<bool> UpdateAsync(Resource model, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        context.Attach(model);
        context.Entry(model).State = EntityState.Modified;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        var entity = await context.Resources.FindAsync(id);
        if (entity == null) return false;
        context.Resources.Remove(entity);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<ArticleSet<Resource, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex, int pageSize,
        string searchField, string searchQuery,
        string sortOrder, TParentIdentifier parentIdentifier,
        string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);

        var query = context.Resources.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(r =>
                (r.Title ?? string.Empty).Contains(searchQuery) ||
                (r.Description != null && r.Description.Contains(searchQuery)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Id)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ArticleSet<Resource, int>(items, totalCount);
    }

    public async Task<List<Resource>> GetByAppNameAsync(string appName, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        return await context.Resources
            .Where(r => r.AppName == appName)
            .OrderBy(r => r.GroupOrder)
            .ThenBy(r => r.Alias)
            .ToListAsync();
    }

    public async Task<ArticleSet<Resource, int>> GetByAppNameAsync<TParentIdentifier>(
        string appName,
        int pageIndex, int pageSize,
        string searchField, string searchQuery,
        string sortOrder, TParentIdentifier parentIdentifier,
        string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);

        var query = context.Resources
            .Where(r => r.AppName == appName)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(r =>
                (r.Title ?? string.Empty).Contains(searchQuery) ||
                (r.Description != null && r.Description.Contains(searchQuery)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(r => r.GroupOrder)
            .ThenBy(r => r.Alias)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ArticleSet<Resource, int>(items, totalCount);
    }

    public async Task<List<Resource>> GetAllByAppNameAsync(string appName, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);

        return await context.Resources
            .Where(r => r.AppName == appName)
            .OrderBy(r => r.GroupOrder)
            .ThenBy(r => r.Alias)
            .ToListAsync();
    }

    public async Task<bool> MoveUpAsync(int id, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        var current = await context.Resources.FirstOrDefaultAsync(x => x.Id == id);
        if (current == null || current.DisplayOrder == null) return false;

        var upper = await context.Resources
            .Where(x => x.AppName == current.AppName &&
                        x.DisplayOrder != null &&
                        x.DisplayOrder < current.DisplayOrder)
            .OrderByDescending(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (upper == null) return false;

        int temp = current.DisplayOrder.Value;
        current.DisplayOrder = upper.DisplayOrder;
        upper.DisplayOrder = temp;

        context.Resources.Update(current);
        context.Resources.Update(upper);

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveDownAsync(int id, string? connectionString = null)
    {
        await using var context = CreateContext(connectionString);
        var current = await context.Resources.FirstOrDefaultAsync(x => x.Id == id);
        if (current == null || current.DisplayOrder == null) return false;

        var lower = await context.Resources
            .Where(x => x.AppName == current.AppName &&
                        x.DisplayOrder != null &&
                        x.DisplayOrder > current.DisplayOrder)
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (lower == null) return false;

        int temp = current.DisplayOrder.Value;
        current.DisplayOrder = lower.DisplayOrder;
        lower.DisplayOrder = temp;

        context.Resources.Update(current);
        context.Resources.Update(lower);

        await context.SaveChangesAsync();
        return true;
    }
}

