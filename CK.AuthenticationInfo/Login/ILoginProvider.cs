using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Handles the login for one or more providers. There can be multiple implementations (the database providers,
/// the weak actors, etc.): the authentication center aggregates them.
/// <para>
/// A scheme is a provider name, optionally followed by a dot and a suffix: "Oidc.Azure" is handled by the "Oidc"
/// provider (see <see cref="LoginScheme.GetProviderName(string)"/>). The basic login is the <see cref="BasicLoginPayload.Scheme"/>
/// provider with a <see cref="BasicLoginPayload"/>.
/// </para>
/// <para>
/// The payload is provider dependent. It never leaves the process that handles the login.
/// </para>
/// </summary>
[IsMultiple]
public interface ILoginProvider : ISingletonAutoService
{
    /// <summary>
    /// Gets the provider names handled by this provider.
    /// </summary>
    IReadOnlyCollection<string> ProviderNames { get; }

    /// <summary>
    /// Creates a new, empty, provider dependent payload for a scheme (that is typically filled
    /// from the claims of a remote authentication).
    /// Throws an <see cref="ArgumentException"/> if the provider has no such payload.
    /// </summary>
    /// <param name="scheme">The scheme.</param>
    /// <returns>A new payload.</returns>
    object CreatePayload( string scheme );

    /// <summary>
    /// Attempts to login. Throws an <see cref="ArgumentException"/> if the scheme is not handled by this
    /// provider or if the payload is not compatible.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="scheme">The scheme.</param>
    /// <param name="payload">The provider dependent payload.</param>
    /// <param name="actualLogin">
    /// False to avoid any login side effect on success (like updating a last login time): only checks are done.
    /// This enables a validation step (see <see cref="ILoginValidator"/>) before the actual login.
    /// </param>
    /// <returns>The login result.</returns>
    ValueTask<UserLoginResult> LoginAsync( IActivityMonitor monitor, string scheme, object payload, bool actualLogin = true );
}
