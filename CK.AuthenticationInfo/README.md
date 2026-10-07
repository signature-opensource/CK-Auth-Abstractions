# CK.AuthenticationInfo

Who is logged in, how strongly, and on whose behalf, as two sealed immutable classes in the `CK.Core`
namespace. This package replaces [CK.Auth.Abstractions](../CK.Auth.Abstractions/README.md) for the
Cris based authentication: there are no interfaces, no type system and no serialization.

It coexists with CK.Auth.Abstractions: the types have the same names but live in another namespace.
An application uses one or the other, never both.

## The level belongs to the user

[`UserInfo`](UserInfo.cs) is a user identifier, a name, its authentication schemes and its
[`AuthLevel`](AuthLevel.cs). The level is given when the user is built (or with `WithLevel`), but two
invariants always hold:

- the anonymous (`UserId` is 0) is always `None`, and only the anonymous can be `None`;
- a user with the `"Weak"` scheme (`UserInfo.WeakScheme`, matched case-insensitively) is at most `Unsafe`.

## The session clamps, and can only lower

[`AuthenticationInfo`](AuthenticationInfo.cs) holds the actual user, the user (different when impersonated),
`Expires`, `CriticalExpires` and the `DeviceId`. Every instance goes through the same normalization:

- `utcNow` must be `Utc`; dates must be `Utc` or `Unspecified` (considered Utc), never `Local`;
- reached dates are dropped, and `CriticalExpires` is clamped to `Expires` (it never extends the session);
- no `Expires`: the levels are at most `Unsafe`; no `CriticalExpires`: at most `Normal`;
- the user's level is never above the actual user's level;
- an anonymous actual user means no dates and no impersonation.

The functional updaters (`CheckExpiration`, `WithExpires`, `WithCriticalExpires`, `WithDeviceId`,
`Impersonate`, `ClearImpersonation`) return `this` when nothing changes. Since the stored levels are their
own ceilings, **no updater can raise a level**: only a new authentication (`Create`) can.

`User` and `ActualUser` are the anonymous below `Normal`: using an unsafe identity means writing
`UnsafeUser` or `UnsafeActualUser` explicitly.

## Impersonation

`Impersonate` requires the actual user to be at least `Normal`: the anonymous, the weak actors and the
expired sessions cannot start an impersonation. A weak actor can never be an actual user that impersonates,
not even through `Create`. The impersonated user's level is at most the actual user's level, so an
administrator impersonating a weak actor acts at `Unsafe` level.

`Impersonate` is only the mechanism. Whether an impersonation is allowed is decided by the optional
[`IImpersonationService`](IImpersonationService.cs): without it, any impersonation is refused.

## Login contracts

The authentication center logs users in through these transport neutral contracts (no HTTP, no SQL),
implemented by the back-end packages (the database, the weak actors, etc.):

- [`ILoginProvider`](Login/ILoginProvider.cs) (multiple): handles one or more provider names. A scheme is a provider
  name optionally followed by a dot and a suffix (`"Oidc.Azure"` is handled by `"Oidc"`, see
  [`LoginScheme`](Login/LoginScheme.cs)). The basic login is the `"Basic"` provider with a
  [`BasicLoginPayload`](Login/BasicLoginPayload.cs), whose `ToString()` never shows the password. The payload is
  provider dependent and never leaves the process that handles the login.
- [`UserLoginResult`](Login/UserLoginResult.cs): a successful (non anonymous) `User` at the highest level it can reach,
  or a positive failure code with its reason, possibly flagged as an unregistered user.
- [`ILoginValidator`](Login/ILoginValidator.cs) (optional, multiple): when present, a login is first checked
  (`actualLogin` is false), then validated, then actually done.
- [`IAutoCreateAccountService`](Login/IAutoCreateAccountService.cs) and
  [`IAutoBindingAccountService`](Login/IAutoBindingAccountService.cs) (optional): called for an unregistered user,
  respectively when nobody is logged in and when a user is logged in.
- [`LoginContext`](Login/LoginContext.cs): the scheme, the payload, the initial authentication and free user data
  (an invitation token for instance). The first error set cancels the login.

## Identity restoration

A command that entered the System has been validated there: its `ActorId` (and `ActualActorId`) can be
trusted as long as it travels on the System's trusted paths (in-process executor, delayed command store,
channels between parties of the same System). Its execution can then be delayed or moved far from its
origin, and the `AuthenticationInfo` ambient service is restored from these identifiers so that handlers
and services use "the current authentication" transparently. A command that enters another System must be
validated again there.

[`IUserInfoProvider`](IUserInfoProvider.cs) reads the users for this restoration. It returns the user at the
highest level it can reach (the session clamps it) or null when the user doesn't exist or cannot be used
anymore: a restoration that fails refuses the command, it never runs it as the anonymous.

`AuthenticationInfo.CreateRestored( actualUser, user, level, deviceId, utcNow )` builds the restored session.
The origin dates are irrelevant (a delayed command would find them expired): the dates cover the execution,
`RestoredValidity` (one hour) after `utcNow`, so that the users are exactly at the `level` required by the command
(`Unsafe`: no dates, `Normal`: `Expires`, `Critical`: both). A restored authentication must never be used to
issue an authentication token.

## Ambient service

`AuthenticationInfo` is an `IAmbientAutoService`. Its default value, `AuthenticationInfo.None`, is provided by
[`AuthenticationInfoDefaultProvider`](AuthenticationInfoDefaultProvider.cs).

## No proof inside

An `AuthenticationInfo` is a description, not a credential: anybody can build one. A trusted instance is the one
obtained by verifying an authentication token, and the token format (with its own version) belongs to the
component that signs it.
