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

        using (var connection = new SqlConnection(_masterConnectionString))
        {
            connection.Open();
            var cmd = new SqlCommand("SELECT ConnectionString FROM dbo.Tenants", connection);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var connStr = reader["ConnectionString"]?.ToString();
                    if (!string.IsNullOrEmpty(connStr))
                    {
                        result.Add(connStr);
                    }
                }
            }
        }

        return result;
    }

    private void EnsureResourcesTable(string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // 1. 테이블 생성 여부 확인 및 생성
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
                        [Step] INT NULL DEFAULT(0)
                    )", connection);

                createCmd.ExecuteNonQuery();
                _logger.LogInformation("Resources table created.");
            }

            // 2. 컬럼 보강
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

            foreach (var kvp in expectedColumns)
            {
                var columnName = kvp.Key;

                var cmdColumnCheck = new SqlCommand(@"
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Resources' AND COLUMN_NAME = @ColumnName", connection);
                cmdColumnCheck.Parameters.AddWithValue("@ColumnName", columnName);

                int colExists = (int)cmdColumnCheck.ExecuteScalar();

                if (colExists == 0)
                {
                    var alterCmd = new SqlCommand(
                        $"ALTER TABLE [dbo].[Resources] ADD [{columnName}] {kvp.Value}", connection);
                    alterCmd.ExecuteNonQuery();

                    _logger.LogInformation($"Column added: {columnName} ({kvp.Value})");
                }
            }

            // 3. NULL 보정
            foreach (var column in new[] { "Alias", "Route", "Title", "Description", "SysopUserId", "GroupName", "HeaderHtml", "FooterHtml", "AppName" })
            {
                var updateCmd = new SqlCommand($@"
                    UPDATE [dbo].[Resources]
                    SET [{column}] = ''
                    WHERE [{column}] IS NULL", connection);

                int updated = updateCmd.ExecuteNonQuery();
                if (updated > 0)
                    _logger.LogInformation($"{column} NULL → '' set for {updated} rows.");
            }

            var updateStepCmd = new SqlCommand(@"
                UPDATE [dbo].[Resources]
                SET [Step] = 0
                WHERE [Step] IS NULL", connection);
            updateStepCmd.ExecuteNonQuery();

            // 4. 필수 레코드 삽입 (항목별로 존재 여부 체크)
            var requiredResources = new List<(string Alias, string Title, string Route, string Description, int GroupOrder, int Step, string AppName)>
            {
                // VisualAcademy용: DisplayOrder 순
                ("Home", "Home", "/", "Main landing page", 1, 0, "VisualAcademy"),
                ("About", "About", "/About", "About the site", 2, 0, "VisualAcademy"),
                ("Contact", "Contact", "/Contact", "Contact us form", 3, 0, "VisualAcademy"),
                ("Privacy", "Privacy Policy", "/Privacy", "Privacy Policy Page", 4, 0, "VisualAcademy"),
                ("Terms", "Terms of Use", "/Terms", "Terms of Use", 5, 0, "VisualAcademy"),
                ("Login", "Login", "/Identity/Account/Login", "User login page", 6, 0, "VisualAcademy"),
                ("Register", "Register", "/Identity/Account/Register", "User registration page", 7, 0, "VisualAcademy"),
                ("ManageAccount", "Manage Account", "/Identity/Account/Manage", "User profile and settings", 8, 0, "VisualAcademy"),
                ("AccessDenied", "Access Denied", "/Identity/Account/AccessDenied", "Access denied page", 9, 0, "VisualAcademy"),
                ("NotFound", "404 Not Found", "/Error/404", "Custom 404 page", 10, 0, "VisualAcademy"),
                ("ServerError", "500 Server Error", "/Error/500", "Custom 500 page", 11, 0, "VisualAcademy"),
                ("Blog", "Blog", "/Blog", "Public blog listing", 12, 0, "VisualAcademy"),
                ("BlogAdmin", "Blog Admin", "/Blog/Admin", "Admin blog management", 13, 1, "VisualAcademy"),
                ("Docs", "Documentation", "/Docs", "Developer or user documentation", 14, 0, "VisualAcademy"),
                ("Admin", "Admin Dashboard", "/Admin", "Main admin area", 15, 1, "VisualAcademy"),
                ("Settings", "Site Settings", "/Admin/Settings", "Manage global settings", 16, 1, "VisualAcademy"),
                ("Users", "User Management", "/Admin/Users", "User management area", 17, 1, "VisualAcademy"),
                ("Roles", "Role Management", "/Admin/Roles", "Roles and permissions", 18, 1, "VisualAcademy"),
                ("Menus", "Menu Builder", "/Admin/Menus", "Navigation menu editor", 19, 1, "VisualAcademy"),
                ("Resources", "Resource Management", "/Resources/Manage", "Resource permission settings", 20, 1, "VisualAcademy"),

                // DotNetNote용: DisplayOrder 순
                ("Home", "Home", "/", "Main homepage", 1, 0, "DotNetNote"),
                ("Notes", "Notes", "/Notes", "List of notes", 2, 0, "DotNetNote"),
                ("NoteDetail", "Note Detail", "/Notes/Details/{id}", "View individual note", 3, 0, "DotNetNote"),
                ("WriteNote", "Write Note", "/Notes/Write", "Create or edit a note", 4, 1, "DotNetNote"),
                ("Categories", "Categories", "/Notes/Categories", "Manage or browse categories", 5, 0, "DotNetNote"),
                ("Tags", "Tags", "/Notes/Tags", "Tag cloud or tag browsing", 6, 0, "DotNetNote"),
                ("Search", "Search", "/Notes/Search", "Search notes", 7, 0, "DotNetNote"),
                ("Comments", "Comments", "/Notes/Comments", "View or manage comments", 8, 1, "DotNetNote"),
                ("Archives", "Archives", "/Notes/Archives", "Monthly archive list", 9, 0, "DotNetNote"),
                ("Popular", "Popular Posts", "/Notes/Popular", "Most viewed notes", 10, 0, "DotNetNote"),
                ("About", "About", "/About", "About the blog or author", 11, 0, "DotNetNote"),
                ("Contact", "Contact", "/Contact", "Contact form", 12, 0, "DotNetNote"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "DotNetNote"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "DotNetNote"),
                ("ManageAccount", "My Account", "/Identity/Account/Manage", "User account settings", 15, 0, "DotNetNote"),
                ("Admin", "Admin Panel", "/Admin", "Administrative dashboard", 16, 1, "DotNetNote"),
                ("NoteAdmin", "Note Admin", "/Admin/Notes", "Administer notes", 17, 1, "DotNetNote"),
                ("UserAdmin", "User Admin", "/Admin/Users", "Administer users", 18, 1, "DotNetNote"),
                ("LogViewer", "Log Viewer", "/Admin/Logs", "View system logs", 19, 1, "DotNetNote"),
                ("SiteSettings", "Site Settings", "/Admin/Settings", "Configure site-wide options", 20, 1, "DotNetNote"),

                // DevLec용: DisplayOrder 순
                ("Home", "Home", "/", "DevLec main page", 1, 0, "DevLec"),
                ("Courses", "Courses", "/Courses", "Course catalog", 2, 0, "DevLec"),
                ("CourseDetail", "Course Detail", "/Courses/{courseId}", "View specific course", 3, 0, "DevLec"),
                ("Lessons", "Lessons", "/Courses/{courseId}/Lessons", "Lesson list for a course", 4, 0, "DevLec"),
                ("LessonDetail", "Lesson Detail", "/Courses/{courseId}/Lessons/{lessonId}", "Watch a lesson", 5, 0, "DevLec"),
                ("CodeLab", "Code Lab", "/CodeLab", "Interactive coding environment", 6, 0, "DevLec"),
                ("SubmitCode", "Submit Code", "/CodeLab/Submit", "Submit solution for review", 7, 0, "DevLec"),
                ("MyProgress", "My Progress", "/My/Progress", "Track your learning progress", 8, 0, "DevLec"),
                ("Quizzes", "Quizzes", "/Courses/{courseId}/Quizzes", "Take course quizzes", 9, 0, "DevLec"),
                ("Certificate", "Certificate", "/My/Certificate", "Course completion certificates", 10, 0, "DevLec"),
                ("Discussions", "Discussions", "/Discussions", "Student and instructor Q&A", 11, 0, "DevLec"),
                ("Profile", "My Profile", "/Profile", "User profile and settings", 12, 0, "DevLec"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "DevLec"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "DevLec"),
                ("Admin", "Admin Dashboard", "/Admin", "Admin main page", 15, 1, "DevLec"),
                ("CourseAdmin", "Manage Courses", "/Admin/Courses", "Course creation and editing", 16, 1, "DevLec"),
                ("LessonAdmin", "Manage Lessons", "/Admin/Lessons", "Lesson management", 17, 1, "DevLec"),
                ("UserAdmin", "Manage Users", "/Admin/Users", "User management", 18, 1, "DevLec"),
                ("SubmissionReview", "Submission Review", "/Admin/Submissions", "Code submission review", 19, 1, "DevLec"),
                ("SiteSettings", "Site Settings", "/Admin/Settings", "Platform configuration", 20, 1, "DevLec"),

                // Hawaso용: DisplayOrder 순
                ("Home", "Home", "/", "Main landing page", 1, 0, "Hawaso"),
                ("Notices", "Notices", "/Boards/Notices", "Notice board", 2, 0, "Hawaso"),
                ("FreeBoard", "Free Board", "/Boards/Free", "General discussion board", 3, 0, "Hawaso"),
                ("QnA", "Q&A", "/Boards/QnA", "Question and Answer board", 4, 0, "Hawaso"),
                ("Gallery", "Gallery", "/Gallery", "Image gallery", 5, 0, "Hawaso"),
                ("Files", "Files", "/Files", "File downloads", 6, 0, "Hawaso"),
                ("Categories", "Categories", "/Boards/Categories", "Post category management", 7, 0, "Hawaso"),
                ("Tags", "Tags", "/Boards/Tags", "Tag management", 8, 0, "Hawaso"),
                ("Search", "Search", "/Search", "Content search", 9, 0, "Hawaso"),
                ("MyPosts", "My Posts", "/My/Posts", "User's own posts", 10, 0, "Hawaso"),
                ("MyComments", "My Comments", "/My/Comments", "User's comments", 11, 0, "Hawaso"),
                ("Profile", "Profile", "/Profile", "Edit personal profile", 12, 0, "Hawaso"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "Hawaso"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "Hawaso"),
                ("Admin", "Admin Panel", "/Admin", "Admin dashboard", 15, 1, "Hawaso"),
                ("BoardAdmin", "Board Admin", "/Admin/Boards", "Board settings and management", 16, 1, "Hawaso"),
                ("GalleryAdmin", "Gallery Admin", "/Admin/Gallery", "Gallery configuration", 17, 1, "Hawaso"),
                ("FileAdmin", "File Admin", "/Admin/Files", "Uploaded file management", 18, 1, "Hawaso"),
                ("UserAdmin", "User Admin", "/Admin/Users", "Manage users and roles", 19, 1, "Hawaso"),
                ("SiteSettings", "Site Settings", "/Admin/Settings", "Global site configuration", 20, 1, "Hawaso"),

                // MemoEngine용: DisplayOrder 순
                ("Home", "Home", "/", "Main page", 1, 0, "MemoEngine"),
                ("Blog", "Blog", "/Blog", "Blog post list", 2, 0, "MemoEngine"),
                ("Post", "Post Detail", "/Blog/{slug}", "Individual blog post", 3, 0, "MemoEngine"),
                ("Tags", "Tags", "/Blog/Tags", "Post tags", 4, 0, "MemoEngine"),
                ("Categories", "Categories", "/Blog/Categories", "Blog categories", 5, 0, "MemoEngine"),
                ("Archives", "Archives", "/Blog/Archives", "Monthly archive of posts", 6, 0, "MemoEngine"),
                ("Search", "Search", "/Search", "Search blog or boards", 7, 0, "MemoEngine"),
                ("Boards", "Boards", "/Boards", "All boards list", 8, 0, "MemoEngine"),
                ("BoardDetail", "Board Detail", "/Boards/{boardName}", "Posts by board", 9, 0, "MemoEngine"),
                ("Write", "Write Post", "/Boards/{boardName}/Write", "Write or edit a post", 10, 1, "MemoEngine"),
                ("Files", "Files", "/Files", "File attachments", 11, 0, "MemoEngine"),
                ("Comments", "Comments", "/Comments", "View or manage comments", 12, 0, "MemoEngine"),
                ("Profile", "Profile", "/Profile", "User profile page", 13, 0, "MemoEngine"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 14, 0, "MemoEngine"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 15, 0, "MemoEngine"),
                ("Admin", "Admin Panel", "/Admin", "Admin dashboard", 16, 1, "MemoEngine"),
                ("BlogAdmin", "Blog Admin", "/Admin/Blog", "Manage blog posts", 17, 1, "MemoEngine"),
                ("BoardAdmin", "Board Admin", "/Admin/Boards", "Manage boards", 18, 1, "MemoEngine"),
                ("FileAdmin", "File Admin", "/Admin/Files", "Manage uploaded files", 19, 1, "MemoEngine"),
                ("Settings", "Site Settings", "/Admin/Settings", "Site-wide settings", 20, 1, "MemoEngine"),

                // JavaCampus용: DisplayOrder 순
                ("Home", "Home", "/", "JavaCampus main page", 1, 0, "JavaCampus"),
                ("Courses", "Courses", "/Courses", "List of available courses", 2, 0, "JavaCampus"),
                ("CourseDetail", "Course Detail", "/Courses/{courseId}", "Course content and syllabus", 3, 0, "JavaCampus"),
                ("Lessons", "Lessons", "/Courses/{courseId}/Lessons", "Lesson videos and content", 4, 0, "JavaCampus"),
                ("CodePlayground", "Code Playground", "/Playground", "Java coding practice area", 5, 0, "JavaCampus"),
                ("Assignments", "Assignments", "/Courses/{courseId}/Assignments", "Submit assignments", 6, 0, "JavaCampus"),
                ("Submissions", "My Submissions", "/My/Submissions", "Track assignment submissions", 7, 0, "JavaCampus"),
                ("Quizzes", "Quizzes", "/Courses/{courseId}/Quizzes", "Take quizzes for the course", 8, 0, "JavaCampus"),
                ("Exam", "Exams", "/Courses/{courseId}/Exam", "Midterm and final exams", 9, 0, "JavaCampus"),
                ("Certification", "Certificate", "/My/Certificate", "Earned course certificates", 10, 0, "JavaCampus"),
                ("Forum", "Forum", "/Forum", "Ask questions and help others", 11, 0, "JavaCampus"),
                ("Profile", "Profile", "/Profile", "Edit my student profile", 12, 0, "JavaCampus"),
                ("Login", "Login", "/Identity/Account/Login", "Login to JavaCampus", 13, 0, "JavaCampus"),
                ("Register", "Register", "/Identity/Account/Register", "Register a new account", 14, 0, "JavaCampus"),
                ("Admin", "Admin Dashboard", "/Admin", "Administrative area", 15, 1, "JavaCampus"),
                ("CourseAdmin", "Manage Courses", "/Admin/Courses", "Create and manage courses", 16, 1, "JavaCampus"),
                ("LessonAdmin", "Manage Lessons", "/Admin/Lessons", "Upload or edit lessons", 17, 1, "JavaCampus"),
                ("AssignmentAdmin", "Manage Assignments", "/Admin/Assignments", "Instructor assignment control", 18, 1, "JavaCampus"),
                ("UserAdmin", "User Management", "/Admin/Users", "Manage students and instructors", 19, 1, "JavaCampus"),
                ("Settings", "Platform Settings", "/Admin/Settings", "Global JavaCampus settings", 20, 1, "JavaCampus"),

                // Portal용: DisplayOrder 순
                ("Home", "Home", "/", "Corporate portal homepage", 1, 0, "Portal"),
                ("News", "Company News", "/News", "Latest company news and announcements", 2, 0, "Portal"),
                ("Notice", "Notices", "/Boards/Notice", "Important company notices", 3, 0, "Portal"),
                ("TeamBoard", "Team Board", "/Boards/Team", "Team-specific communications", 4, 0, "Portal"),
                ("Calendar", "Calendar", "/Calendar", "Company-wide schedule", 5, 0, "Portal"),
                ("Events", "Events", "/Events", "Internal company events", 6, 0, "Portal"),
                ("Employees", "Employee Directory", "/Employees", "Search for employees", 7, 0, "Portal"),
                ("OrgChart", "Organization Chart", "/OrgChart", "Company organizational structure", 8, 0, "Portal"),
                ("Links", "Quick Links", "/Links", "Useful internal/external links", 9, 0, "Portal"),
                ("MyPage", "My Page", "/My", "Personal dashboard", 10, 0, "Portal"),
                ("Memo", "Memo Pad", "/Memo", "Private memo/notes", 11, 0, "Portal"),
                ("Profile", "Profile", "/Profile", "Update my profile", 12, 0, "Portal"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "Portal"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "Portal"),
                ("Admin", "Admin Panel", "/Admin", "Portal admin dashboard", 15, 1, "Portal"),
                ("NewsAdmin", "News Management", "/Admin/News", "Create and manage news", 16, 1, "Portal"),
                ("BoardAdmin", "Board Management", "/Admin/Boards", "Manage portal boards", 17, 1, "Portal"),
                ("EmployeeAdmin", "Employee Admin", "/Admin/Employees", "Manage employee directory", 18, 1, "Portal"),
                ("LinkAdmin", "Link Admin", "/Admin/Links", "Manage quick links", 19, 1, "Portal"),
                ("Settings", "Portal Settings", "/Admin/Settings", "System-wide settings", 20, 1, "Portal"),

                // EmployeeLicensing용: DisplayOrder 순
                ("Home", "Home", "/", "Main dashboard", 1, 0, "EmployeeLicensing"),
                ("Employees", "Employees", "/Employees", "List of employees", 2, 0, "EmployeeLicensing"),
                ("EmployeeDetail", "Employee Detail", "/Employees/{id}", "Employee profile", 3, 0, "EmployeeLicensing"),
                ("Licenses", "Licenses", "/Licenses", "List of licenses", 4, 0, "EmployeeLicensing"),
                ("LicenseDetail", "License Detail", "/Licenses/{id}", "License detail view", 5, 0, "EmployeeLicensing"),
                ("AddLicense", "Add License", "/Licenses/Add", "Register a new license", 6, 1, "EmployeeLicensing"),
                ("VerifyLicense", "Verify License", "/Licenses/Verify", "Verify license status", 7, 0, "EmployeeLicensing"),
                ("ExpiringSoon", "Expiring Soon", "/Licenses/Expiring", "Licenses nearing expiration", 8, 0, "EmployeeLicensing"),
                ("BackgroundChecks", "Background Checks", "/BackgroundChecks", "List of background checks", 9, 0, "EmployeeLicensing"),
                ("SubmitCheck", "Submit Background Check", "/BackgroundChecks/Submit", "Submit a new background check", 10, 1, "EmployeeLicensing"),
                ("MyLicenses", "My Licenses", "/My/Licenses", "View my licenses", 11, 0, "EmployeeLicensing"),
                ("MyChecks", "My Checks", "/My/BackgroundChecks", "My background check requests", 12, 0, "EmployeeLicensing"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "EmployeeLicensing"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "EmployeeLicensing"),
                ("Profile", "Profile", "/Profile", "User profile page", 15, 0, "EmployeeLicensing"),
                ("Admin", "Admin Dashboard", "/Admin", "Administrative area", 16, 1, "EmployeeLicensing"),
                ("EmployeeAdmin", "Manage Employees", "/Admin/Employees", "Manage employee records", 17, 1, "EmployeeLicensing"),
                ("LicenseAdmin", "Manage Licenses", "/Admin/Licenses", "License type configuration", 18, 1, "EmployeeLicensing"),
                ("CheckAdmin", "Manage Background Checks", "/Admin/Checks", "Review and manage checks", 19, 1, "EmployeeLicensing"),
                ("SiteSettings", "Site Settings", "/Admin/Settings", "Platform configuration", 20, 1, "EmployeeLicensing"),

                // VendorLicensing용: DisplayOrder 순
                ("Home", "Home", "/", "Vendor Licensing main page", 1, 0, "VendorLicensing"),
                ("Vendors", "Vendors", "/Vendors", "List of registered vendors", 2, 0, "VendorLicensing"),
                ("VendorDetail", "Vendor Detail", "/Vendors/{vendorId}", "View specific vendor", 3, 0, "VendorLicensing"),
                ("Employees", "Vendor Employees", "/Vendors/{vendorId}/Employees", "List of vendor's employees", 4, 0, "VendorLicensing"),
                ("EmployeeDetail", "Employee Detail", "/Employees/{id}", "Vendor employee detail view", 5, 0, "VendorLicensing"),
                ("Licenses", "Licenses", "/Employees/{id}/Licenses", "Employee license records", 6, 0, "VendorLicensing"),
                ("SubmitLicense", "Submit License", "/Licenses/Submit", "Submit a new license", 7, 1, "VendorLicensing"),
                ("BackgroundChecks", "Background Checks", "/Employees/{id}/BackgroundChecks", "Employee background check history", 8, 0, "VendorLicensing"),
                ("SubmitCheck", "Submit Check", "/BackgroundChecks/Submit", "Submit background check request", 9, 1, "VendorLicensing"),
                ("ComplianceStatus", "Compliance Status", "/Compliance", "Vendor compliance overview", 10, 0, "VendorLicensing"),
                ("ExpiringSoon", "Expiring Licenses", "/Licenses/Expiring", "Licenses nearing expiration", 11, 0, "VendorLicensing"),
                ("AccessRequests", "Access Requests", "/Access/Requests", "Facility access requests", 12, 0, "VendorLicensing"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "VendorLicensing"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "VendorLicensing"),
                ("Profile", "Profile", "/Profile", "User profile page", 15, 0, "VendorLicensing"),
                ("Admin", "Admin Panel", "/Admin", "Admin dashboard", 16, 1, "VendorLicensing"),
                ("VendorAdmin", "Manage Vendors", "/Admin/Vendors", "Add/edit vendor records", 17, 1, "VendorLicensing"),
                ("EmployeeAdmin", "Manage Employees", "/Admin/Employees", "Manage vendor employees", 18, 1, "VendorLicensing"),
                ("LicenseAdmin", "License Management", "/Admin/Licenses", "Manage license definitions", 19, 1, "VendorLicensing"),
                ("Settings", "Settings", "/Admin/Settings", "Platform configuration", 20, 1, "VendorLicensing"),

                // InternalAudit용: DisplayOrder 순
                ("Home", "Home", "/", "Internal Audit home", 1, 0, "InternalAudit"),
                ("AuditPlan", "Audit Plan", "/AuditPlans", "Annual or periodic audit plans", 2, 0, "InternalAudit"),
                ("AuditDetail", "Audit Detail", "/AuditPlans/{id}", "Audit plan details", 3, 0, "InternalAudit"),
                ("RiskAssessment", "Risk Assessment", "/RiskAssessments", "Risk evaluation for entities", 4, 0, "InternalAudit"),
                ("FieldWork", "Field Work", "/FieldWorks", "In-progress audit work", 5, 0, "InternalAudit"),
                ("Findings", "Findings", "/Findings", "Audit findings and observations", 6, 0, "InternalAudit"),
                ("CorrectiveActions", "Corrective Actions", "/Actions", "Remediation and corrective actions", 7, 0, "InternalAudit"),
                ("Tracking", "Tracking Status", "/Actions/Tracking", "Status of action items", 8, 0, "InternalAudit"),
                ("Documents", "Documents", "/Documents", "Upload and manage audit documents", 9, 0, "InternalAudit"),
                ("MyAudits", "My Audits", "/My/Audits", "Audits assigned to me", 10, 0, "InternalAudit"),
                ("Login", "Login", "/Identity/Account/Login", "User login", 11, 0, "InternalAudit"),
                ("Register", "Register", "/Identity/Account/Register", "User registration", 12, 0, "InternalAudit"),
                ("Profile", "Profile", "/Profile", "User profile page", 13, 0, "InternalAudit"),
                ("Admin", "Admin Dashboard", "/Admin", "Administrative dashboard", 14, 1, "InternalAudit"),
                ("AuditAdmin", "Manage Audits", "/Admin/Audits", "Create and manage audit plans", 15, 1, "InternalAudit"),
                ("RiskAdmin", "Risk Admin", "/Admin/Risks", "Manage risk matrices and categories", 16, 1, "InternalAudit"),
                ("UserAdmin", "User Management", "/Admin/Users", "Manage audit team and stakeholders", 17, 1, "InternalAudit"),
                ("Settings", "System Settings", "/Admin/Settings", "Audit platform configuration", 18, 1, "InternalAudit"),
                ("Logs", "Activity Logs", "/Admin/Logs", "Audit trail and user actions", 19, 1, "InternalAudit"),
                ("Dashboard", "Analytics Dashboard", "/Dashboard", "Visual insights and KPIs", 20, 0, "InternalAudit"),

                // ReportWriter용: DisplayOrder 순
                ("Dashboard", "Dashboard", "Dashboard", "Dashboard", 1, 0, "ReportWriter"),
                ("DailyLog", "Daily Log", "DailyLog", "Daily Log", 2, 0, "ReportWriter"),
                ("BriefingLogs", "Briefing Log", "BriefingLogs", "Briefing Log", 3, 0, "ReportWriter"),
                ("Incident", "Incident", "Incident", "Incident", 4, 0, "ReportWriter"),
                ("Incident-AddWritten", "Incident-AddWritten", "", "Incident-AddWritten", 5, 1, "ReportWriter"),
                ("Incident-AddReviewed", "Incident-AddReviewed", "", "Incident-AddReviewed", 6, 1, "ReportWriter"),
                ("Incident-AddApproved", "Incident-AddApproved", "", "Incident-AddApproved", 7, 1, "ReportWriter"),
                ("Incident-ArchiveReport", "Incident-ArchiveReport", "", "Incident-ArchiveReport", 8, 1, "ReportWriter"),
                ("ArchivedIncidents", "ArchivedIncidents", "ArchivedIncidents", "ArchivedIncidents", 9, 0, "ReportWriter"),
                ("Violations", "Violations", "Violations", "Violations", 10, 0, "ReportWriter"),
                ("BannedCustomers", "BannedCustomers", "BannedCustomers", "BannedCustomers", 11, 0, "ReportWriter"),
                ("Employee", "Employee", "EmployeeFinder", "Employee", 12, 0, "ReportWriter"),
                ("Vendor", "Vendor", "VendorFinder", "Vendor", 13, 0, "ReportWriter"),
                ("VendorEmployee", "Vendor Employee", "VendorEmployeeFinder", "Vendor Employee", 14, 0, "ReportWriter"),
                ("Others", "Others", "SubjectFinder", "Others", 15, 0, "ReportWriter"),
                ("Analytics", "Analytics", "Analytics/Report", "Analytics", 16, 0, "ReportWriter"),
                ("Malfunction", "Malfunction", "Malfunction", "Malfunction", 17, 0, "ReportWriter"),
                ("Library", "Library", "Libraries", "Library", 18, 0, "ReportWriter"),
                ("Archive", "Archive", "Archives", "Archive", 19, 0, "ReportWriter"),
                ("Drop-Down Lists", "Drop-Down Lists", "", "Drop-Down Lists", 20, 0, "ReportWriter"),

                // AssetTracking용: DisplayOrder 순
                ("Dashboard", "Dashboard", "/", "Dashboard", 1, 0, "SAT"),
                ("Projects", "Projects", "/Projects", "Projects Management", 2, 0, "SAT"),
                ("Machines", "Machines", "/Machines", "Machines Management", 3, 0, "SAT"),
                ("Software", "Software", "/Medias", "Software Management", 4, 0, "SAT"),
                ("LogicBoardAccessLog", "Logic Board Access Log", "/Logics", "Logic Board Access Log", 5, 0, "SAT"),
                ("Roles", "Roles", "/Administrations/Roles", "Roles Management", 6, 0, "SAT"),
                ("Users", "Users", "/Administrations/Users", "Users Management", 7, 0, "SAT"),
                ("Users", "User Roles", "/Admin/UserRoles", "User Roles Management", 8, 0, "SAT"),
                ("Resources", "Page Permission", "/Resources/Manage", "Page Permission", 9, 0, "SAT"),
                ("Manufacturers", "Manufacturers", "/Manufacturers", "Manufacturers List", 10, 0, "SAT"),
                ("MachineTypes", "Machine Types", "/MachineTypes", "Machine Types List", 11, 0, "SAT"),
                ("Subcategories ", "Subcategories", "/Subcategories ", "Sub Category", 12, 0, "SAT"),
                ("Reasons", "Reasons", "/Reasons", "Reasons Management", 13, 0, "SAT"),
                ("Depots", "Asset Location", "/Depots", "Asset Location", 14, 0, "SAT"),
                ("Denominations", "Denominations", "/Denominations", "Denominations", 15, 0, "SAT"),
                ("ProgressiveTypes", "Progressive Types", "/ProgressiveTypes", "Progressive Types", 16, 0, "SAT"),
                ("MediaThemes", "Media Themes", "/MediaThemes", "Media Themes", 17, 0, "SAT"),
                ("VettingSteps", "Vetting Steps", "/VettingSteps", "Vetting Steps Management", 18, 0, "SAT"),
                ("ApplicationLogs", "Application Logs", "/Logs", "Application Logs", 19, 0, "SAT")
            };

            foreach (var (alias, title, route, description, groupOrder, step, appName) in requiredResources)
            {
                var checkCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM [dbo].[Resources]
                    WHERE [Alias] = @Alias AND [AppName] = @AppName", connection);

                checkCmd.Parameters.AddWithValue("@Alias", alias);
                checkCmd.Parameters.AddWithValue("@AppName", appName);

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

                    insertCmd.Parameters.AddWithValue("@Alias", alias);
                    insertCmd.Parameters.AddWithValue("@Route", route ?? "");
                    insertCmd.Parameters.AddWithValue("@Description", description ?? "");
                    insertCmd.Parameters.AddWithValue("@GroupName", appName ?? "");
                    insertCmd.Parameters.AddWithValue("@GroupOrder", groupOrder);
                    insertCmd.Parameters.AddWithValue("@DisplayOrder", groupOrder); // DisplayOrder는 GroupOrder 기준
                    insertCmd.Parameters.AddWithValue("@Title", title ?? "");
                    insertCmd.Parameters.AddWithValue("@AppName", appName ?? "");
                    insertCmd.Parameters.AddWithValue("@Step", step);

                    insertCmd.ExecuteNonQuery();
                    _logger.LogInformation($"Inserted missing resource: {alias} ({appName})");
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

                    updateCmd.Parameters.AddWithValue("@Alias", alias);
                    updateCmd.Parameters.AddWithValue("@AppName", appName);
                    updateCmd.Parameters.AddWithValue("@GroupOrder", groupOrder);
                    updateCmd.Parameters.AddWithValue("@DisplayOrder", groupOrder); // DisplayOrder도 갱신
                    updateCmd.Parameters.AddWithValue("@Title", title ?? "");
                    updateCmd.Parameters.AddWithValue("@Route", route ?? "");
                    updateCmd.Parameters.AddWithValue("@Description", description ?? "");

                    int affected = updateCmd.ExecuteNonQuery();
                    if (affected > 0)
                        _logger.LogInformation($"Updated existing resource: {alias} ({appName}) with Group/DisplayOrder");
                }
            }

            // 5. DisplayOrder가 NULL인 경우 GroupOrder로 초기화
            // DisplayOrder 보정: NULL 또는 0 → GroupOrder로
            var fixDisplayOrderCmd = new SqlCommand(@"
                UPDATE [dbo].[Resources]
                SET DisplayOrder = GroupOrder
                WHERE DisplayOrder IS NULL OR DisplayOrder = 0", connection);

            int fixedDisplay = fixDisplayOrderCmd.ExecuteNonQuery();
            if (fixedDisplay > 0)
                _logger.LogInformation($"DisplayOrder NULL or 0 → GroupOrder 적용: {fixedDisplay} rows updated.");

            // 선택: GroupOrder가 NULL일 경우 DisplayOrder 값으로 보정
            var fixGroupOrderCmd = new SqlCommand(@"
                UPDATE [dbo].[Resources]
                SET GroupOrder = DisplayOrder
                WHERE GroupOrder IS NULL OR GroupOrder = 0", connection);

            int fixedGroup = fixGroupOrderCmd.ExecuteNonQuery();
            if (fixedGroup > 0)
                _logger.LogInformation($"GroupOrder NULL or 0 → DisplayOrder 적용: {fixedGroup} rows updated.");
        }
    }

    public static void Run(IServiceProvider services, bool forMaster)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<ResourcesTableBuilder>>();
            var config = services.GetRequiredService<IConfiguration>();
            var masterConnectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(masterConnectionString))
            {
                throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");
            }

            var builder = new ResourcesTableBuilder(masterConnectionString, logger);

            if (forMaster)
            {
                builder.BuildMasterDatabase();
            }
            else
            {
                builder.BuildTenantDatabases();
            }
        }
        catch (Exception ex)
        {
            var fallbackLogger = services.GetService<ILogger<ResourcesTableBuilder>>();
            fallbackLogger?.LogError(ex, "Error while processing Resources table.");
        }
    }
}
