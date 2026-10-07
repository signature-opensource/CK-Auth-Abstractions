using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Singleton context-less service that reads a <see cref="UserInfo"/> from its identifier.
/// <para>
/// This is the key of the identity restoration: a command validated when it entered the System carries
/// trustable actor identifiers, and its execution can be delayed or moved far from its origin. The
/// <see cref="AuthenticationInfo"/> is then restored from these identifiers, and reading the user again
/// gives its current state (a user that has been removed or disabled since cannot be restored).
/// </para>
/// </summary>
public interface IUserInfoProvider : ISingletonAutoService
{
    /// <summary>
    /// Obtains a <see cref="UserInfo"/> from its identifier.
    /// <para>
    /// The returned user is at the highest level it can reach (typically <see cref="AuthLevel.Critical"/>,
    /// and at most <see cref="AuthLevel.Unsafe"/> for a weak actor by construction): the session in which it
    /// is placed clamps it (see <see cref="AuthenticationInfo"/>).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="userId">The user identifier. Must not be 0.</param>
    /// <returns>The user or null if it doesn't exist or cannot be used anymore.</returns>
    ValueTask<UserInfo?> GetUserInfoAsync( IActivityMonitor monitor, int userId );
}
