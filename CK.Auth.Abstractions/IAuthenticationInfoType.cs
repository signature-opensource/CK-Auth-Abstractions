using System.Security.Claims;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace CK.Auth;

/// <summary>
/// Defines "non instance" functionalities (that would have been non extensible static methods) like 
/// builders and converters of the <see cref="IAuthenticationInfo"/> type.
/// </summary>
public interface IAuthenticationInfoType
{
    /// <summary>
    /// Gets the "non authenticated" information: it has a <see cref="IAuthenticationInfo.Level"/> equals to
    /// <see cref="AuthLevel.None"/> and an empty <see cref="IAuthenticationInfo.DeviceId"/>.
    /// It is semantically the same as a null reference.
    /// </summary>
    IAuthenticationInfo None { get; }

    /// <summary>
    /// Creates a new <see cref="IAuthenticationInfo"/>.
    /// </summary>
    /// <param name="user">The user (and actual user).</param>
    /// <param name="expires">When null or already expired, Level is <see cref="AuthLevel.Unsafe"/>.</param>
    /// <param name="criticalExpires">Optional critical expiration.</param>
    /// <param name="deviceId">Optional device identifier.</param>
    /// <returns>The authentication info.</returns>
    IAuthenticationInfo Create( IUserInfo? user, DateTime? expires = null, DateTime? criticalExpires = null, string? deviceId = null );

    /// <summary>
    /// Creates a <see cref="IAuthenticationInfo"/> from a ClaimsIdentity.
    /// Must return null if <paramref name="p"/> is null or if <see cref="ClaimsIdentity.AuthenticationType"/>
    /// is not the same as <see cref="IAuthenticationTypeSystem.ClaimAuthenticationType"/> or <see cref="IAuthenticationTypeSystem.ClaimAuthenticationTypeSimple"/>.
    /// </summary>
    /// <param name="p">The claims identity.</param>
    /// <returns>The extracted authentication info or null if <paramref name="p"/> is null.</returns>
    [return: NotNullIfNotNull( "p" )]
    IAuthenticationInfo? FromClaimsIdentity( ClaimsIdentity? p );

    /// <summary>
    /// Creates a <see cref="IAuthenticationInfo"/> from a JObject.
    /// Must return null if <paramref name="o"/> is null.
    /// Must throw <see cref="InvalidDataException"/> if the o is not valid.
    /// </summary>
    /// <param name="o">The Json object.</param>
    /// <returns>The extracted authentication info or null if <paramref name="o"/> is null.</returns>
    /// <exception cref="InvalidDataException">
    /// Whenever the object is not in the expected format.
    /// </exception>
    [return: NotNullIfNotNull( "o" )]
    IAuthenticationInfo? FromJObject( JObject? o );

    /// <summary>
    /// Exports a <see cref="IAuthenticationInfo"/> as a <see cref="ClaimsIdentityAnonymousNotAuthenticated"/> object.
    /// Returns null if <paramref name="info"/> is null.
    /// <para>
    /// When <paramref name="userInfoOnly"/> is true, the <see cref="ClaimsIdentity.AuthenticationType"/> is
    /// "CKA-S" (<see cref="IAuthenticationTypeSystem.ClaimAuthenticationTypeSimple"/>) and the created ClaimIdentity
    /// is simple and contains the safe user claims ("name", "id" and "schemes").
    /// Expirations and device identifier appear on this simple primary ClaimsIdentity.
    /// </para>
    /// <para>
    /// When <paramref name="userInfoOnly"/> is false, the claim's AuthenticationType is "CKA" (<see cref="IAuthenticationTypeSystem.ClaimAuthenticationType"/>)
    /// and the created ClaimIdentity contains the unsafe user claims. When the info is impersonated, the subordinated
    /// <see cref="ClaimsIdentity.Actor"/> contains the unsafe actual user claims.
    /// The <see cref="StdAuthenticationTypeSystem.AuthLevelKeyType"/> claim with the authentication level, the expirations
    /// and the device identifier appear on the Actor identity when impersonated, otherwise on the primary identity.
    /// </para>
    /// </summary>
    /// <param name="info">The authentication information.</param>
    /// <param name="userInfoOnly">
    /// True to add (safe) user claims (and ignore any impersonation) to a simple primary ClaimsIdentity,
    /// false to create a more complex ClaimsIdentity that uses the <see cref="ClaimsIdentity.Actor"/>.
    /// </param>
    /// <returns>Authentication information as a claim identity.</returns>
    [return: NotNullIfNotNull( "info" )]
    ClaimsIdentityAnonymousNotAuthenticated? ToClaimsIdentity( IAuthenticationInfo? info, bool userInfoOnly );

    /// <summary>
    /// Exports a <see cref="IAuthenticationInfo"/> as a JObject.
    /// Must return null if <paramref name="info"/> is null.
    /// </summary>
    /// <param name="info">The authentication info.</param>
    /// <returns>The Json object or null if <paramref name="info"/> is null.</returns>
    [return: NotNullIfNotNull( "info" )]
    JObject? ToJObject( IAuthenticationInfo? info );

    /// <summary>
    /// Writes the authentication information in binary format.
    /// </summary>
    /// <param name="w">The binary writer.</param>
    /// <param name="info">The authentication info to write.</param>
    void Write( BinaryWriter w, IAuthenticationInfo? info );

    /// <summary>
    /// Reads a authentication information in binary format.
    /// Must throw <see cref="InvalidDataException"/> if the binary data is not valid.
    /// </summary>
    /// <param name="r">The binary reader.</param>
    /// <returns>The authentication info. Can be null.</returns>
    /// <exception cref="InvalidDataException">
    /// Whenever the binary data can not be read.
    /// </exception>
    IAuthenticationInfo? Read( BinaryReader r );


}
