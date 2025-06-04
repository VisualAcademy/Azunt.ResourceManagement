using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.ResourceManagement;

public static class ResourceSeedDataProvider
{
    public static void InsertRequiredResources(SqlConnection connection, ILogger logger, string? appName = null)
    {
        var all = GetAllSeedData();

        var filtered = string.IsNullOrEmpty(appName)
            ? all
            : all.Where(x => x.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered)
        {
            var checkCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM [dbo].[Resources]
                WHERE [Alias] = @Alias AND [AppName] = @AppName", connection);

            checkCmd.Parameters.AddWithValue("@Alias", item.Alias);
            checkCmd.Parameters.AddWithValue("@AppName", item.AppName);

            int exists = (int)checkCmd.ExecuteScalar();

            if (exists == 0)
            {
                var insertCmd = new SqlCommand(@"
                    INSERT INTO [dbo].[Resources]
                        ([Alias], [Route], [Description], [GroupName], [GroupOrder], [DisplayOrder],
                         [Title], [IsPublic], [MainShowList], [ShowList], [AppName], [Step])
                    VALUES
                        (@Alias, @Route, @Description, @GroupName, @GroupOrder, @DisplayOrder,
                         @Title, 1, 1, 1, @AppName, @Step)", connection);

                insertCmd.Parameters.AddWithValue("@Alias", item.Alias);
                insertCmd.Parameters.AddWithValue("@Route", item.Route ?? "");
                insertCmd.Parameters.AddWithValue("@Description", item.Description ?? "");
                insertCmd.Parameters.AddWithValue("@GroupName", item.AppName ?? "");
                insertCmd.Parameters.AddWithValue("@GroupOrder", item.GroupOrder);
                insertCmd.Parameters.AddWithValue("@DisplayOrder", item.GroupOrder);
                insertCmd.Parameters.AddWithValue("@Title", item.Title ?? "");
                insertCmd.Parameters.AddWithValue("@AppName", item.AppName ?? "");
                insertCmd.Parameters.AddWithValue("@Step", item.Step);

                insertCmd.ExecuteNonQuery();
                logger.LogInformation($"[Inserted] {item.Alias} ({item.AppName})");
            }
            else
            {
                var updateCmd = new SqlCommand(@"
                    UPDATE [dbo].[Resources]
                    SET 
                        [GroupOrder] = @GroupOrder,
                        [DisplayOrder] = @DisplayOrder,
                        [Title] = @Title,
                        [Route] = @Route,
                        [Description] = @Description
                    WHERE [Alias] = @Alias AND [AppName] = @AppName", connection);

                updateCmd.Parameters.AddWithValue("@Alias", item.Alias);
                updateCmd.Parameters.AddWithValue("@AppName", item.AppName);
                updateCmd.Parameters.AddWithValue("@GroupOrder", item.GroupOrder);
                updateCmd.Parameters.AddWithValue("@DisplayOrder", item.GroupOrder);
                updateCmd.Parameters.AddWithValue("@Title", item.Title ?? "");
                updateCmd.Parameters.AddWithValue("@Route", item.Route ?? "");
                updateCmd.Parameters.AddWithValue("@Description", item.Description ?? "");

                int affected = updateCmd.ExecuteNonQuery();
                if (affected > 0)
                    logger.LogInformation($"[Updated] {item.Alias} ({item.AppName})");
            }
        }
    }

    private static List<ResourceSeedItem> GetAllSeedData()
    {
        return new List<ResourceSeedItem>
        {
            new("Home", "Home", "/", "Main landing page", 1, 0, "VisualAcademy"),
            new("About", "About", "/About", "About the site", 2, 0, "VisualAcademy"),
            new("Notes", "Notes", "/Notes", "Note page", 1, 0, "DotNetNote"),
            // ... TODO
        };
    }
}

public record ResourceSeedItem(
    string Alias,
    string Title,
    string Route,
    string Description,
    int GroupOrder,
    int Step,
    string AppName
);
