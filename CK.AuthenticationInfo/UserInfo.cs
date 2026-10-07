using System;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Immutable user information: identifier, name, authentication schemes and <see cref="Level"/>.
/// <para>
/// The level is provided when the user information is built (or updated with <see cref="WithLevel(AuthLevel)"/>)
/// but two invariants always apply:
/// <list type="bullet">
///     <item>The anonymous (<see cref="UserId"/> is 0) is always at <see cref="AuthLevel.None"/>.</item>
///     <item>A user with the <see cref="WeakScheme"/> is at most at <see cref="AuthLevel.Unsafe"/>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class UserInfo
{
    /// <summary>
    /// The scheme name of the weak actors (actors that log in with their name only).
    /// A user that has this scheme is at most at <see cref="AuthLevel.Unsafe"/> level.
    /// The name is matched case-insensitively.
    /// </summary>
    public const string WeakScheme = "Weak";

    /// <summary>
    /// The anonymous user: <see cref="UserId"/> is 0, <see cref="UserName"/> is empty,
    /// no <see cref="Schemes"/> and <see cref="Level"/> is <see cref="AuthLevel.None"/>.
    /// </summary>
    public static readonly UserInfo Anonymous = new UserInfo( 0, null, AuthLevel.None );

    /// <summary>
    /// Initializes a new <see cref="UserInfo"/>.
    /// </summary>
    /// <param name="userId">The user identifier. 0 is the anonymous identifier, negative values are valid.</param>
    /// <param name="userName">The user name. Must be null or empty if and only if <paramref name="userId"/> is 0.</param>
    /// <param name="level">
    /// The authentication level. This is clamped: the anonymous is always <see cref="AuthLevel.None"/> and
    /// a user with the <see cref="WeakScheme"/> is at most <see cref="AuthLevel.Unsafe"/>.
    /// Only the anonymous can be at <see cref="AuthLevel.None"/>.
    /// </param>
    /// <param name="schemes">The authentication schemes. Defaults to an empty list.</param>
    public UserInfo( int userId, string? userName, AuthLevel level, ImmutableArray<UserSchemeInfo> schemes = default )
    {
        userName ??= string.Empty;
        Throw.CheckArgument( "UserName must be empty if and only if UserId is 0.", (userName.Length == 0) == (userId == 0) );
        CheckLevel( userId, level );
        if( schemes.IsDefault ) schemes = ImmutableArray<UserSchemeInfo>.Empty;
        foreach( var s in schemes ) Throw.CheckArgument( "Schemes must not contain null.", s != null );
        UserId = userId;
        UserName = userName;
        Schemes = schemes;
        Level = Clamp( level );
    }

    /// <summary>
    /// Gets the user identifier. 0 is the anonymous.
    /// </summary>
    public int UserId { get; }

    /// <summary>
    /// Gets the user name. Never null, empty if and only if <see cref="UserId"/> is 0.
    /// </summary>
    public string UserName { get; }

    /// <summary>
    /// Gets the authentication schemes. Never default, may be empty.
    /// </summary>
    public ImmutableArray<UserSchemeInfo> Schemes { get; }

    /// <summary>
    /// Gets the authentication level of this user.
    /// </summary>
    public AuthLevel Level { get; }

    /// <summary>
    /// Gets whether this user is the anonymous (<see cref="UserId"/> is 0).
    /// </summary>
    public bool IsAnonymous => UserId == 0;

    /// <summary>
    /// Gets whether this user has the <see cref="WeakScheme"/>.
    /// </summary>
    public bool IsWeak => HasScheme( WeakScheme );

    /// <summary>
    /// Gets whether a scheme exists in <see cref="Schemes"/> (the name is matched case-insensitively).
    /// </summary>
    /// <param name="name">The scheme name.</param>
    /// <returns>True if the scheme exists.</returns>
    public bool HasScheme( string name )
    {
        foreach( var s in Schemes )
        {
            if( string.Equals( s.Name, name, StringComparison.OrdinalIgnoreCase ) ) return true;
        }
        return false;
    }

    /// <summary>
    /// Returns this user with a new level (or this instance if the clamped level doesn't change).
    /// The invariants apply (see <see cref="UserInfo(int, string?, AuthLevel, ImmutableArray{UserSchemeInfo})"/>).
    /// </summary>
    /// <param name="level">The new level.</param>
    /// <returns>This or a new user information.</returns>
    public UserInfo WithLevel( AuthLevel level )
    {
        CheckLevel( UserId, level );
        return Clamp( level ) == Level ? this : new UserInfo( UserId, UserName, level, Schemes );
    }

    static void CheckLevel( int userId, AuthLevel level )
    {
        Throw.CheckOutOfRangeArgument( level >= AuthLevel.None && level <= AuthLevel.Critical );
        Throw.CheckArgument( "Only the anonymous can be at None level.", userId == 0 || level != AuthLevel.None );
    }

    AuthLevel Clamp( AuthLevel level )
    {
        if( UserId == 0 ) return AuthLevel.None;
        if( level > AuthLevel.Unsafe && IsWeak ) return AuthLevel.Unsafe;
        return level;
    }

    /// <summary>
    /// Overridden to return the name, identifier and level (for debugging and logs).
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => UserId == 0 ? "Anonymous" : $"{UserName} (#{UserId}, {Level})";
}
