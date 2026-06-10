using FeedbackBot.Models;
using FeedbackBot.Security;
using FeedbackBot.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FeedbackBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(_ => Configuration);
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<AuthSettings>(Configuration.GetSection("Authentication"));

            services.AddHttpClient();
            services.AddTransient<IGitHubService, GitHubService>();

            services.AddDistributedMemoryCache();
            services.AddSession();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            var useLocalAuth = Configuration.GetValue<bool>("Authentication:UseLocalAuth");
            var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = useLocalAuth
                    ? LocalAuthenticationHandler.SchemeName
                    : CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = useLocalAuth
                    ? LocalAuthenticationHandler.SchemeName
                    : CookieAuthenticationDefaults.AuthenticationScheme;
            });

            authenticationBuilder.AddCookie(options =>
            {
                options.LoginPath = new PathString("/login");
            });

            if (useLocalAuth)
            {
                authenticationBuilder.AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(
                    LocalAuthenticationHandler.SchemeName,
                    options => { });
            }

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(routes =>
            {
                routes.MapDefaultControllerRoute();
            });
        }
    }
}
