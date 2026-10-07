namespace CK.Core;

/// <summary>
/// Provides the default <see cref="AuthenticationInfo"/> ambient service: <see cref="AuthenticationInfo.None"/>.
/// </summary>
public sealed class AuthenticationInfoDefaultProvider : IAmbientServiceDefaultProvider<AuthenticationInfo>
{
    /// <summary>
    /// Gets <see cref="AuthenticationInfo.None"/>.
    /// </summary>
    public AuthenticationInfo Default => AuthenticationInfo.None;
}
