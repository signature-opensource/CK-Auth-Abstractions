using System;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Immutable actor information: identifier, name, authentication schemes and <see cref="Level"/>.
/// <para>
/// The level is provided when the actor information is built (or updated with <see cref="WithLevel(AuthLevel)"/>)
/// but two invariants always apply:
/// <list type="bullet">
///     <item>The anonymous (<see cref="ActorId"/> is 0) is always at <see cref="AuthLevel.None"/>.</item>
///     <item>An actor with the <see cref="WeakScheme"/> is at most at <see cref="AuthLevel.Unsafe"/>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ActorInfo
{
    /// <summary>
    /// The scheme name of the weak actors (actors that log in with their name only).
    /// An actor that has this scheme is at most at <see cref="AuthLevel.Unsafe"/> level.
    /// The name is matched case-insensitively.
    /// </summary>
    public const string WeakScheme = "Weak";

    /// <summary>
    /// The anonymous actor: <see cref="ActorId"/> is 0, <see cref="ActorName"/> is empty,
    /// no <see cref="Schemes"/> and <see cref="Level"/> is <see cref="AuthLevel.None"/>.
    /// </summary>
    public static readonly ActorInfo Anonymous = new ActorInfo( 0, null, AuthLevel.None );

    /// <summary>
    /// Initializes a new <see cref="ActorInfo"/>.
    /// </summary>
    /// <param name="actorId">The actor identifier. 0 is the anonymous identifier, negative values are valid.</param>
    /// <param name="actorName">The actor name. Must be null or empty if and only if <paramref name="actorId"/> is 0.</param>
    /// <param name="level">
    /// The authentication level. This is clamped: the anonymous is always <see cref="AuthLevel.None"/> and
    /// an actor with the <see cref="WeakScheme"/> is at most <see cref="AuthLevel.Unsafe"/>.
    /// Only the anonymous can be at <see cref="AuthLevel.None"/>.
    /// </param>
    /// <param name="schemes">The authentication schemes. Defaults to an empty list.</param>
    public ActorInfo( int actorId, string? actorName, AuthLevel level, ImmutableArray<SchemeInfo> schemes = default )
    {
        actorName ??= string.Empty;
        Throw.CheckArgument( "ActorName must be empty if and only if ActorId is 0.", (actorName.Length == 0) == (actorId == 0) );
        CheckLevel( actorId, level );
        if( schemes.IsDefault ) schemes = ImmutableArray<SchemeInfo>.Empty;
        foreach( var s in schemes ) Throw.CheckArgument( "Schemes must not contain null.", s != null );
        ActorId = actorId;
        ActorName = actorName;
        Schemes = schemes;
        Level = Clamp( level );
    }

    /// <summary>
    /// Gets the actor identifier. 0 is the anonymous.
    /// </summary>
    public int ActorId { get; }

    /// <summary>
    /// Gets the actor name. Never null, empty if and only if <see cref="ActorId"/> is 0.
    /// </summary>
    public string ActorName { get; }

    /// <summary>
    /// Gets the authentication schemes. Never default, may be empty.
    /// </summary>
    public ImmutableArray<SchemeInfo> Schemes { get; }

    /// <summary>
    /// Gets the authentication level of this actor.
    /// </summary>
    public AuthLevel Level { get; }

    /// <summary>
    /// Gets whether this actor is the anonymous (<see cref="ActorId"/> is 0).
    /// </summary>
    public bool IsAnonymous => ActorId == 0;

    /// <summary>
    /// Gets whether this actor has the <see cref="WeakScheme"/>.
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
    /// Returns this actor with a new level (or this instance if the clamped level doesn't change).
    /// The invariants apply (see <see cref="ActorInfo(int, string?, AuthLevel, ImmutableArray{SchemeInfo})"/>).
    /// </summary>
    /// <param name="level">The new level.</param>
    /// <returns>This or a new actor information.</returns>
    public ActorInfo WithLevel( AuthLevel level )
    {
        CheckLevel( ActorId, level );
        return Clamp( level ) == Level ? this : new ActorInfo( ActorId, ActorName, level, Schemes );
    }

    static void CheckLevel( int actorId, AuthLevel level )
    {
        Throw.CheckOutOfRangeArgument( level >= AuthLevel.None && level <= AuthLevel.Critical );
        Throw.CheckArgument( "Only the anonymous can be at None level.", actorId == 0 || level != AuthLevel.None );
    }

    AuthLevel Clamp( AuthLevel level )
    {
        if( ActorId == 0 ) return AuthLevel.None;
        if( level > AuthLevel.Unsafe && IsWeak ) return AuthLevel.Unsafe;
        return level;
    }

    /// <summary>
    /// Overridden to return the name, identifier and level (for debugging and logs).
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => ActorId == 0 ? "Anonymous" : $"{ActorName} (#{ActorId}, {Level})";
}
