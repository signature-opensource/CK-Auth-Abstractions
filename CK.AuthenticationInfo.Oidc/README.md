# CK.AuthenticationInfo.Oidc

The OpenID Connect contract between the two parties that must agree on it, and nobody else:

- the **authentication center**, that runs the OpenID Connect protocol and validates the identity tokens
  (CK.AppIdentity.AuthCenter.Oidc);
- the **storage** of the identities (CK.DB.User.UserOidc for instance), behind the "Oidc" `ILoginProvider`.

They never reference each other. This package exists only when OpenID Connect is wanted: the generic
[CK.AuthenticationInfo](../CK.AuthenticationInfo/README.md) knows nothing about it ("Package First": one package per
login technology).

## The contract

[`IOidcLoginPayload`](IOidcLoginPayload.cs) is the payload of the "Oidc" provider: the payload that its
`ILoginProvider.CreatePayload( "Oidc.<Suffix>" )` creates implements it. The authentication center checks it when a login
starts (before the user is sent to the identity provider), then fills it with the verified identity: the `SchemeSuffix`,
the `Sub` and the `Claims` (a JSON object, without the protocol claims; empty means "not provided").

The payload holds no proof: it is trusted because only the authentication center fills it, after the validation of the
identity token, and a payload never leaves the process that handles the login.
