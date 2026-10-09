Passkey (WebAuthn) contracts of the CK stack, between the authentication center that verifies the passkeys and the storage
that keeps them.

- `IPasskeyStore`: user handles, lookup by credential identifier, an actor's passkeys, registration.
- `PasskeyCredential`: an immutable registered passkey (COSE public key, signature counter, backup flags...).
- `PasskeyLoginPayload`: the payload of the "Passkey" scheme, built by the authentication center from a **verified**
  assertion only.

Reference it only when passkeys are wanted: CK.AuthenticationInfo stays generic.
