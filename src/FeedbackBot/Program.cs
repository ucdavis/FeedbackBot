using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using FeedbackBot;
using FeedbackBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Add services to the container.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(oidc =>
{
    oidc.ClientId = builder.Configuration["Authentication:ClientId"];
    oidc.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
    oidc.Authority = builder.Configuration["Authentication:Authority"];
    oidc.ResponseType = OpenIdConnectResponseType.Code;
    oidc.Scope.Add("openid");
    oidc.Scope.Add("profile");
    oidc.Scope.Add("email");
    oidc.Scope.Add("eduPerson");
    oidc.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    };
});

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// infrastructure
builder.Services.AddTransient<IGitHubService, GitHubService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
