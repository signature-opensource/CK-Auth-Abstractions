using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Optional service that allows impersonation. When no implementation is available, any impersonation is refused.
/// <para>
/// Impersonation is not an actual login: it must have no visible impact on the impersonated user data.
/// The returned user is placed in the session by <see cref="AuthenticationInfo.Impersonate(UserInfo, System.DateTime)"/>:
/// its level is at most the actual user's level.
/// </para>
/// </summary>
public interface IImpersonationService : ISingletonAutoService
{
    /// <summary>
    /// Attempts to impersonate the current actual user into another one.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="current">The current authentication information.</param>
    /// <param name="userId">The target user identifier.</param>
    /// <returns>The user to impersonate or null if the impersonation is not allowed.</returns>
    ValueTask<UserInfo?> ImpersonateAsync( IActivityMonitor monitor, AuthenticationInfo current, int userId );

    /// <summary>
    /// Attempts to impersonate the current actual user into another one.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="current">The current authentication information.</param>
    /// <param name="userName">The target user name.</param>
    /// <returns>The user to impersonate or null if the impersonation is not allowed.</returns>
    ValueTask<UserInfo?> ImpersonateAsync( IActivityMonitor monitor, AuthenticationInfo current, string userName );
}
