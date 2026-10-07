using System;

namespace CK.Core;

/// <summary>
/// Immutable authentication information: the actual user, the (possibly impersonated) user, the session
/// expiration dates and the device identifier.
/// <para>
/// The levels are carried by the users (<see cref="UserInfo.Level"/>). This class only clamps them:
/// <list type="bullet">
///     <item>No valid <see cref="Expires"/>: the levels are at most <see cref="AuthLevel.Unsafe"/>.</item>
///     <item>No valid <see cref="CriticalExpires"/>: the levels are at most <see cref="AuthLevel.Normal"/>.</item>
///     <item>The user's level is never greater than the actual user's level.</item>
/// </list>
/// The functional updaters (<see cref="CheckExpiration(DateTime)"/>, <see cref="WithExpires(DateTime?, DateTime)"/>, etc.)
/// can only lower the levels: raising a level requires a new authentication (a new <see cref="Create(UserInfo, DateTime?, DateTime?, string?, DateTime)"/>).
/// </para>
/// <para>
/// The <see cref="User"/> and <see cref="ActualUser"/> are the anonymous when their level is below <see cref="AuthLevel.Normal"/>:
/// using an unsafe identity requires an explicit use of <see cref="UnsafeUser"/> or <see cref="UnsafeActualUser"/>.
/// </para>
/// <para>
/// This object carries no proof: building one is not authenticating anybody. A trusted instance is the
/// one obtained from a verified authentication token.
/// </para>
/// </summary>
public sealed class AuthenticationInfo : IAmbientAutoService
{
    readonly UserInfo _actualUser;
    readonly UserInfo _user;
    readonly DateTime? _expires;
    readonly DateTime? _criticalExpires;
    readonly string _deviceId;

    /// <summary>
    /// The anonymous authentication without device identifier.
    /// </summary>
    public static readonly AuthenticationInfo None = new AuthenticationInfo( UserInfo.Anonymous, UserInfo.Anonymous, null, null, string.Empty );

    AuthenticationInfo( UserInfo actualUser, UserInfo user, DateTime? expires, DateTime? criticalExpires, string deviceId )
    {
        _actualUser = actualUser;
        _user = user;
        _expires = expires;
        _criticalExpires = criticalExpires;
        _deviceId = deviceId;
    }

    /// <summary>
    /// Creates a new non impersonated authentication information.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="expires">The session expiration. When null or expired, the level is at most <see cref="AuthLevel.Unsafe"/>.</param>
    /// <param name="criticalExpires">The critical expiration. Clamped to <paramref name="expires"/>.</param>
    /// <param name="deviceId">The optional device identifier.</param>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>The authentication information.</returns>
    public static AuthenticationInfo Create( UserInfo user, DateTime? expires, DateTime? criticalExpires, string? deviceId, DateTime utcNow )
    {
        Throw.CheckNotNullArgument( user );
        return Create( user, user, expires, criticalExpires, deviceId, utcNow );
    }

    /// <summary>
    /// Creates a new authentication information where <paramref name="user"/> may be impersonated by
    /// the <paramref name="actualUser"/>.
    /// <para>
    /// This doesn't check the impersonation rules (use <see cref="Impersonate(UserInfo, DateTime)"/> for this)
    /// except that a weak actor (see <see cref="UserInfo.WeakScheme"/>) can never impersonate anybody.
    /// </para>
    /// </summary>
    /// <param name="actualUser">The actual user.</param>
    /// <param name="user">The user.</param>
    /// <param name="expires">The session expiration. When null or expired, the levels are at most <see cref="AuthLevel.Unsafe"/>.</param>
    /// <param name="criticalExpires">The critical expiration. Clamped to <paramref name="expires"/>.</param>
    /// <param name="deviceId">The optional device identifier.</param>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>The authentication information.</returns>
    public static AuthenticationInfo Create( UserInfo actualUser, UserInfo user, DateTime? expires, DateTime? criticalExpires, string? deviceId, DateTime utcNow )
    {
        Throw.CheckNotNullArgument( actualUser );
        Throw.CheckNotNullArgument( user );
        Throw.CheckArgument( "A weak actor cannot impersonate.", !actualUser.IsWeak || actualUser.UserId == user.UserId );
        CheckUtcNow( utcNow );
        return Normalize( null, actualUser, user, expires, criticalExpires, deviceId ?? string.Empty, utcNow );
    }

    /// <summary>
    /// The validity of a restored authentication (see <see cref="CreateRestored(UserInfo, UserInfo, AuthLevel, string?, DateTime)"/>):
    /// it covers one execution. This is one hour.
    /// </summary>
    public static readonly TimeSpan RestoredValidity = TimeSpan.FromHours( 1 );

    /// <summary>
    /// Creates the authentication information of a command execution from the identifiers of a command that has been
    /// validated when it entered the System (identity restoration).
    /// <para>
    /// The origin dates are irrelevant (a delayed command would find them expired): the dates are relative to
    /// <paramref name="utcNow"/> and cover the execution (<see cref="RestoredValidity"/>):
    /// <list type="bullet">
    ///     <item><see cref="AuthLevel.Unsafe"/>: no dates.</item>
    ///     <item><see cref="AuthLevel.Normal"/>: <see cref="Expires"/> only.</item>
    ///     <item><see cref="AuthLevel.Critical"/>: <see cref="Expires"/> and <see cref="CriticalExpires"/>.</item>
    /// </list>
    /// The users are clamped to <paramref name="level"/> (and the invariants of <see cref="UserInfo"/> apply).
    /// A restored authentication must never be used to issue an authentication token.
    /// </para>
    /// </summary>
    /// <param name="actualUser">The actual user.</param>
    /// <param name="user">The user (the actual user when not impersonated).</param>
    /// <param name="level">
    /// The level required by the command: at least <see cref="AuthLevel.Unsafe"/>, unless <paramref name="actualUser"/>
    /// is the anonymous (the result is then at <see cref="AuthLevel.None"/>).
    /// </param>
    /// <param name="deviceId">The optional device identifier.</param>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>The restored authentication information.</returns>
    public static AuthenticationInfo CreateRestored( UserInfo actualUser, UserInfo user, AuthLevel level, string? deviceId, DateTime utcNow )
    {
        Throw.CheckNotNullArgument( actualUser );
        Throw.CheckOutOfRangeArgument( level >= AuthLevel.None && level <= AuthLevel.Critical );
        Throw.CheckArgument( "Only the anonymous can be restored at None level.", level != AuthLevel.None || actualUser.IsAnonymous );
        CheckUtcNow( utcNow );
        DateTime? expires = level >= AuthLevel.Normal ? utcNow + RestoredValidity : null;
        DateTime? criticalExpires = level == AuthLevel.Critical ? expires : null;
        // The dates clamp the users exactly to the level.
        return Create( actualUser, user, expires, criticalExpires, deviceId, utcNow );
    }

    /// <summary>
    /// Gets the user if its level is at least <see cref="AuthLevel.Normal"/>, otherwise the <see cref="UserInfo.Anonymous"/>.
    /// </summary>
    public UserInfo User => _user.Level >= AuthLevel.Normal ? _user : UserInfo.Anonymous;

    /// <summary>
    /// Gets the actual user if its level is at least <see cref="AuthLevel.Normal"/>, otherwise the <see cref="UserInfo.Anonymous"/>.
    /// </summary>
    public UserInfo ActualUser => _actualUser.Level >= AuthLevel.Normal ? _actualUser : UserInfo.Anonymous;

    /// <summary>
    /// Gets the user whatever its level is.
    /// </summary>
    public UserInfo UnsafeUser => _user;

    /// <summary>
    /// Gets the actual user whatever its level is.
    /// </summary>
    public UserInfo UnsafeActualUser => _actualUser;

    /// <summary>
    /// Gets whether the <see cref="UnsafeUser"/> is not the <see cref="UnsafeActualUser"/>.
    /// </summary>
    public bool IsImpersonated => _user.UserId != _actualUser.UserId;

    /// <summary>
    /// Gets the session expiration (Utc). Null when the levels are at most <see cref="AuthLevel.Unsafe"/>.
    /// </summary>
    public DateTime? Expires => _expires;

    /// <summary>
    /// Gets the critical expiration (Utc). Null or lower or equal to <see cref="Expires"/>.
    /// </summary>
    public DateTime? CriticalExpires => _criticalExpires;

    /// <summary>
    /// Gets the device identifier. Never null, empty when there is no device identifier.
    /// <para>
    /// A device identifier is not a secret and doesn't prove anything.
    /// </para>
    /// </summary>
    public string DeviceId => _deviceId;

    /// <summary>
    /// Lowers the levels if <see cref="Expires"/> or <see cref="CriticalExpires"/> are reached.
    /// </summary>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>This or a new authentication information.</returns>
    public AuthenticationInfo CheckExpiration( DateTime utcNow )
    {
        CheckUtcNow( utcNow );
        return Normalize( this, _actualUser, _user, _expires, _criticalExpires, _deviceId, utcNow );
    }

    /// <summary>
    /// Sets a new <see cref="Expires"/>. The levels can only be lowered.
    /// </summary>
    /// <param name="expires">The new session expiration.</param>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>This or a new authentication information.</returns>
    public AuthenticationInfo WithExpires( DateTime? expires, DateTime utcNow )
    {
        CheckUtcNow( utcNow );
        return Normalize( this, _actualUser, _user, expires, _criticalExpires, _deviceId, utcNow );
    }

    /// <summary>
    /// Sets a new <see cref="CriticalExpires"/>. It is clamped to <see cref="Expires"/> (this never extends
    /// the session) and the levels can only be lowered.
    /// </summary>
    /// <param name="criticalExpires">The new critical expiration.</param>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>This or a new authentication information.</returns>
    public AuthenticationInfo WithCriticalExpires( DateTime? criticalExpires, DateTime utcNow )
    {
        CheckUtcNow( utcNow );
        return Normalize( this, _actualUser, _user, _expires, criticalExpires, _deviceId, utcNow );
    }

    /// <summary>
    /// Sets a new <see cref="DeviceId"/>.
    /// </summary>
    /// <param name="deviceId">The new device identifier. Null is the same as the empty string.</param>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>This or a new authentication information.</returns>
    public AuthenticationInfo WithDeviceId( string? deviceId, DateTime utcNow )
    {
        CheckUtcNow( utcNow );
        return Normalize( this, _actualUser, _user, _expires, _criticalExpires, deviceId ?? string.Empty, utcNow );
    }

    /// <summary>
    /// Impersonates the actual user into <paramref name="user"/>. The impersonated user's level is
    /// at most the actual user's one.
    /// <para>
    /// This throws an <see cref="InvalidOperationException"/> if the actual user's level is below <see cref="AuthLevel.Normal"/>
    /// (this covers the anonymous, the weak actors and the expired sessions).
    /// </para>
    /// <para>
    /// This is only the mechanism: whether an impersonation is allowed is the responsibility of
    /// the optional <see cref="IImpersonationService"/>. Without it, any impersonation is refused.
    /// </para>
    /// </summary>
    /// <param name="user">The user to impersonate. When it is the actual user, the impersonation is cleared.</param>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>This or a new authentication information.</returns>
    public AuthenticationInfo Impersonate( UserInfo user, DateTime utcNow )
    {
        Throw.CheckNotNullArgument( user );
        var current = CheckExpiration( utcNow );
        if( current._actualUser.Level < AuthLevel.Normal )
        {
            Throw.InvalidOperationException( $"Impersonation requires the actual user to be at least at Normal level (actual user: {current._actualUser})." );
        }
        return Normalize( current, current._actualUser, user, current._expires, current._criticalExpires, current._deviceId, utcNow );
    }

    /// <summary>
    /// Removes the impersonation if any: the <see cref="UnsafeActualUser"/> becomes the <see cref="UnsafeUser"/>.
    /// </summary>
    /// <param name="utcNow">The current date and time. Must be <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>This or a new authentication information.</returns>
    public AuthenticationInfo ClearImpersonation( DateTime utcNow )
    {
        CheckUtcNow( utcNow );
        return Normalize( this, _actualUser, _actualUser, _expires, _criticalExpires, _deviceId, utcNow );
    }

    /// <summary>
    /// Overridden to return a readable description (for debugging and logs).
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString()
    {
        var u = IsImpersonated ? $"{_user} impersonated by {_actualUser}" : _user.ToString();
        return _deviceId.Length > 0 ? $"{u} on device '{_deviceId}'" : u;
    }

    static void CheckUtcNow( DateTime utcNow )
    {
        Throw.CheckArgument( "Kind must be Utc.", utcNow.Kind == DateTimeKind.Utc );
    }

    static DateTime? NormalizeDate( DateTime? d, DateTime utcNow, string name )
    {
        if( !d.HasValue ) return null;
        var v = d.Value;
        if( v.Kind == DateTimeKind.Local ) Throw.ArgumentException( name, "Kind must be Utc or Unspecified, not Local." );
        if( v.Kind == DateTimeKind.Unspecified ) v = DateTime.SpecifyKind( v, DateTimeKind.Utc );
        return v > utcNow ? v : null;
    }

    static UserInfo AtMost( UserInfo u, AuthLevel max ) => u.Level <= max ? u : u.WithLevel( max );

    static AuthenticationInfo Normalize( AuthenticationInfo? current,
                                         UserInfo actualUser,
                                         UserInfo user,
                                         DateTime? expires,
                                         DateTime? criticalExpires,
                                         string deviceId,
                                         DateTime utcNow )
    {
        expires = NormalizeDate( expires, utcNow, nameof( expires ) );
        criticalExpires = NormalizeDate( criticalExpires, utcNow, nameof( criticalExpires ) );
        if( actualUser.IsAnonymous )
        {
            actualUser = user = UserInfo.Anonymous;
            expires = criticalExpires = null;
        }
        else
        {
            if( !expires.HasValue ) criticalExpires = null;
            else if( criticalExpires.HasValue && criticalExpires.Value > expires.Value ) criticalExpires = expires;
            var datesLevel = !expires.HasValue
                                ? AuthLevel.Unsafe
                                : !criticalExpires.HasValue
                                    ? AuthLevel.Normal
                                    : AuthLevel.Critical;
            actualUser = AtMost( actualUser, datesLevel );
            user = user.UserId == actualUser.UserId
                    ? actualUser
                    : AtMost( user, actualUser.Level );
        }
        if( current != null
            && ReferenceEquals( current._actualUser, actualUser )
            && ReferenceEquals( current._user, user )
            && current._expires == expires
            && current._criticalExpires == criticalExpires
            && current._deviceId == deviceId )
        {
            return current;
        }
        if( actualUser.IsAnonymous && deviceId.Length == 0 ) return None;
        return new AuthenticationInfo( actualUser, user, expires, criticalExpires, deviceId );
    }
}
