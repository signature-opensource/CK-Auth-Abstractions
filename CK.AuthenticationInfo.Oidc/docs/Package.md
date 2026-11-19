OpenID Connect contract of the CK stack, between the authentication center that runs the protocol and the storage of the
identities.

- `IOidcLoginPayload`: the payload of the "Oidc" login provider (`SchemeSuffix`, `Sub`, `Claims`), filled by the
  authentication center with the identity it has verified.

Reference it only when OpenID Connect is wanted: CK.AuthenticationInfo stays generic.
