using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Optional service that decides how far a caller party (see <see cref="AuthenticationInfo.CallerPartyId"/>) may act
/// for the actors of the identities it brings. When no implementation is available, any caller party other than the
/// local one is refused.
/// <para>
/// Our own local party is never asked: it received the identity itself, so nothing is capped. Another party only
/// answers for the identities it sends: the receiver caps their levels with the maximal level returned here, so a
/// command that requires a higher level fails its validation.
/// </para>
/// </summary>
public interface ICallerPartyService : ISingletonAutoService
{
    /// <summary>
    /// Gets the maximal level at which the caller party may act for these actors.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="callerPartyId">The actor identifier of the caller party (never 0).</param>
    /// <param name="actualActor">The actual actor of the received identity.</param>
    /// <param name="actor">The actor of the received identity (the actual actor when not impersonated).</param>
    /// <returns>The maximal level, <see cref="AuthLevel.None"/> when the caller party is refused.</returns>
    ValueTask<AuthLevel> GetMaxLevelAsync( IActivityMonitor monitor, int callerPartyId, ActorInfo actualActor, ActorInfo actor );
}
