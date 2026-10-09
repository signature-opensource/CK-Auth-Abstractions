using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Immutable passkey of an actor, as registered: its credential identifier, its public key and the state that the
/// authentication center checks at each login (the signature counter, the backup flags).
/// <para>
/// Nothing here is secret: the private key never leaves the authenticator. The byte arrays are copied, an instance
/// cannot be changed by the code that built it.
/// </para>
/// </summary>
public sealed class PasskeyCredential
{
    /// <summary>
    /// The maximal length of a <see cref="CredentialId"/>: 1023 bytes (WebAuthn).
    /// </summary>
    public const int MaxCredentialIdLength = 1023;

    /// <summary>
    /// The maximal length of a <see cref="UserHandle"/>: 64 bytes (WebAuthn).
    /// </summary>
    public const int MaxUserHandleLength = 64;

    /// <summary>
    /// Initializes a new <see cref="PasskeyCredential"/>.
    /// </summary>
    /// <param name="credentialId">The credential identifier. 1 to <see cref="MaxCredentialIdLength"/> bytes.</param>
    /// <param name="actorId">The actor that owns the passkey. Must be positive (never the anonymous).</param>
    /// <param name="userHandle">The user handle of the actor. 1 to <see cref="MaxUserHandleLength"/> bytes.</param>
    /// <param name="publicKey">The public key, as a COSE_Key. Must not be empty.</param>
    /// <param name="signCount">The last signature counter.</param>
    /// <param name="backupEligible">Whether the passkey can be backed up (synchronized). It never changes.</param>
    /// <param name="backupState">Whether the passkey was backed up at its last use.</param>
    /// <param name="aaguid">The model of the authenticator, as declared by it (not attested: for display only).</param>
    /// <param name="transports">The transports declared by the authenticator ("internal", "hybrid", "usb"...).</param>
    /// <param name="name">The name given to the passkey ("My phone"). Must not be null or white space.</param>
    public PasskeyCredential( ReadOnlyMemory<byte> credentialId,
                              int actorId,
                              ReadOnlyMemory<byte> userHandle,
                              ReadOnlyMemory<byte> publicKey,
                              uint signCount,
                              bool backupEligible,
                              bool backupState,
                              Guid aaguid,
                              IEnumerable<string> transports,
                              string name )
    {
        Throw.CheckArgument( credentialId.Length is > 0 and <= MaxCredentialIdLength );
        Throw.CheckArgument( actorId > 0 );
        Throw.CheckArgument( userHandle.Length is > 0 and <= MaxUserHandleLength );
        Throw.CheckArgument( publicKey.Length > 0 );
        Throw.CheckArgument( "A passkey that is not backup eligible cannot be backed up.", backupEligible || !backupState );
        Throw.CheckNotNullArgument( transports );
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        var t = transports.ToArray();
        Throw.CheckArgument( "Transports must not be null or white space.", t.All( s => !string.IsNullOrWhiteSpace( s ) ) );
        CredentialId = credentialId.ToArray();
        ActorId = actorId;
        UserHandle = userHandle.ToArray();
        PublicKey = publicKey.ToArray();
        SignCount = signCount;
        BackupEligible = backupEligible;
        BackupState = backupState;
        Aaguid = aaguid;
        Transports = t;
        Name = name;
    }

    /// <summary>
    /// Gets the credential identifier, chosen by the authenticator. It is unique across all the actors.
    /// </summary>
    public ReadOnlyMemory<byte> CredentialId { get; }

    /// <summary>
    /// Gets the actor that owns this passkey.
    /// </summary>
    public int ActorId { get; }

    /// <summary>
    /// Gets the user handle of the actor: an opaque random value (never the actor identifier), the same for all the
    /// passkeys of the actor.
    /// </summary>
    public ReadOnlyMemory<byte> UserHandle { get; }

    /// <summary>
    /// Gets the public key, as a COSE_Key.
    /// </summary>
    public ReadOnlyMemory<byte> PublicKey { get; }

    /// <summary>
    /// Gets the signature counter of the last use. Synchronized passkeys usually always send 0.
    /// </summary>
    public uint SignCount { get; }

    /// <summary>
    /// Gets whether the passkey can be backed up (synchronized between devices). It never changes.
    /// </summary>
    public bool BackupEligible { get; }

    /// <summary>
    /// Gets whether the passkey was backed up at its last use.
    /// </summary>
    public bool BackupState { get; }

    /// <summary>
    /// Gets the model of the authenticator, as declared by it. Without attestation it is not verified: it is for
    /// display only (a friendly name), never for a decision. Can be <see cref="Guid.Empty"/>.
    /// </summary>
    public Guid Aaguid { get; }

    /// <summary>
    /// Gets the transports declared by the authenticator: a hint for the browser.
    /// </summary>
    public IReadOnlyList<string> Transports { get; }

    /// <summary>
    /// Gets the name given to this passkey.
    /// </summary>
    public string Name { get; }
}
