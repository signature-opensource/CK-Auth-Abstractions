using System;

namespace CK.Core;

/// <summary>
/// The payload of the <see cref="Scheme"/> scheme: what the authentication center has <b>verified</b> in a passkey
/// assertion. The "Passkey" <see cref="ILoginProvider"/> logs in the actor of the <see cref="CredentialId"/> and, on an
/// actual login, records the new <see cref="SignCount"/> and <see cref="BackupState"/>.
/// <para>
/// It holds no proof: it is trusted because only the authentication center builds it, after the verification of the
/// signature, and a payload never leaves the process that handles the login. It must never be built from values sent
/// by a client.
/// </para>
/// </summary>
public sealed class PasskeyLoginPayload
{
    /// <summary>
    /// The "Passkey" scheme (and provider name).
    /// </summary>
    public const string Scheme = "Passkey";

    /// <summary>
    /// Initializes a new <see cref="PasskeyLoginPayload"/>.
    /// </summary>
    /// <param name="credentialId">The verified credential identifier. 1 to <see cref="PasskeyCredential.MaxCredentialIdLength"/> bytes.</param>
    /// <param name="signCount">The signature counter of the verified assertion.</param>
    /// <param name="backupState">The backup state of the verified assertion.</param>
    public PasskeyLoginPayload( ReadOnlyMemory<byte> credentialId, uint signCount, bool backupState )
    {
        Throw.CheckArgument( credentialId.Length is > 0 and <= PasskeyCredential.MaxCredentialIdLength );
        CredentialId = credentialId.ToArray();
        SignCount = signCount;
        BackupState = backupState;
    }

    /// <summary>
    /// Gets the credential identifier of the verified assertion.
    /// </summary>
    public ReadOnlyMemory<byte> CredentialId { get; }

    /// <summary>
    /// Gets the signature counter of the verified assertion.
    /// </summary>
    public uint SignCount { get; }

    /// <summary>
    /// Gets the backup state of the verified assertion.
    /// </summary>
    public bool BackupState { get; }
}
