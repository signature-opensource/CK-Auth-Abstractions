using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Optional service that enables automatic account creation. This must be used with care: the
/// <see cref="LoginContext.UserData"/> should typically contain a short lived and verifiable value
/// (like an invitation token) that is required to actually create the account, and not all schemes
/// should be supported.
/// </summary>
public interface IAutoCreateAccountService : ISingletonAutoService
{
    /// <summary>
    /// Called when a login failed with <see cref="UserLoginResult.IsUnregisteredUser"/> and nobody is
    /// currently logged in (the <see cref="LoginContext.InitialAuthentication"/> actual user is the anonymous).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The login context.</param>
    /// <returns>
    /// The login result of the created (and logged in) account, or null if the account is not created
    /// (with an error set on the context or the standard "automatic registration is disabled" error).
    /// </returns>
    ValueTask<UserLoginResult?> CreateAccountAndLoginAsync( IActivityMonitor monitor, LoginContext context );
}
