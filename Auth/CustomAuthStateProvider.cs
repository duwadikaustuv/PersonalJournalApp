using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace PersonalJournalApp.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private const string UserIdKey = "userId";

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var userId = Preferences.Get(UserIdKey, string.Empty);

            if (string.IsNullOrEmpty(userId))
            {
                // User not authenticated
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return Task.FromResult(new AuthenticationState(anonymous));
            }

            // User authenticated
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "Custom Authentication");
            var user = new ClaimsPrincipal(identity);

            return Task.FromResult(new AuthenticationState(user));
        }

        public Task MarkUserAsAuthenticated(string userId)
        {
            Preferences.Set(UserIdKey, userId);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "Custom Authentication");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));

            return Task.CompletedTask;
        }

        public Task MarkUserAsLoggedOut()
        {
            Preferences.Remove(UserIdKey);

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));

            return Task.CompletedTask;
        }

        public string? GetCurrentUserId()
        {
            return Preferences.Get(UserIdKey, string.Empty);
        }
    }
}