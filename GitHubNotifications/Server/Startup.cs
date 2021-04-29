using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using Azure.Storage.Blobs;
using Azure.Core.Extensions;
using System;
using Microsoft.AspNetCore.ResponseCompression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GitHubNotifications.Server.Controllers;
using Azure.Identity;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GitHubNotifications.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration) { Configuration = configuration; }
        public static string RequireOrganizationPolicy = "RequireOrganization";

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddSignalR();
            services.AddAzureClients(
                builder =>
                {
                    builder.AddBlobServiceClient(Configuration["ConnectionStrings:storageconnection:blob"], true);
                    builder.AddTableServiceClient(Configuration["ConnectionStrings:storageconnection"]);
                });
            services.AddResponseCompression(
                opts =>
                {
                    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                        new[] { "application/octet-stream" });
                });
            services.AddSingleton<NotificationsHub>();
            var cred = new ChainedTokenCredential(new TokenCredential[] { new ManagedIdentityCredential(), new AzureCliCredential() });
            // services.AddSingleton(new EventHubConsumerClient("$default", "githubwebhooks.servicebus.windows.net", "githubwebhooks", cred));
            string containerUri = Configuration["ConnectionStrings:storageconnection:blob"] + "checkpoint";
            services.AddSingleton(
                new EventProcessorClient(
                    new BlobContainerClient(new Uri(containerUri), cred),
                    Configuration["ConnectionStrings:storageconnection:eventhub:cg"],
                    Configuration["ConnectionStrings:storageconnection:eventhub:ns"],
                    Configuration["ConnectionStrings:storageconnection:eventhub:name"],
                    cred));

            services.AddAuthentication(
                    options =>
                    {
                        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    })
                .AddCookie(
                    options =>
                    {
                        options.LoginPath = "/Login";
                        options.AccessDeniedPath = "/Unauthorized";
                    })
                .AddOAuth(
                    "GitHub",
                    options =>
                    {
                        options.ClientId = Configuration["Github:ClientId"];
                        options.ClientSecret = Configuration["Github:ClientSecret"];
                        options.CallbackPath = new PathString("/signin-github");
                        options.Scope.Add("user:email");

                        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                        options.UserInformationEndpoint = "https://api.github.com/user";

                        options.SaveTokens = true;

                        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                        options.ClaimActions.MapJsonKey(ClaimConstants.Login, "login");
                        options.ClaimActions.MapJsonKey(ClaimConstants.Url, "html_url");
                        options.ClaimActions.MapJsonKey(ClaimConstants.Avatar, "avatar_url");
                        options.ClaimActions.MapJsonKey(ClaimConstants.Name, "name");

                        options.Events = new OAuthEvents
                        {
                            OnCreatingTicket = async context =>
                            {
                                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                                var response = await context.Backchannel.SendAsync(
                                    request,
                                    HttpCompletionOption.ResponseHeadersRead,
                                    context.HttpContext.RequestAborted);
                                response.EnsureSuccessStatusCode();

                                var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                                context.RunClaimActions(user.RootElement);
                                if (user.RootElement.TryGetProperty("organizations_url", out var organizationsUrlProperty))
                                {
                                    request = new HttpRequestMessage(HttpMethod.Get, organizationsUrlProperty.GetString());
                                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                                    response = await context.Backchannel.SendAsync(
                                        request,
                                        HttpCompletionOption.ResponseHeadersRead,
                                        context.HttpContext.RequestAborted);
                                    response.EnsureSuccessStatusCode();

                                    var orgNames = new StringBuilder();
                                    using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
                                    {
                                        bool isFirst = true;
                                        foreach (JsonElement element in document.RootElement.EnumerateArray())
                                        {
                                            if (isFirst)
                                            {
                                                isFirst = false;
                                            }
                                            else
                                            {
                                                orgNames.Append(",");
                                            }
                                            orgNames.Append(element.GetProperty("login").GetString());
                                        }
                                    }

                                    string msEmail = await GetMicrosoftEmailAsync(context);
                                    if (msEmail != null)
                                    {
                                        context.Identity.AddClaim(
                                            new Claim(ClaimConstants.Email, msEmail));
                                    }
                                    context.Identity.AddClaim(new Claim(ClaimConstants.Orgs, orgNames.ToString()));
                                }
                            }
                        };
                    });

            services.AddAuthorization();
            services.AddSingleton<IConfigureOptions<AuthorizationOptions>, ConfigureOrganizationPolicy>();
            services.AddSingleton<IAuthorizationHandler, OrganizationRequirementHandler>();
            services.AddHostedService<EventHubProcessor>();
        }
        private static async Task<string> GetMicrosoftEmailAsync(OAuthCreatingTicketContext context)
        {
            var message = new HttpRequestMessage(
                HttpMethod.Get,
                "https://api.github.com/user/emails");
            message.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                context.AccessToken);

            var response = await context.Backchannel.SendAsync(message);

            var respString = await response.Content.ReadAsStringAsync();
            try
            {
                using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
                {
                    foreach (JsonElement element in document.RootElement.EnumerateArray())
                    {
                        var address = element.GetProperty("email").GetString();
                        if (address != null && address.EndsWith("@microsoft.com", StringComparison.OrdinalIgnoreCase))
                        {
                            return address;
                        }
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                throw new Exception(respString, e);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapControllers();
                    endpoints.MapFallbackToFile("index.html");
                    endpoints.MapHub<NotificationsHub>("notificationshub");
                });
        }



    }

    internal static class StartupExtensions
    {
        public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(
            this AzureClientFactoryBuilder builder,
            string serviceUriOrConnectionString,
            bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddBlobServiceClient(serviceUri);
            }
            else
            {
                return builder.AddBlobServiceClient(serviceUriOrConnectionString);
            }
        }
    }
}