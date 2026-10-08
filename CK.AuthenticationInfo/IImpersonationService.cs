using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Optional service that allows impersonation. When no implementation is available, any impersonation is refused.
/// <para>
/// Impersonation is not an actual login: it must have no visible impact on the impersonated actor data.
/// The returned actor is placed in the session by <see cref="AuthenticationInfo.Impersonate(ActorInfo, System.DateTime)"/>:
/// its level is at most the actual actor's level.
/// </para>
/// </summary>
public interface IImpersonationService : ISingletonAutoService
{
    /// <summary>
    /// Attempts to impersonate the current actual actor into another one.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="current">The current authentication information.</param>
    /// <param name="actorId">The target actor identifier.</param>
    /// <returns>The actor to impersonate or null if the impersonation is not allowed.</returns>
    ValueTask<ActorInfo?> ImpersonateAsync( IActivityMonitor monitor, AuthenticationInfo current, int actorId );

    /// <summary>
    /// Attempts to impersonate the current actual actor into another one.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="current">The current authentication information.</param>
    /// <param name="actorName">The target actor name.</param>
    /// <returns>The actor to impersonate or null if the impersonation is not allowed.</returns>
    ValueTask<ActorInfo?> ImpersonateAsync( IActivityMonitor monitor, AuthenticationInfo current, string actorName );
}
