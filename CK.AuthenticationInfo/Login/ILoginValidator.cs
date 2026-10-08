using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Optional login validation. When at least one validator exists, a login follows three steps:
/// <list type="number">
///     <item><see cref="ILoginProvider.LoginAsync"/> is called with a false <c>actualLogin</c>: only checks are done.</item>
///     <item>On success, every validator is called: any error set on the <see cref="LoginContext"/> cancels the login.</item>
///     <item>Only if no error has been set, <see cref="ILoginProvider.LoginAsync"/> is called again with a true <c>actualLogin</c>.</item>
/// </list>
/// </summary>
[IsMultiple]
public interface ILoginValidator : ISingletonAutoService
{
    /// <summary>
    /// Validates a login. Calls <see cref="LoginContext.SetError(string, string?)"/> to refuse it.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="loggedInActor">The actor that will be logged in.</param>
    /// <param name="context">The login context.</param>
    /// <returns>The awaitable.</returns>
    ValueTask ValidateLoginAsync( IActivityMonitor monitor, ActorInfo loggedInActor, LoginContext context );
}
