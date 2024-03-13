﻿using Fancy.ResourceLinker.Gateway.Authentication;
using Fancy.ResourceLinker.Gateway.Routing.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Forwarder;

namespace Fancy.ResourceLinker.Gateway.Routing;

/// <summary>
/// A http transformer used in combination with the http forwarder to add token if necessary.
/// </summary>
/// <seealso cref="Yarp.ReverseProxy.Forwarder.HttpTransformer" />
internal class GatewayForwarderHttpTransformer : HttpTransformer
{
    /// <summary>
    /// The send access token item key.
    /// </summary>
    internal static readonly string RouteNameItemKey = "RouteNameItemKey";

    /// <summary>
    /// The target URL item key.
    /// </summary>
    internal static readonly string TargetUrlItemKey = "TargetUrlItemKey";

    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        string? routeName = httpContext.Items[RouteNameItemKey]?.ToString();

        if(!string.IsNullOrEmpty(routeName))
        {
            // Add authentication
            AuthStrategyFactory authStrategyFactory = httpContext.RequestServices.GetRequiredService<AuthStrategyFactory>();
            IAuthStrategy authStrategy = authStrategyFactory.GetAuthStrategy(routeName);
            await authStrategy.SetAuthenticationAsync(httpContext.RequestServices, proxyRequest);
        }

        if(httpContext.Items.ContainsKey(TargetUrlItemKey))
        {
            // If an alternative target url ist provided use it
            string? targetUrl = Convert.ToString(httpContext.Items[TargetUrlItemKey]);
            if(targetUrl != null) proxyRequest.RequestUri = new Uri(targetUrl);
        }
        
        // Suppress the original request header, use the one from the destination Uri.
        proxyRequest.Headers.Host = null;
    }
}
