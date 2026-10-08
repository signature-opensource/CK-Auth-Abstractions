using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Transport neutral context of a login, given to the <see cref="ILoginValidator"/>, <see cref="IAutoCreateAccountService"/>
/// and <see cref="IAutoBindingAccountService"/>. The first error set cancels the login.
/// </summary>
public sealed class LoginContext
{
    string? _errorId;
    string? _errorText;

    /// <summary>
    /// Initializes a new login context.
    /// </summary>
    /// <param name="scheme">The login scheme.</param>
    /// <param name="payload">The provider dependent payload.</param>
    /// <param name="initialAuthentication">The authentication before the login.</param>
    /// <param name="userData">Optional free key/value data that come with the login (an invitation token for instance).</param>
    public LoginContext( string scheme,
                         object payload,
                         AuthenticationInfo initialAuthentication,
                         IReadOnlyDictionary<string, string?>? userData = null )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( scheme );
        Throw.CheckNotNullArgument( payload );
        Throw.CheckNotNullArgument( initialAuthentication );
        Scheme = scheme;
        Payload = payload;
        InitialAuthentication = initialAuthentication;
        UserData = userData ?? ImmutableDictionary<string, string?>.Empty;
    }

    /// <summary>
    /// Gets the login scheme.
    /// </summary>
    public string Scheme { get; }

    /// <summary>
    /// Gets the provider dependent payload.
    /// </summary>
    public object Payload { get; }

    /// <summary>
    /// Gets the authentication before the login.
    /// </summary>
    public AuthenticationInfo InitialAuthentication { get; }

    /// <summary>
    /// Gets the free key/value data that come with the login.
    /// </summary>
    public IReadOnlyDictionary<string, string?> UserData { get; }

    /// <summary>
    /// Gets whether an error has been set.
    /// </summary>
    public bool HasError => _errorId != null;

    /// <summary>
    /// Gets the error identifier (a dotted identifier like "User.AccountBinding.CriticalLevelRequired"). Null when there is no error.
    /// </summary>
    public string? ErrorId => _errorId;

    /// <summary>
    /// Gets the error text. Null when there is no error.
    /// </summary>
    public string? ErrorText => _errorText;

    /// <summary>
    /// Sets an error: the login is cancelled. Only the first error is kept.
    /// This returns null so that a service can write <c>return context.SetError( ... );</c>.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="errorText">The error text. Defaults to the identifier.</param>
    /// <returns>Always null.</returns>
    public ActorLoginResult? SetError( string errorId, string? errorText = null )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( errorId );
        if( _errorId == null )
        {
            _errorId = errorId;
            _errorText = string.IsNullOrWhiteSpace( errorText ) ? errorId : errorText;
        }
        return null;
    }

    /// <summary>
    /// Sets an error from an exception: the identifier is the exception type full name and the text is its message.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <returns>Always null.</returns>
    public ActorLoginResult? SetError( Exception ex )
    {
        Throw.CheckNotNullArgument( ex );
        return SetError( ex.GetType().FullName ?? ex.GetType().Name, ex.Message );
    }
}
