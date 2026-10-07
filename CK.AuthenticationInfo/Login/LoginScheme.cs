namespace CK.Core;

/// <summary>
/// Login scheme helpers. A scheme is a provider name, optionally followed by a dot and a suffix:
/// "Oidc.Azure" is handled by the "Oidc" provider (see <see cref="ILoginProvider.ProviderNames"/>).
/// </summary>
public static class LoginScheme
{
    /// <summary>
    /// Gets the provider name of a scheme: the scheme itself or the part before its first dot.
    /// </summary>
    /// <param name="scheme">The scheme.</param>
    /// <returns>The provider name.</returns>
    public static string GetProviderName( string scheme )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( scheme );
        int idx = scheme.IndexOf( '.' );
        Throw.CheckArgument( "The provider name cannot be empty.", idx != 0 );
        return idx < 0 ? scheme : scheme.Substring( 0, idx );
    }
}
