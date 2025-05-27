using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.ResourceManagement;

/// <summary>
/// ResourceAppDbContext 인스턴스를 생성하는 Factory 클래스입니다.
/// - 다중 연결 문자열 또는 DI 없이 직접 생성 가능
/// </summary>
public class ResourceAppDbContextFactory
{
    private readonly IConfiguration? _configuration;

    /// <summary>
    /// 기본 생성자 (Configuration 없이 사용 가능)
    /// </summary>
    public ResourceAppDbContextFactory()
    {
    }

    /// <summary>
    /// IConfiguration을 주입받는 생성자
    /// </summary>
    public ResourceAppDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 연결 문자열을 사용하여 DbContext 인스턴스를 생성합니다.
    /// </summary>
    public ResourceAppDbContext CreateDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));
        }

        var options = new DbContextOptionsBuilder<ResourceAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ResourceAppDbContext(options);
    }

    /// <summary>
    /// DbContextOptions를 사용하여 DbContext 인스턴스를 생성합니다.
    /// </summary>
    public ResourceAppDbContext CreateDbContext(DbContextOptions<ResourceAppDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ResourceAppDbContext(options);
    }

    /// <summary>
    /// appsettings.json의 "DefaultConnection"을 사용하여 DbContext 인스턴스를 생성합니다.
    /// </summary>
    public ResourceAppDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}
