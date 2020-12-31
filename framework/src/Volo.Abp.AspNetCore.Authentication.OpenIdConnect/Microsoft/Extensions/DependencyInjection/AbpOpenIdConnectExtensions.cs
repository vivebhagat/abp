﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.MultiTenancy;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AbpOpenIdConnectExtensions
    {
        public static AuthenticationBuilder AddAbpOpenIdConnect(this AuthenticationBuilder builder)
            => builder.AddAbpOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddAbpOpenIdConnect(this AuthenticationBuilder builder, Action<OpenIdConnectOptions> configureOptions)
            => builder.AddAbpOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddAbpOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, Action<OpenIdConnectOptions> configureOptions)
            => builder.AddAbpOpenIdConnect(authenticationScheme, OpenIdConnectDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddAbpOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<OpenIdConnectOptions> configureOptions)
        {
            return builder.AddOpenIdConnect(authenticationScheme, displayName, options =>
            {
                options.ClaimActions.MapAbpClaimTypes();

                configureOptions?.Invoke(options);

                if(options.Events != null)
                {
                    var authorizationCodeReceived = options.Events.OnAuthorizationCodeReceived;
                    options.Events.OnAuthorizationCodeReceived = receivedContext =>
                    {
                        AbpOnAuthorizationCodeReceived(receivedContext);
                        return authorizationCodeReceived(receivedContext);
                    };
                }
                else
                {
                    options.Events = new OpenIdConnectEvents
                    {
                        OnAuthorizationCodeReceived = AbpOnAuthorizationCodeReceived
                    };
                }
            });
        }

        private static Task AbpOnAuthorizationCodeReceived(AuthorizationCodeReceivedContext receivedContext)
        {
            var tenantKey = receivedContext.HttpContext.RequestServices
                .GetRequiredService<IOptionsSnapshot<AbpAspNetCoreMultiTenancyOptions>>().Value.TenantKey;

            if (receivedContext.Request.Cookies.ContainsKey(tenantKey))
            {
                receivedContext.TokenEndpointRequest.SetParameter(tenantKey,
                    receivedContext.Request.Cookies[tenantKey]);
            }

            return Task.CompletedTask;
        }
    }
}
