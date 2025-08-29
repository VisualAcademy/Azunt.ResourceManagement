using Azunt.ResourceManagement;
using Azunt.Web.Components;
using Azunt.Web.Components.Account;
using Azunt.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

#region ResourceManagement 
builder.Services.Configure<ResourceSeederOptions>(builder.Configuration.GetSection("Database:ResourceSeeder"));

// Resource 모듈 등록 (AdoNet 모드 선택)
builder.Services.AddDependencyInjectionContainerForResourceApp(connectionString, Azunt.Models.Enums.RepositoryMode.EfCore);
builder.Services.AddTransient<ResourceAppDbContextFactory>();
#endregion

var app = builder.Build();

// DB 초기화
// DB 초기화
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ResourceInitialization");
    var config = sp.GetRequiredService<IConfiguration>();

    // 전체 초기화 토글 (옵션)
    var initEnabled = config.GetSection("Database").GetValue<bool>("InitializeOnStartup", true);
    if (!initEnabled)
    {
        logger.LogInformation("Database initialization is disabled by configuration.");
    }
    else
    {
        try
        {
            // (1) 테이블 생성 및 보강
            ResourcesTableBuilder.Run(sp, forMaster: true); // 또는 app.Services 그대로 유지 가능
            logger.LogInformation("Resources table schema ensured.");

            // (2) 시드: appsettings.json(Database:ResourceSeeder) 기준
            var seederOptions = sp.GetRequiredService<IOptions<ResourceSeederOptions>>().Value;

            if (seederOptions.Enable)
            {
                if (seederOptions.AppNames == null || seederOptions.AppNames.Count == 0)
                {
                    ResourceSeeder.InsertRequiredResources(connectionString, logger, appName: null);
                    logger.LogInformation("ResourceSeeder: seeded ALL apps (AppNames empty).");
                }
                else
                {
                    foreach (var appName in seederOptions.AppNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                    {
                        ResourceSeeder.InsertRequiredResources(connectionString, logger, appName);
                    }
                    logger.LogInformation("ResourceSeeder: seeded apps: {Apps}", string.Join(", ", seederOptions.AppNames));
                }
            }
            else
            {
                logger.LogInformation("ResourceSeeder: disabled by configuration.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Resources initialization.");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Azunt.Web.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
