using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Indusoft.CalendarPlanning.Common
{
    public class ForbidSchemeHandler : IAuthenticationHandler
    {
        private HttpContext _context;

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            properties = properties ?? new AuthenticationProperties();
            _context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            properties = properties ?? new AuthenticationProperties();
            _context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
    }
}
