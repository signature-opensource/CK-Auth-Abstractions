# CK.AuthenticationInfo.Passkey

The passkey (WebAuthn) contracts between the two parties that must agree on them, and nobody else:

- the **authentication center**, that runs the WebAuthn protocol and verifies the passkeys (CK.AppIdentity.Cris.AuthCenter.Passkey);
- the **storage**, that keeps them (CK.DB.User.UserPasskey for instance).

They never reference each other. This package exists only when passkeys are wanted: the generic
[CK.AuthenticationInfo](../CK.AuthenticationInfo/README.md) knows nothing about them ("Package First": one package per
login technology).

## The contracts

- [`IPasskeyStore`](IPasskeyStore.cs): the storage of the passkeys. It gives the user handle of an actor (random, created
  on first use, never derived from the actor identifier), finds a passkey by its credential identifier, lists the passkeys of
  an actor and stores a new one. The authentication center enables passkeys only when a store is registered.
- [`PasskeyCredential`](PasskeyCredential.cs): an immutable registered passkey: credential identifier, owner, user handle,
  COSE public key, signature counter, backup flags, declared model (AAGUID), transports and name. Nothing in it is secret.
- [`PasskeyLoginPayload`](PasskeyLoginPayload.cs): the payload of the `"Passkey"` scheme, for the `"Passkey"`
  `ILoginProvider` (implemented by the storage).

## The payload carries what was verified

`PasskeyLoginPayload` holds the credential identifier, the signature counter and the backup state of an assertion
**that the authentication center has verified**. It holds no proof: it is trusted because only the authentication center
builds it, after checking the signature, and a payload never leaves the process that handles the login. It is not the
login command: the command carries the raw browser response, unverified, and a provider must never see a client value where
it expects a verified one.

On an actual login, the provider records the new signature counter, the backup state and the last use. The store does not
update them.

## Not attested

The authentication center asks for no attestation: the AAGUID (the authenticator model) is the one the authenticator
declares. It is kept for display (a friendly name), never for a decision.
