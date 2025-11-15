using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Adastral.Cockatoo.Common.AspNet;

partial class StartupGlue
{
    public static void Authentication(IServiceCollection services)
    {
        var cfg = AppConfig.Instance;
        if (cfg.Auth.OAuth.Count < 1) return;
        
        var auth = services.AddAuthentication()
            .AddCookie(JwtBearerDefaults.AuthenticationScheme);
        foreach (var item in cfg.Auth.OAuth)
        {
            auth.AddOpenIdConnect(
                item.Identifier,
                item.DisplayName,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.ClientId = item.ClientId;
                    options.ClientSecret = item.ClientSecret;
                    options.Authority = item.Endpoint;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.ResponseMode = "query";
                    options.Scope.Clear();
                    foreach (var x in item.Scopes)
                    {
                        options.Scope.Add(x);
                    }
                    options.SaveTokens = true;
                    // options.GetClaimsFromUserInfoEndpoint = true;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.TokenValidationParameters.RoleClaimType = "roles";
                    if (item.UseTokenLifetime.HasValue)
                    {
                        options.UseTokenLifetime = item.UseTokenLifetime.Value;
                    }
                    foreach (var inner in item.Jwt?.Items ?? [])
                    {
                        switch (inner.InternalName)
                        {
                            case "name":
                                options.TokenValidationParameters.NameClaimType = inner.JwtValue;
                                break;
                            case "role":
                                options.TokenValidationParameters.RoleClaimType = inner.JwtValue;
                                break;
                        }
                    }
                    if (!item.ValidateIssuer)
                    {
                        options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                        options.TokenValidationParameters.SignatureValidator
                            = (a, _) => new JsonWebToken(a);
                    }

                    Task EnsureHttpsRedirect(RedirectContext ctx)
                    {
                        if (AppConfig.Instance.PublicUrl.StartsWith("https://") &&
                            ctx.ProtocolMessage.RedirectUri.StartsWith("http://"))
                        {
                            ctx.ProtocolMessage.RedirectUri = "https://" + ctx.ProtocolMessage.RedirectUri[7..];
                        }
                        return Task.CompletedTask;
                    }
                    options.Events.OnRedirectToIdentityProvider += EnsureHttpsRedirect;
                    options.Events.OnRedirectToIdentityProviderForSignOut += EnsureHttpsRedirect;
                });
        }
    }
}