namespace CK.Core;

/// <summary>
/// The payload of the "Oidc" login provider (an <see cref="ILoginProvider"/> whose <see cref="ILoginProvider.CreatePayload"/>
/// creates it for an "Oidc.&lt;Suffix&gt;" scheme): the authentication center fills it with the identity that it has
/// verified (the validated identity token) before the login. It never leaves the process that handles the login.
/// </summary>
public interface IOidcLoginPayload
{
    /// <summary>
    /// Gets or sets the suffix of the scheme ("Google" for "Oidc.Google").
    /// </summary>
    string SchemeSuffix { get; set; }

    /// <summary>
    /// Gets or sets the subject identifier: unique for the issuer of the scheme.
    /// </summary>
    string Sub { get; set; }

    /// <summary>
    /// Gets or sets the claims of the identity, as received from the provider: a JSON object (the values as they are),
    /// without the protocol claims nor "sub". Empty means "not provided".
    /// </summary>
    string Claims { get; set; }
}
