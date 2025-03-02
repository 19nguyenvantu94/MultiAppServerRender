﻿using WebhookClient.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static WebhookClient.ServiceDefaults.HttpClientExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace WebhookClient.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddAuthenticationServices();

        // Application services
        builder.Services.AddOptions<WebhookClientOptions>().BindConfiguration(nameof(WebhookClientOptions));
        builder.Services.AddSingleton<HooksRepository>();
        builder.Services.AddSingleton<WebhooksClient>();

        builder.Services.TryAddTransient<HttpClientAuthorizationDelegatingHandler>();

      
    }

    public static void AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;


        var identitySection = configuration.GetSection("Identity");

        var identityUrl = identitySection.GetRequiredValue("Url");
        var callBackUrl = identitySection.GetRequiredValue("Url");

        //var callBackUrl = configuration.GetRequiredValue("CallBackUrl");
        var sessionCookieLifetime = configuration.GetValue("SessionCookieLifetimeMinutes", 60);

        // Add Authentication services
        services.AddAuthorization();
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionCookieLifetime);

            // Must be distinct from WebApp's cookie name, otherwise the two sites will interfere
            // with each other when both are on localhost (yes, even when they are on different ports)
            options.Cookie.Name = ".AspNetCore.WebHooksClientIdentity";
        })
        .AddOpenIdConnect(options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = identityUrl.ToString();
            //options.SignedOutRedirectUri = callBackUrl.ToString();
            options.ClientId = "webhooksclient";
            options.ClientSecret = "secret";
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.RequireHttpsMetadata = false;
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("offline_access");
            options.Scope.Add("webhooks");
        });

        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();
    }
}
