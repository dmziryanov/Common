using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Indusoft.CalendarPlanning.Common
{
    public class ClaimRequirementAttribute : TypeFilterAttribute
    {
        public ClaimRequirementAttribute(string claimValue) : base(typeof(ClaimRequirementFilter))
        {
            Arguments = new object[] { new Claim("WORK_AREA", claimValue) };
        }
    }


    public class ClaimRequirementFilter : IAuthorizationFilter
    {
        private const string denyClaim = "deny";
        readonly Claim _claim;
        private readonly IConfiguration _configuration;
        private static List<SecurityKey> keys;

        public ClaimRequirementFilter(Claim claim, IConfiguration configuration)
        {
            _claim = claim;
            _configuration = configuration;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var idToken = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            var issuer = ((IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration))).GetValue<string>("AuthenticationServiceUrl");

            var user = GetClaimsPrincipal(idToken, issuer);

            if (user is null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var hasDenyClaim = user.Claims.Any(c => c.Value == _claim.Value && _claim.Value.ToLower().Contains(denyClaim));


            if (hasDenyClaim)
            {
                context.Result = new ForbidResult();
                return;
            }

            //TODO: побить типы по клэймам c.Type == _claim.Type &&
            var hasClaim = user.Claims.Any(c => c.Value == _claim.Value);

            if (!hasClaim)
            {
                context.Result = new ForbidResult();
            }
        }

        private ClaimsPrincipal GetClaimsPrincipal(string idToken, string issuer)
        {
            var client = new System.Net.Http.HttpClient();
            var r = new DiscoveryDocumentRequest();
            r.Policy.RequireHttps = false;
            r.Address = issuer;


            var disco = client.GetDiscoveryDocumentAsync(r).Result;

            if (keys is null)
                keys = new List<SecurityKey>();

            foreach (var webKey in disco.KeySet.Keys)
            {
                var e = Base64Url.Decode(webKey.E);
                var n = Base64Url.Decode(webKey.N);

                var key = new RsaSecurityKey(new RSAParameters { Exponent = e, Modulus = n })
                {
                    KeyId = webKey.Kid
                };

                keys.Add(key);
            }

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = disco.Issuer,
                RequireAudience = false,
                ValidateAudience = false,
                IssuerSigningKey = keys.FirstOrDefault(),
                IssuerSigningKeys = keys,
                RequireSignedTokens = true
            };


            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                return tokenHandler.ValidateToken(idToken, parameters, out var _);
            }
            catch (Exception e)
            {
                return null;
            }

        }
    }
}
