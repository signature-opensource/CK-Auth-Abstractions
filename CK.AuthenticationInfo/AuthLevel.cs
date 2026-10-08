namespace CK.Core;

/// <summary>
/// Defines the level of trust of an authenticated actor.
/// It is carried by the <see cref="ActorInfo.Level"/>.
/// </summary>
public enum AuthLevel
{
    /// <summary>
    /// No authentication: the actor is the <see cref="ActorInfo.Anonymous"/>.
    /// </summary>
    None = 0,

    /// <summary>
    /// The actor is known but not proven: an expired session or a weak actor
    /// (see <see cref="ActorInfo.WeakScheme"/>).
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
