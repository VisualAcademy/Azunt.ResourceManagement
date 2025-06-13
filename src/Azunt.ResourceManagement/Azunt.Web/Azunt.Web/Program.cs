using Azunt.ResourceManagement;
using Azunt.ResourceManagement.Initializers;
using Azunt.Web.Components;
using Azunt.Web.Components.Account;
using Azunt.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
// Resource 모듈 등록 (AdoNet 모드 선택)
builder.Services.AddDependencyInjectionContainerForResourceApp(connectionString, Azunt.Models.Enums.RepositoryMode.EfCore);
builder.Services.AddTransient<ResourceAppDbContextFactory>();
#endregion

var app = builder.Build();

// DB 초기화
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("ResourceInitialization");

    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // (1) 테이블 생성 및 보강 (테이블이 없으면 생성, 필드 없으면 보강)
    ResourcesTableBuilder.Run(app.Services, forMaster: true); // 마스터 DB용

    // (2) 시드 데이터 삽입 (앱 이름별로 분리 가능)
    ResourceSeeder.InsertRequiredResources(connectionString, logger, appName: null);
    ResourceSeeder.InsertRequiredResources(connectionString, logger, appName: "DotNetNote");
    // 필요한 경우 다수 AppName 호출 가능
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
