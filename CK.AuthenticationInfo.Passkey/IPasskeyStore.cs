using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// The storage of the passkeys, implemented by a back-end package (a database for instance). The authentication center
/// runs the WebAuthn protocol and verifies; this store only keeps the passkeys.
/// <para>
/// The signature counter and the backup state are updated by the "Passkey" <see cref="ILoginProvider"/> on an actual
/// login (see <see cref="PasskeyLoginPayload"/>), not by this store.
/// </para>
/// </summary>
public interface IPasskeyStore : ISingletonAutoService
{
    /// <summary>
    /// Gets the user handle of an actor, created on the first call: random, never derived from the actor identifier,
    /// the same for all the passkeys of the actor.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="actorId">The actor identifier. Must be positive.</param>
    /// <returns>The user handle (1 to <see cref="PasskeyCredential.MaxUserHandleLength"/> bytes).</returns>
    ValueTask<ReadOnlyMemory<byte>> GetOrCreateUserHandleAsync( IActivityMonitor monitor, int actorId );

    /// <summary>
    /// Finds a passkey by its credential identifier.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="credentialId">The credential identifier.</param>
    /// <returns>The passkey or null if there is no such credential.</returns>
    ValueTask<PasskeyCredential?> FindAsync( IActivityMonitor monitor, ReadOnlyMemory<byte> credentialId );

    /// <summary>
    /// Gets the passkeys of an actor (when a new one is registered, the authenticators that already have one are excluded).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="actorId">The actor identifier.</param>
    /// <returns>The passkeys of the actor (possibly empty).</returns>
    ValueTask<IReadOnlyList<PasskeyCredential>> GetCredentialsAsync( IActivityMonitor monitor, int actorId );

    /// <summary>
    /// Stores a new passkey. Its <see cref="PasskeyCredential.UserHandle"/> must be the one of its actor
    /// (see <see cref="GetOrCreateUserHandleAsync"/>).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="credential">The new passkey.</param>
    /// <returns>True on success, false if the credential identifier already exists (for any actor).</returns>
    ValueTask<bool> CreateAsync( IActivityMonitor monitor, PasskeyCredential credential );
}
