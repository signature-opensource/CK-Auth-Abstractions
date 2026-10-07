using System;

namespace CK.Core;

/// <summary>
/// Immutable authentication scheme use: the scheme name and when it was last used.
/// </summary>
public sealed class UserSchemeInfo
{
    /// <summary>
    /// Initializes a new <see cref="UserSchemeInfo"/>.
    /// </summary>
    /// <param name="name">The scheme name. Must not be null, empty or white space.</param>
    /// <param name="lastUsed">
    /// The last use. Must be <see cref="DateTimeKind.Utc"/> or <see cref="DateTimeKind.Unspecified"/>
    /// (considered to be Utc).
    /// </param>
    public UserSchemeInfo( string name, DateTime lastUsed )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        Throw.CheckArgument( "Kind must be Utc or Unspecified, not Local.", lastUsed.Kind != DateTimeKind.Local );
        Name = name;
        LastUsed = lastUsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind( lastUsed, DateTimeKind.Utc )
                    : lastUsed;
    }

    /// <summary>
    /// Gets the scheme name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the last time this scheme has been used (Utc).
    /// </summary>
    public DateTime LastUsed { get; }
}
