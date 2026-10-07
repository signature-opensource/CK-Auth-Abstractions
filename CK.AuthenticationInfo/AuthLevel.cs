namespace CK.Core;

/// <summary>
/// Defines the level of trust of an authenticated user.
/// It is carried by the <see cref="UserInfo.Level"/>.
/// </summary>
public enum AuthLevel
{
    /// <summary>
    /// No authentication: the user is the <see cref="UserInfo.Anonymous"/>.
    /// </summary>
    None = 0,

    /// <summary>
    /// The user is known but not proven: an expired session or a weak actor
    /// (see <see cref="UserInfo.WeakScheme"/>).
    /// </summary>
    Unsafe = 1,

    /// <summary>
    /// Normal authentication.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Critical level: short lived and based on strong authentication (re-authentication,
    /// two-factor authentication, etc.).
    /// </summary>
    Critical = 3
}
