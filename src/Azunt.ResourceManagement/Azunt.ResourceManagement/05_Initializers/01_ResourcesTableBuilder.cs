using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azunt.ResourceManagement;

public class ResourcesTableBuilder
{
    private readonly string _masterConnectionString;
    private readonly ILogger<ResourcesTableBuilder> _logger;

    public ResourcesTableBuilder(string masterConnectionString, ILogger<ResourcesTableBuilder> logger)
    {
        _masterConnectionString = masterConnectionString;
        _logger = logger;
    }

    public void BuildTenantDatabases()
    {
        var tenantConnectionStrings = GetTenantConnectionStrings();

        foreach (var connStr in tenantConnectionStrings)
        {
            try
            {
                EnsureResourcesTable(connStr);
                _logger.LogInformation($"Resources table processed (tenant DB): {connStr}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{connStr}] Error processing tenant DB (Resources)");
            }
        }
    }

    public void BuildMasterDatabase()
    {
        try
        {
            EnsureResourcesTable(_masterConnectionString);
            _logger.LogInformation("Resources table processed (master DB)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing master DB (Resources)");
        }
    }

    private List<string> GetTenantConnectionStrings()
    {
        var result = new List<string>();

        using var connection = new SqlConnection(_masterConnectionString);
        connection.Open();

        var cmd = new SqlCommand("SELECT ConnectionString FROM dbo.Tenants", connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var connStr = reader["ConnectionString"]?.ToString();
            if (!string.IsNullOrEmpty(connStr))
                result.Add(connStr);
        }

        return result;
    }

    private void EnsureResourcesTable(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // 1. 테이블 생성
        var cmdCheck = new SqlCommand(@"
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = 'Resources'", connection);

        int tableExists = (int)cmdCheck.ExecuteScalar();

        if (tableExists == 0)
        {
            var createCmd = new SqlCommand(@"
                CREATE TABLE [dbo].[Resources] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Alias] NVARCHAR(50) NULL,
                    [Route] NVARCHAR(255) NULL,
                    [Title] NVARCHAR(50) NULL,
                    [Description] NVARCHAR(200) NULL,
                    [SysopUserId] NVARCHAR(50) NULL,
                    [IsPublic] BIT NULL DEFAULT ((1)),
                    [GroupName] NVARCHAR(50) NULL,
                    [GroupOrder] INT NULL DEFAULT ((0)),
                    [DisplayOrder] INT NULL DEFAULT ((0)),
                    [MailEnable] BIT NULL DEFAULT ((0)),
                    [ShowList] BIT NULL DEFAULT ((1)),
                    [MainShowList] BIT DEFAULT(1),
                    [HeaderHtml] NVARCHAR(MAX) NULL,
                    [FooterHtml] NVARCHAR(MAX) NULL,
                    [AppName] NVARCHAR(100) NULL DEFAULT('ReportWriter'),
                    [Step] INT NULL DEFAULT(0),
                    [CreatedBy] NVARCHAR(255) NULL,
                    [Created] DATETIMEOFFSET NULL DEFAULT SYSDATETIMEOFFSET(),
                    [ModifiedBy] NVARCHAR(255) NULL,
                    [Modified] DATETIMEOFFSET NULL
                )", connection);

            createCmd.ExecuteNonQuery();
            _logger.LogInformation("Resources table created.");
        }

        // 2. 누락 컬럼 보강
        var expectedColumns = new Dictionary<string, string>
        {
            ["Alias"] = "NVARCHAR(50) NULL",
            ["Route"] = "NVARCHAR(255) NULL",
            ["Title"] = "NVARCHAR(50) NULL",
            ["Description"] = "NVARCHAR(200) NULL",
            ["SysopUserId"] = "NVARCHAR(50) NULL",
            ["IsPublic"] = "BIT NULL DEFAULT ((1))",
            ["GroupName"] = "NVARCHAR(50) NULL",
            ["GroupOrder"] = "INT NULL DEFAULT ((0))",
            ["DisplayOrder"] = "INT NULL DEFAULT ((0))",
            ["MailEnable"] = "BIT NULL DEFAULT ((0))",
            ["ShowList"] = "BIT NULL DEFAULT ((1))",
            ["MainShowList"] = "BIT DEFAULT(1)",
            ["HeaderHtml"] = "NVARCHAR(MAX) NULL",
            ["FooterHtml"] = "NVARCHAR(MAX) NULL",
            ["AppName"] = "NVARCHAR(100) NULL DEFAULT('ReportWriter')",
            ["Step"] = "INT NULL DEFAULT(0)",
            ["CreatedBy"] = "NVARCHAR(255) NULL",
            ["Created"] = "DATETIMEOFFSET NULL DEFAULT SYSDATETIMEOFFSET()",
            ["ModifiedBy"] = "NVARCHAR(255) NULL",
            ["Modified"] = "DATETIMEOFFSET NULL"
        };

        foreach (var (columnName, columnType) in expectedColumns)
        {
            var cmdColumnCheck = new SqlCommand(@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Resources' AND COLUMN_NAME = @ColumnName", connection);
            cmdColumnCheck.Parameters.AddWithValue("@ColumnName", columnName);

            int colExists = (int)cmdColumnCheck.ExecuteScalar();

            if (colExists == 0)
            {
                var alterCmd = new SqlCommand(
                    $"ALTER TABLE [dbo].[Resources] ADD [{columnName}] {columnType}", connection);
                alterCmd.ExecuteNonQuery();
                _logger.LogInformation($"Column added: {columnName} ({columnType})");
            }
        }

        // 3. NULL 보정
        foreach (var column in new[] { "Alias", "Route", "Title", "Description", "SysopUserId", "GroupName", "HeaderHtml", "FooterHtml", "AppName" })
        {
            var updateCmd = new SqlCommand($@"
                UPDATE [dbo].[Resources]
                SET [{column}] = ''
                WHERE [{column}] IS NULL", connection);
            updateCmd.ExecuteNonQuery();
        }

        new SqlCommand(@"
            UPDATE [dbo].[Resources]
            SET [Step] = 0
            WHERE [Step] IS NULL", connection).ExecuteNonQuery();

        // 4. 정렬값 보정
        new SqlCommand(@"
            UPDATE [dbo].[Resources]
            SET DisplayOrder = GroupOrder
            WHERE DisplayOrder IS NULL OR DisplayOrder = 0", connection).ExecuteNonQuery();

        new SqlCommand(@"
            UPDATE [dbo].[Resources]
            SET GroupOrder = DisplayOrder
            WHERE GroupOrder IS NULL OR GroupOrder = 0", connection).ExecuteNonQuery();
    }

    public static void Run(IServiceProvider services, bool forMaster)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<ResourcesTableBuilder>>();
            var config = services.GetRequiredService<IConfiguration>();
            var masterConnectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(masterConnectionString))
                throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");

            var builder = new ResourcesTableBuilder(masterConnectionString, logger);

            if (forMaster)
                builder.BuildMasterDatabase();
            else
                builder.BuildTenantDatabases();
        }
        catch (Exception ex)
        {
            var fallbackLogger = services.GetService<ILogger<ResourcesTableBuilder>>();
            fallbackLogger?.LogError(ex, "Error while processing Resources table.");
        }
    }
}
