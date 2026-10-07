using System.Text;

namespace CK.Core;

/// <summary>
/// The payload of the <see cref="Scheme"/> scheme: a user name and a password.
/// The basic login is a scheme like the others (see <see cref="ILoginProvider"/>).
/// </summary>
/// <param name="UserName">The user name.</param>
/// <param name="Password">The password. It never appears in <see cref="object.ToString()"/>.</param>
public sealed record BasicLoginPayload( string UserName, string Password )
{
    /// <summary>
    /// The "Basic" scheme (and provider name).
    /// </summary>
    public const string Scheme = "Basic";

    // The generated ToString() would print the password: it is redacted.
    bool PrintMembers( StringBuilder builder )
    {
        builder.Append( "UserName = " ).Append( UserName ).Append( ", Password = ***" );
        return true;
    }
}
