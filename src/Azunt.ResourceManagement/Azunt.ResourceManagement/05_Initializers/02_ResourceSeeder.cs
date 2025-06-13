using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.ResourceManagement;

/// <summary>
/// Resources 테이블에 초기 데이터를 삽입하는 클래스입니다.
/// AppName에 따라 시드 데이터를 삽입하거나 업데이트합니다.
/// </summary>
public static class ResourceSeeder
{
    public static void InsertRequiredResources(string connectionString, ILogger logger, string? appName = null)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        InsertRequiredResources(connection, logger, appName);
    }

    /// <summary>
    /// 지정된 SqlConnection을 사용하여 Resources 테이블에 초기 데이터를 삽입 또는 갱신합니다.
    /// </summary>
    /// <param name="connection">열린 SqlConnection 객체</param>
    /// <param name="logger">로깅 도구</param>
    /// <param name="appName">선택적으로 특정 AppName만 삽입</param>
    public static void InsertRequiredResources(SqlConnection connection, ILogger logger, string? appName = null)
    {
        var all = GetAllSeedData();

        var filtered = string.IsNullOrWhiteSpace(appName)
            ? all
            : all.Where(x => x.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered)
        {
            var existsCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM [dbo].[Resources]
                WHERE [Alias] = @Alias AND [AppName] = @AppName", connection);

            existsCmd.Parameters.AddWithValue("@Alias", item.Alias);
            existsCmd.Parameters.AddWithValue("@AppName", item.AppName);

            int exists = (int)existsCmd.ExecuteScalar();

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
                insertCmd.Parameters.AddWithValue("@DisplayOrder", item.GroupOrder); // 정렬 기준
                insertCmd.Parameters.AddWithValue("@Title", item.Title ?? "");
                insertCmd.Parameters.AddWithValue("@AppName", item.AppName ?? "");
                insertCmd.Parameters.AddWithValue("@Step", item.Step);

                insertCmd.ExecuteNonQuery();
                logger.LogInformation($"[INSERT] {item.Alias} ({item.AppName})");
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

                updateCmd.Parameters.AddWithValue("@GroupOrder", item.GroupOrder);
                updateCmd.Parameters.AddWithValue("@DisplayOrder", item.GroupOrder);
                updateCmd.Parameters.AddWithValue("@Title", item.Title ?? "");
                updateCmd.Parameters.AddWithValue("@Route", item.Route ?? "");
                updateCmd.Parameters.AddWithValue("@Description", item.Description ?? "");
                updateCmd.Parameters.AddWithValue("@Alias", item.Alias);
                updateCmd.Parameters.AddWithValue("@AppName", item.AppName);

                int affected = updateCmd.ExecuteNonQuery();
                if (affected > 0)
                {
                    logger.LogInformation($"[UPDATE] {item.Alias} ({item.AppName})");
                }
            }
        }
    }

    /// <summary>
    /// 모든 AppName에 해당하는 시드 데이터를 반환합니다.
    /// </summary>
    /// <returns>리소스 초기값 목록</returns>
    private static List<ResourceSeedItem> GetAllSeedData()
    {
        return new List<ResourceSeedItem>
        {
                // VisualAcademy용: DisplayOrder 순
                new("Home", "Home", "/", "Main landing page", 1, 0, "VisualAcademy"),
                new("About", "About", "/About", "About the site", 2, 0, "VisualAcademy"),
                new("Contact", "Contact", "/Contact", "Contact us form", 3, 0, "VisualAcademy"),
                new("Privacy", "Privacy Policy", "/Privacy", "Privacy Policy Page", 4, 0, "VisualAcademy"),
                new("Terms", "Terms of Use", "/Terms", "Terms of Use", 5, 0, "VisualAcademy"),
                new("Login", "Login", "/Identity/Account/Login", "User login page", 6, 0, "VisualAcademy"),
                new("Register", "Register", "/Identity/Account/Register", "User registration page", 7, 0, "VisualAcademy"),
                new("ManageAccount", "Manage Account", "/Identity/Account/Manage", "User profile and settings", 8, 0, "VisualAcademy"),
                new("AccessDenied", "Access Denied", "/Identity/Account/AccessDenied", "Access denied page", 9, 0, "VisualAcademy"),
                new("NotFound", "404 Not Found", "/Error/404", "Custom 404 page", 10, 0, "VisualAcademy"),
                new("ServerError", "500 Server Error", "/Error/500", "Custom 500 page", 11, 0, "VisualAcademy"),
                new("Blog", "Blog", "/Blog", "Public blog listing", 12, 0, "VisualAcademy"),
                new("BlogAdmin", "Blog Admin", "/Blog/Admin", "Admin blog management", 13, 1, "VisualAcademy"),
                new("Docs", "Documentation", "/Docs", "Developer or user documentation", 14, 0, "VisualAcademy"),
                new("Admin", "Admin Dashboard", "/Admin", "Main admin area", 15, 1, "VisualAcademy"),
                new("Settings", "Site Settings", "/Admin/Settings", "Manage global settings", 16, 1, "VisualAcademy"),
                new("Users", "User Management", "/Admin/Users", "User management area", 17, 1, "VisualAcademy"),
                new("Roles", "Role Management", "/Admin/Roles", "Roles and permissions", 18, 1, "VisualAcademy"),
                new("Menus", "Menu Builder", "/Admin/Menus", "Navigation menu editor", 19, 1, "VisualAcademy"),
                new("Resources", "Resource Management", "/Resources/Manage", "Resource permission settings", 20, 1, "VisualAcademy"),

                // DotNetNote용: DisplayOrder 순
                new("Home", "Home", "/", "Main homepage", 1, 0, "DotNetNote"),
                new("Notes", "Notes", "/Notes", "List of notes", 2, 0, "DotNetNote"),
                new("NoteDetail", "Note Detail", "/Notes/Details/{id}", "View individual note", 3, 0, "DotNetNote"),
                new("WriteNote", "Write Note", "/Notes/Write", "Create or edit a note", 4, 1, "DotNetNote"),
                new("Categories", "Categories", "/Notes/Categories", "Manage or browse categories", 5, 0, "DotNetNote"),
                new("Tags", "Tags", "/Notes/Tags", "Tag cloud or tag browsing", 6, 0, "DotNetNote"),
                new("Search", "Search", "/Notes/Search", "Search notes", 7, 0, "DotNetNote"),
                new("Comments", "Comments", "/Notes/Comments", "View or manage comments", 8, 1, "DotNetNote"),
                new("Archives", "Archives", "/Notes/Archives", "Monthly archive list", 9, 0, "DotNetNote"),
                new("Popular", "Popular Posts", "/Notes/Popular", "Most viewed notes", 10, 0, "DotNetNote"),
                new("About", "About", "/About", "About the blog or author", 11, 0, "DotNetNote"),
                new("Contact", "Contact", "/Contact", "Contact form", 12, 0, "DotNetNote"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "DotNetNote"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "DotNetNote"),
                new("ManageAccount", "My Account", "/Identity/Account/Manage", "User account settings", 15, 0, "DotNetNote"),
                new("Admin", "Admin Panel", "/Admin", "Administrative dashboard", 16, 1, "DotNetNote"),
                new("NoteAdmin", "Note Admin", "/Admin/Notes", "Administer notes", 17, 1, "DotNetNote"),
                new("UserAdmin", "User Admin", "/Admin/Users", "Administer users", 18, 1, "DotNetNote"),
                new("LogViewer", "Log Viewer", "/Admin/Logs", "View system logs", 19, 1, "DotNetNote"),
                new("SiteSettings", "Site Settings", "/Admin/Settings", "Configure site-wide options", 20, 1, "DotNetNote"),

                // DevLec용: DisplayOrder 순
                new("Home", "Home", "/", "DevLec main page", 1, 0, "DevLec"),
                new("Courses", "Courses", "/Courses", "Course catalog", 2, 0, "DevLec"),
                new("CourseDetail", "Course Detail", "/Courses/{courseId}", "View specific course", 3, 0, "DevLec"),
                new("Lessons", "Lessons", "/Courses/{courseId}/Lessons", "Lesson list for a course", 4, 0, "DevLec"),
                new("LessonDetail", "Lesson Detail", "/Courses/{courseId}/Lessons/{lessonId}", "Watch a lesson", 5, 0, "DevLec"),
                new("CodeLab", "Code Lab", "/CodeLab", "Interactive coding environment", 6, 0, "DevLec"),
                new("SubmitCode", "Submit Code", "/CodeLab/Submit", "Submit solution for review", 7, 0, "DevLec"),
                new("MyProgress", "My Progress", "/My/Progress", "Track your learning progress", 8, 0, "DevLec"),
                new("Quizzes", "Quizzes", "/Courses/{courseId}/Quizzes", "Take course quizzes", 9, 0, "DevLec"),
                new("Certificate", "Certificate", "/My/Certificate", "Course completion certificates", 10, 0, "DevLec"),
                new("Discussions", "Discussions", "/Discussions", "Student and instructor Q&A", 11, 0, "DevLec"),
                new("Profile", "My Profile", "/Profile", "User profile and settings", 12, 0, "DevLec"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "DevLec"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "DevLec"),
                new("Admin", "Admin Dashboard", "/Admin", "Admin main page", 15, 1, "DevLec"),
                new("CourseAdmin", "Manage Courses", "/Admin/Courses", "Course creation and editing", 16, 1, "DevLec"),
                new("LessonAdmin", "Manage Lessons", "/Admin/Lessons", "Lesson management", 17, 1, "DevLec"),
                new("UserAdmin", "Manage Users", "/Admin/Users", "User management", 18, 1, "DevLec"),
                new("SubmissionReview", "Submission Review", "/Admin/Submissions", "Code submission review", 19, 1, "DevLec"),
                new("SiteSettings", "Site Settings", "/Admin/Settings", "Platform configuration", 20, 1, "DevLec"),

                // Hawaso용: DisplayOrder 순
                new("Home", "Home", "/", "Main landing page", 1, 0, "Hawaso"),
                new("Notices", "Notices", "/Boards/Notices", "Notice board", 2, 0, "Hawaso"),
                new("FreeBoard", "Free Board", "/Boards/Free", "General discussion board", 3, 0, "Hawaso"),
                new("QnA", "Q&A", "/Boards/QnA", "Question and Answer board", 4, 0, "Hawaso"),
                new("Gallery", "Gallery", "/Gallery", "Image gallery", 5, 0, "Hawaso"),
                new("Files", "Files", "/Files", "File downloads", 6, 0, "Hawaso"),
                new("Categories", "Categories", "/Boards/Categories", "Post category management", 7, 0, "Hawaso"),
                new("Tags", "Tags", "/Boards/Tags", "Tag management", 8, 0, "Hawaso"),
                new("Search", "Search", "/Search", "Content search", 9, 0, "Hawaso"),
                new("MyPosts", "My Posts", "/My/Posts", "User's own posts", 10, 0, "Hawaso"),
                new("MyComments", "My Comments", "/My/Comments", "User's comments", 11, 0, "Hawaso"),
                new("Profile", "Profile", "/Profile", "Edit personal profile", 12, 0, "Hawaso"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "Hawaso"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "Hawaso"),
                new("Admin", "Admin Panel", "/Admin", "Admin dashboard", 15, 1, "Hawaso"),
                new("BoardAdmin", "Board Admin", "/Admin/Boards", "Board settings and management", 16, 1, "Hawaso"),
                new("GalleryAdmin", "Gallery Admin", "/Admin/Gallery", "Gallery configuration", 17, 1, "Hawaso"),
                new("FileAdmin", "File Admin", "/Admin/Files", "Uploaded file management", 18, 1, "Hawaso"),
                new("UserAdmin", "User Admin", "/Admin/Users", "Manage users and roles", 19, 1, "Hawaso"),
                new("SiteSettings", "Site Settings", "/Admin/Settings", "Global site configuration", 20, 1, "Hawaso"),

                // MemoEngine용: DisplayOrder 순
                new("Home", "Home", "/", "Main page", 1, 0, "MemoEngine"),
                new("Blog", "Blog", "/Blog", "Blog post list", 2, 0, "MemoEngine"),
                new("Post", "Post Detail", "/Blog/{slug}", "Individual blog post", 3, 0, "MemoEngine"),
                new("Tags", "Tags", "/Blog/Tags", "Post tags", 4, 0, "MemoEngine"),
                new("Categories", "Categories", "/Blog/Categories", "Blog categories", 5, 0, "MemoEngine"),
                new("Archives", "Archives", "/Blog/Archives", "Monthly archive of posts", 6, 0, "MemoEngine"),
                new("Search", "Search", "/Search", "Search blog or boards", 7, 0, "MemoEngine"),
                new("Boards", "Boards", "/Boards", "All boards list", 8, 0, "MemoEngine"),
                new("BoardDetail", "Board Detail", "/Boards/{boardName}", "Posts by board", 9, 0, "MemoEngine"),
                new("Write", "Write Post", "/Boards/{boardName}/Write", "Write or edit a post", 10, 1, "MemoEngine"),
                new("Files", "Files", "/Files", "File attachments", 11, 0, "MemoEngine"),
                new("Comments", "Comments", "/Comments", "View or manage comments", 12, 0, "MemoEngine"),
                new("Profile", "Profile", "/Profile", "User profile page", 13, 0, "MemoEngine"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 14, 0, "MemoEngine"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 15, 0, "MemoEngine"),
                new("Admin", "Admin Panel", "/Admin", "Admin dashboard", 16, 1, "MemoEngine"),
                new("BlogAdmin", "Blog Admin", "/Admin/Blog", "Manage blog posts", 17, 1, "MemoEngine"),
                new("BoardAdmin", "Board Admin", "/Admin/Boards", "Manage boards", 18, 1, "MemoEngine"),
                new("FileAdmin", "File Admin", "/Admin/Files", "Manage uploaded files", 19, 1, "MemoEngine"),
                new("Settings", "Site Settings", "/Admin/Settings", "Site-wide settings", 20, 1, "MemoEngine"),

                // JavaCampus용: DisplayOrder 순
                new("Home", "Home", "/", "JavaCampus main page", 1, 0, "JavaCampus"),
                new("Courses", "Courses", "/Courses", "List of available courses", 2, 0, "JavaCampus"),
                new("CourseDetail", "Course Detail", "/Courses/{courseId}", "Course content and syllabus", 3, 0, "JavaCampus"),
                new("Lessons", "Lessons", "/Courses/{courseId}/Lessons", "Lesson videos and content", 4, 0, "JavaCampus"),
                new("CodePlayground", "Code Playground", "/Playground", "Java coding practice area", 5, 0, "JavaCampus"),
                new("Assignments", "Assignments", "/Courses/{courseId}/Assignments", "Submit assignments", 6, 0, "JavaCampus"),
                new("Submissions", "My Submissions", "/My/Submissions", "Track assignment submissions", 7, 0, "JavaCampus"),
                new("Quizzes", "Quizzes", "/Courses/{courseId}/Quizzes", "Take quizzes for the course", 8, 0, "JavaCampus"),
                new("Exam", "Exams", "/Courses/{courseId}/Exam", "Midterm and final exams", 9, 0, "JavaCampus"),
                new("Certification", "Certificate", "/My/Certificate", "Earned course certificates", 10, 0, "JavaCampus"),
                new("Forum", "Forum", "/Forum", "Ask questions and help others", 11, 0, "JavaCampus"),
                new("Profile", "Profile", "/Profile", "Edit my student profile", 12, 0, "JavaCampus"),
                new("Login", "Login", "/Identity/Account/Login", "Login to JavaCampus", 13, 0, "JavaCampus"),
                new("Register", "Register", "/Identity/Account/Register", "Register a new account", 14, 0, "JavaCampus"),
                new("Admin", "Admin Dashboard", "/Admin", "Administrative area", 15, 1, "JavaCampus"),
                new("CourseAdmin", "Manage Courses", "/Admin/Courses", "Create and manage courses", 16, 1, "JavaCampus"),
                new("LessonAdmin", "Manage Lessons", "/Admin/Lessons", "Upload or edit lessons", 17, 1, "JavaCampus"),
                new("AssignmentAdmin", "Manage Assignments", "/Admin/Assignments", "Instructor assignment control", 18, 1, "JavaCampus"),
                new("UserAdmin", "User Management", "/Admin/Users", "Manage students and instructors", 19, 1, "JavaCampus"),
                new("Settings", "Platform Settings", "/Admin/Settings", "Global JavaCampus settings", 20, 1, "JavaCampus"),

                // Portal용: DisplayOrder 순
                new("Home", "Home", "/", "Corporate portal homepage", 1, 0, "Portal"),
                new("News", "Company News", "/News", "Latest company news and announcements", 2, 0, "Portal"),
                new("Notice", "Notices", "/Boards/Notice", "Important company notices", 3, 0, "Portal"),
                new("TeamBoard", "Team Board", "/Boards/Team", "Team-specific communications", 4, 0, "Portal"),
                new("Calendar", "Calendar", "/Calendar", "Company-wide schedule", 5, 0, "Portal"),
                new("Events", "Events", "/Events", "Internal company events", 6, 0, "Portal"),
                new("Employees", "Employee Directory", "/Employees", "Search for employees", 7, 0, "Portal"),
                new("OrgChart", "Organization Chart", "/OrgChart", "Company organizational structure", 8, 0, "Portal"),
                new("Links", "Quick Links", "/Links", "Useful internal/external links", 9, 0, "Portal"),
                new("MyPage", "My Page", "/My", "Personal dashboard", 10, 0, "Portal"),
                new("Memo", "Memo Pad", "/Memo", "Private memo/notes", 11, 0, "Portal"),
                new("Profile", "Profile", "/Profile", "Update my profile", 12, 0, "Portal"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "Portal"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "Portal"),
                new("Admin", "Admin Panel", "/Admin", "Portal admin dashboard", 15, 1, "Portal"),
                new("NewsAdmin", "News Management", "/Admin/News", "Create and manage news", 16, 1, "Portal"),
                new("BoardAdmin", "Board Management", "/Admin/Boards", "Manage portal boards", 17, 1, "Portal"),
                new("EmployeeAdmin", "Employee Admin", "/Admin/Employees", "Manage employee directory", 18, 1, "Portal"),
                new("LinkAdmin", "Link Admin", "/Admin/Links", "Manage quick links", 19, 1, "Portal"),
                new("Settings", "Portal Settings", "/Admin/Settings", "System-wide settings", 20, 1, "Portal"),

                // EmployeeLicensing용: DisplayOrder 순
                new("Home", "Home", "/", "Main dashboard", 1, 0, "EmployeeLicensing"),
                new("Employees", "Employees", "/Employees", "List of employees", 2, 0, "EmployeeLicensing"),
                new("EmployeeDetail", "Employee Detail", "/Employees/{id}", "Employee profile", 3, 0, "EmployeeLicensing"),
                new("Licenses", "Licenses", "/Licenses", "List of licenses", 4, 0, "EmployeeLicensing"),
                new("LicenseDetail", "License Detail", "/Licenses/{id}", "License detail view", 5, 0, "EmployeeLicensing"),
                new("AddLicense", "Add License", "/Licenses/Add", "Register a new license", 6, 1, "EmployeeLicensing"),
                new("VerifyLicense", "Verify License", "/Licenses/Verify", "Verify license status", 7, 0, "EmployeeLicensing"),
                new("ExpiringSoon", "Expiring Soon", "/Licenses/Expiring", "Licenses nearing expiration", 8, 0, "EmployeeLicensing"),
                new("BackgroundChecks", "Background Checks", "/BackgroundChecks", "List of background checks", 9, 0, "EmployeeLicensing"),
                new("SubmitCheck", "Submit Background Check", "/BackgroundChecks/Submit", "Submit a new background check", 10, 1, "EmployeeLicensing"),
                new("MyLicenses", "My Licenses", "/My/Licenses", "View my licenses", 11, 0, "EmployeeLicensing"),
                new("MyChecks", "My Checks", "/My/BackgroundChecks", "My background check requests", 12, 0, "EmployeeLicensing"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "EmployeeLicensing"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "EmployeeLicensing"),
                new("Profile", "Profile", "/Profile", "User profile page", 15, 0, "EmployeeLicensing"),
                new("Admin", "Admin Dashboard", "/Admin", "Administrative area", 16, 1, "EmployeeLicensing"),
                new("EmployeeAdmin", "Manage Employees", "/Admin/Employees", "Manage employee records", 17, 1, "EmployeeLicensing"),
                new("LicenseAdmin", "Manage Licenses", "/Admin/Licenses", "License type configuration", 18, 1, "EmployeeLicensing"),
                new("CheckAdmin", "Manage Background Checks", "/Admin/Checks", "Review and manage checks", 19, 1, "EmployeeLicensing"),
                new("SiteSettings", "Site Settings", "/Admin/Settings", "Platform configuration", 20, 1, "EmployeeLicensing"),

                // VendorLicensing용: DisplayOrder 순
                new("Home", "Home", "/", "Vendor Licensing main page", 1, 0, "VendorLicensing"),
                new("Vendors", "Vendors", "/Vendors", "List of registered vendors", 2, 0, "VendorLicensing"),
                new("VendorDetail", "Vendor Detail", "/Vendors/{vendorId}", "View specific vendor", 3, 0, "VendorLicensing"),
                new("Employees", "Vendor Employees", "/Vendors/{vendorId}/Employees", "List of vendor's employees", 4, 0, "VendorLicensing"),
                new("EmployeeDetail", "Employee Detail", "/Employees/{id}", "Vendor employee detail view", 5, 0, "VendorLicensing"),
                new("Licenses", "Licenses", "/Employees/{id}/Licenses", "Employee license records", 6, 0, "VendorLicensing"),
                new("SubmitLicense", "Submit License", "/Licenses/Submit", "Submit a new license", 7, 1, "VendorLicensing"),
                new("BackgroundChecks", "Background Checks", "/Employees/{id}/BackgroundChecks", "Employee background check history", 8, 0, "VendorLicensing"),
                new("SubmitCheck", "Submit Check", "/BackgroundChecks/Submit", "Submit background check request", 9, 1, "VendorLicensing"),
                new("ComplianceStatus", "Compliance Status", "/Compliance", "Vendor compliance overview", 10, 0, "VendorLicensing"),
                new("ExpiringSoon", "Expiring Licenses", "/Licenses/Expiring", "Licenses nearing expiration", 11, 0, "VendorLicensing"),
                new("AccessRequests", "Access Requests", "/Access/Requests", "Facility access requests", 12, 0, "VendorLicensing"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 13, 0, "VendorLicensing"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 14, 0, "VendorLicensing"),
                new("Profile", "Profile", "/Profile", "User profile page", 15, 0, "VendorLicensing"),
                new("Admin", "Admin Panel", "/Admin", "Admin dashboard", 16, 1, "VendorLicensing"),
                new("VendorAdmin", "Manage Vendors", "/Admin/Vendors", "Add/edit vendor records", 17, 1, "VendorLicensing"),
                new("EmployeeAdmin", "Manage Employees", "/Admin/Employees", "Manage vendor employees", 18, 1, "VendorLicensing"),
                new("LicenseAdmin", "License Management", "/Admin/Licenses", "Manage license definitions", 19, 1, "VendorLicensing"),
                new("Settings", "Settings", "/Admin/Settings", "Platform configuration", 20, 1, "VendorLicensing"),

                // InternalAudit용: DisplayOrder 순
                new("Home", "Home", "/", "Internal Audit home", 1, 0, "InternalAudit"),
                new("AuditPlan", "Audit Plan", "/AuditPlans", "Annual or periodic audit plans", 2, 0, "InternalAudit"),
                new("AuditDetail", "Audit Detail", "/AuditPlans/{id}", "Audit plan details", 3, 0, "InternalAudit"),
                new("RiskAssessment", "Risk Assessment", "/RiskAssessments", "Risk evaluation for entities", 4, 0, "InternalAudit"),
                new("FieldWork", "Field Work", "/FieldWorks", "In-progress audit work", 5, 0, "InternalAudit"),
                new("Findings", "Findings", "/Findings", "Audit findings and observations", 6, 0, "InternalAudit"),
                new("CorrectiveActions", "Corrective Actions", "/Actions", "Remediation and corrective actions", 7, 0, "InternalAudit"),
                new("Tracking", "Tracking Status", "/Actions/Tracking", "Status of action items", 8, 0, "InternalAudit"),
                new("Documents", "Documents", "/Documents", "Upload and manage audit documents", 9, 0, "InternalAudit"),
                new("MyAudits", "My Audits", "/My/Audits", "Audits assigned to me", 10, 0, "InternalAudit"),
                new("Login", "Login", "/Identity/Account/Login", "User login", 11, 0, "InternalAudit"),
                new("Register", "Register", "/Identity/Account/Register", "User registration", 12, 0, "InternalAudit"),
                new("Profile", "Profile", "/Profile", "User profile page", 13, 0, "InternalAudit"),
                new("Admin", "Admin Dashboard", "/Admin", "Administrative dashboard", 14, 1, "InternalAudit"),
                new("AuditAdmin", "Manage Audits", "/Admin/Audits", "Create and manage audit plans", 15, 1, "InternalAudit"),
                new("RiskAdmin", "Risk Admin", "/Admin/Risks", "Manage risk matrices and categories", 16, 1, "InternalAudit"),
                new("UserAdmin", "User Management", "/Admin/Users", "Manage audit team and stakeholders", 17, 1, "InternalAudit"),
                new("Settings", "System Settings", "/Admin/Settings", "Audit platform configuration", 18, 1, "InternalAudit"),
                new("Logs", "Activity Logs", "/Admin/Logs", "Audit trail and user actions", 19, 1, "InternalAudit"),
                new("Dashboard", "Analytics Dashboard", "/Dashboard", "Visual insights and KPIs", 20, 0, "InternalAudit"),

                // ReportWriter용: DisplayOrder 순
                new("Dashboard", "Dashboard", "Dashboard", "Dashboard", 1, 0, "ReportWriter"),
                new("DailyLog", "Daily Log", "DailyLog", "Daily Log", 2, 0, "ReportWriter"),
                new("BriefingLogs", "Briefing Log", "BriefingLogs", "Briefing Log", 3, 0, "ReportWriter"),
                new("Incident", "Incident", "Incident", "Incident", 4, 0, "ReportWriter"),
                new("Incident-AddWritten", "Incident-AddWritten", "", "Incident-AddWritten", 5, 1, "ReportWriter"),
                new("Incident-AddReviewed", "Incident-AddReviewed", "", "Incident-AddReviewed", 6, 1, "ReportWriter"),
                new("Incident-AddApproved", "Incident-AddApproved", "", "Incident-AddApproved", 7, 1, "ReportWriter"),
                new("Incident-ArchiveReport", "Incident-ArchiveReport", "", "Incident-ArchiveReport", 8, 1, "ReportWriter"),
                new("ArchivedIncidents", "ArchivedIncidents", "ArchivedIncidents", "ArchivedIncidents", 9, 0, "ReportWriter"),
                new("Violations", "Violations", "Violations", "Violations", 10, 0, "ReportWriter"),
                new("BannedCustomers", "BannedCustomers", "BannedCustomers", "BannedCustomers", 11, 0, "ReportWriter"),
                new("Employee", "Employee", "EmployeeFinder", "Employee", 12, 0, "ReportWriter"),
                new("Vendor", "Vendor", "VendorFinder", "Vendor", 13, 0, "ReportWriter"),
                new("VendorEmployee", "Vendor Employee", "VendorEmployeeFinder", "Vendor Employee", 14, 0, "ReportWriter"),
                new("Others", "Others", "SubjectFinder", "Others", 15, 0, "ReportWriter"),
                new("Analytics", "Analytics", "Analytics/Report", "Analytics", 16, 0, "ReportWriter"),
                new("Malfunction", "Malfunction", "Malfunction", "Malfunction", 17, 0, "ReportWriter"),
                new("Library", "Library", "Libraries", "Library", 18, 0, "ReportWriter"),
                new("Archive", "Archive", "Archives", "Archive", 19, 0, "ReportWriter"),
                new("Drop-Down Lists", "Drop-Down Lists", "", "Drop-Down Lists", 20, 0, "ReportWriter"),

                // AssetTracking용: DisplayOrder 순
                new("Dashboard", "Dashboard", "/", "Dashboard", 1, 0, "SAT"),
                new("Projects", "Projects", "/Projects", "Projects Management", 2, 0, "SAT"),
                new("Machines", "Machines", "/Machines", "Machines Management", 3, 0, "SAT"),
                new("Software", "Software", "/Medias", "Software Management", 4, 0, "SAT"),
                new("LogicBoardAccessLog", "Logic Board Access Log", "/Logics", "Logic Board Access Log", 5, 0, "SAT"),
                new("Roles", "Roles", "/Administrations/Roles", "Roles Management", 6, 0, "SAT"),
                new("Users", "Users", "/Administrations/Users", "Users Management", 7, 0, "SAT"),
                new("Users", "User Roles", "/Admin/UserRoles", "User Roles Management", 8, 0, "SAT"),
                new("Resources", "Page Permission", "/Resources/Manage", "Page Permission", 9, 0, "SAT"),
                new("Manufacturers", "Manufacturers", "/Manufacturers", "Manufacturers List", 10, 0, "SAT"),
                new("MachineTypes", "Machine Types", "/MachineTypes", "Machine Types List", 11, 0, "SAT"),
                new("Subcategories ", "Subcategories", "/Subcategories ", "Sub Category", 12, 0, "SAT"),
                new("Reasons", "Reasons", "/Reasons", "Reasons Management", 13, 0, "SAT"),
                new("Depots", "Asset Location", "/Depots", "Asset Location", 14, 0, "SAT"),
                new("Denominations", "Denominations", "/Denominations", "Denominations", 15, 0, "SAT"),
                new("ProgressiveTypes", "Progressive Types", "/ProgressiveTypes", "Progressive Types", 16, 0, "SAT"),
                new("MediaThemes", "Media Themes", "/MediaThemes", "Media Themes", 17, 0, "SAT"),
                new("VettingSteps", "Vetting Steps", "/VettingSteps", "Vetting Steps Management", 18, 0, "SAT"),
                new("ApplicationLogs", "Application Logs", "/Logs", "Application Logs", 19, 0, "SAT")
        };
    }

    /// <summary>
    /// 리소스 시드 아이템 레코드
    /// </summary>
    private record ResourceSeedItem(
        string Alias,
        string Title,
        string Route,
        string Description,
        int GroupOrder,
        int Step,
        string AppName
    );
}
