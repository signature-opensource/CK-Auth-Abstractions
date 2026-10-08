using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Optional service that enables automatic account binding: a user that is logged in logs in with a
/// scheme for which it is not registered, and the scheme is bound to its account.
/// An implementation should require the <see cref="AuthLevel.Critical"/> level of the current user.
/// </summary>
public interface IAutoBindingAccountService : ISingletonAutoService
{
    /// <summary>
    /// Called when a login failed with <see cref="ActorLoginResult.IsUnregisteredUser"/> and a user is currently
    /// logged in (see <see cref="LoginContext.InitialAuthentication"/>).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The login context.</param>
    /// <returns>
    /// The login result of the current user with the new scheme bound, or null if the scheme is not bound
    /// (with an error set on the context or the standard "automatic account binding is disabled" error).
    /// </returns>
    ValueTask<ActorLoginResult?> BindAccountAsync( IActivityMonitor monitor, LoginContext context );
}
