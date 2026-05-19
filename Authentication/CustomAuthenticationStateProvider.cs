using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LawFlow.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage;
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        // In-memory cache of the current principal for this circuit. Avoids re-reading
        // ProtectedLocalStorage on every navigation — which can race against an
        // in-flight write and transiently return anonymous, breaking the post-login flow.
        private ClaimsPrincipal? _current;
        private bool _hydratedFromStorage;

        // One-shot flag the post-login flow uses to surface a welcome modal.
        // Set when login succeeds; cleared the first time MainLayout consumes it,
        // so refresh/back-nav doesn't keep replaying the modal.
        private bool _welcomePending;
        public bool ConsumeWelcomeFlag()
        {
            if (!_welcomePending) return false;
            _welcomePending = false;
            return true;
        }

        public CustomAuthenticationStateProvider(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // 1) If we've already authenticated (or signed out) this circuit, use the cache.
            if (_current != null)
                return new AuthenticationState(_current);

            // 2) Otherwise hydrate once from local storage (first call after circuit start).
            if (!_hydratedFromStorage)
            {
                _hydratedFromStorage = true;
                try
                {
                    var userSessionResult = await _localStorage.GetAsync<UserSession>("UserSession");
                    var userSession = userSessionResult.Success ? userSessionResult.Value : null;

                    if (userSession != null)
                    {
                        _current = BuildPrincipal(userSession);
                        return new AuthenticationState(_current);
                    }
                }
                catch
                {
                    // Pre-render or interop not ready — fall through to anonymous, but
                    // DO NOT cache anonymous here. We want a subsequent call (after JS
                    // interop is available) to be able to re-hydrate from storage.
                    _hydratedFromStorage = false;
                }
            }

            return new AuthenticationState(_anonymous);
        }

        public async Task UpdateAuthenticationState(UserSession? userSession)
        {
            ClaimsPrincipal claimsPrincipal;

            if (userSession != null)
            {
                await _localStorage.SetAsync("UserSession", userSession);
                claimsPrincipal = BuildPrincipal(userSession);
            }
            else
            {
                try { await _localStorage.DeleteAsync("UserSession"); } catch { /* best effort */ }
                claimsPrincipal = _anonymous;
            }

            _current = claimsPrincipal;
            _hydratedFromStorage = true;
            // Only flag the welcome on a real sign-in (not on sign-out).
            if (userSession != null) _welcomePending = true;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        private static ClaimsPrincipal BuildPrincipal(UserSession s) =>
            new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, s.UserId),
                new Claim(ClaimTypes.Name, s.UserName),
                new Claim(ClaimTypes.GivenName, s.FullName),
                new Claim(ClaimTypes.Role, s.Role)
            }, "CustomAuth"));
    }
}
