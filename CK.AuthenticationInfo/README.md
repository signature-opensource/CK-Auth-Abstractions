# CK.AuthenticationInfo

Who is logged in, how strongly, and on whose behalf, as two sealed immutable classes in the `CK.Core`
namespace. This package replaces [CK.Auth.Abstractions](../CK.Auth.Abstractions/README.md) for the
Cris based authentication: there are no interfaces, no type system and no serialization.

It coexists with CK.Auth.Abstractions: the types have the same names but live in another namespace.
An application uses one or the other, never both.

## The level belongs to the actor

[`ActorInfo`](ActorInfo.cs) is an actor identifier, a name, its authentication schemes and its
[`AuthLevel`](AuthLevel.cs). The level is given when the actor is built (or with `WithLevel`), but two
invariants always hold:

- the anonymous (`ActorId` is 0) is always `None`, and only the anonymous can be `None`;
- an actor with the `"Weak"` scheme (`ActorInfo.WeakScheme`, matched case-insensitively) is at most `Unsafe`.

## The session clamps, and can only lower

[`AuthenticationInfo`](AuthenticationInfo.cs) holds the actual actor, the actor (different when impersonated),
`Expires`, `CriticalExpires` and the `DeviceId`. Every instance goes through the same normalization:

- `utcNow` must be `Utc`; dates must be `Utc` or `Unspecified` (considered Utc), never `Local`;
- reached dates are dropped, and `CriticalExpires` is clamped to `Expires` (it never extends the session);
- no `Expires`: the levels are at most `Unsafe`; no `CriticalExpires`: at most `Normal`;
- the actor's level is never above the actual actor's level;
- an anonymous actual actor means no dates and no impersonation.

The functional updaters (`CheckExpiration`, `WithExpires`, `WithCriticalExpires`, `WithDeviceId`,
`WithCallerParty`, `Impersonate`, `ClearImpersonation`) return `this` when nothing changes. Since the stored levels are their
own ceilings, **no updater can raise a level**: only a new authentication (`Create`) can.

`Actor` and `ActualActor` are the anonymous below `Normal`: using an unsafe identity means writing
`UnsafeActor` or `UnsafeActualActor` explicitly.

## Impersonation

`Impersonate` requires the actual actor to be at least `Normal`: the anonymous, the weak actors and the
expired sessions cannot start an impersonation. A weak actor can never be an actual actor that impersonates,
not even through `Create`. The impersonated actor's level is at most the actual actor's level, so an
administrator impersonating a weak actor acts at `Unsafe` level.

`Impersonate` is only the mechanism. Whether an impersonation is allowed is decided by the optional
[`IImpersonationService`](IImpersonationService.cs): without it, any impersonation is refused.

## Caller party

`CallerPartyId` is the party through which the identity entered: the remote party that called us, or our local party
when we received it directly. It is the actor identifier of that party (its PartyActor), 0 when there is none (no party
model installed, or the party's identifier not resolved yet). It is a third role, orthogonal to the actors:
impersonation stays about users, so "Alice impersonating Bob" received from the party Paul keeps Alice, Bob and Paul.

It is never carried by a command nor a token: whoever builds the information at reception sets it with
`WithCallerParty`. `Create` and `CreateRestored` start at 0; the other updaters keep it (changing who you impersonate
doesn't change who called). `WithCallerParty` doesn't change the levels: when the caller party is not the local one,
the builder caps them with the optional [`ICallerPartyService`](ICallerPartyService.cs). Without it, any caller party
other than the local one is refused.

## Login contracts

The authentication center logs actors in through these transport neutral contracts (no HTTP, no SQL),
implemented by the back-end packages (the database, the weak actors, etc.):

- [`ILoginProvider`](Login/ILoginProvider.cs) (multiple): handles one or more provider names. A scheme is a provider
  name optionally followed by a dot and a suffix (`"Oidc.Azure"` is handled by `"Oidc"`, see
  [`LoginScheme`](Login/LoginScheme.cs)). This package knows no scheme in particular: each one is defined by the
  packages that implement it (the `"Basic"` provider and its payload by CK.DB.User.UserPassword for instance). The
  payload is provider dependent and never leaves the process that handles the login.
- [`ActorLoginResult`](Login/ActorLoginResult.cs): a successful (non anonymous) `Actor` at the highest level it can reach,
  or a positive failure code with its reason, possibly flagged as an unregistered user.
- [`ILoginValidator`](Login/ILoginValidator.cs) (optional, multiple): when present, a login is first checked
  (`actualLogin` is false), then validated, then actually done.
- [`IAutoCreateAccountService`](Login/IAutoCreateAccountService.cs) and
  [`IAutoBindingAccountService`](Login/IAutoBindingAccountService.cs) (optional): called for an unregistered user,
  respectively when nobody is logged in and when an actor is logged in.
- [`LoginContext`](Login/LoginContext.cs): the scheme, the payload, the initial authentication and free user data
  (an invitation token for instance). The first error set cancels the login.

## Identity restoration

A command that entered the System has been validated there: its `ActorId` (and `ActualActorId`) can be
trusted as long as it travels on the System's trusted paths (in-process executor, delayed command store,
channels between parties of the same System). Its execution can then be delayed or moved far from its
origin, and the `AuthenticationInfo` ambient service is restored from these identifiers so that handlers
and services use "the current authentication" transparently. A command that enters another System must be
validated again there.

[`IActorInfoProvider`](IActorInfoProvider.cs) reads the actors for this restoration. It returns the actor at the
highest level it can reach (the session clamps it) or null when the actor doesn't exist or cannot be used
anymore: a restoration that fails refuses the command, it never runs it as the anonymous.

`AuthenticationInfo.CreateRestored( actualActor, actor, level, deviceId, utcNow )` builds the restored session.
The origin dates are irrelevant (a delayed command would find them expired): the dates cover the execution,
`RestoredValidity` (one hour) after `utcNow`, so that the actors are exactly at the `level` required by the command
(`Unsafe`: no dates, `Normal`: `Expires`, `Critical`: both). A restored authentication must never be used to
issue an authentication token.

## Ambient service

`AuthenticationInfo` is an `IAmbientAutoService`. Its default value, `AuthenticationInfo.None`, is provided by
[`AuthenticationInfoDefaultProvider`](AuthenticationInfoDefaultProvider.cs).

## No proof inside

An `AuthenticationInfo` is a description, not a credential: anybody can build one. A trusted instance is the one
obtained by verifying an authentication token, and the token format (with its own version) belongs to the
component that signs it.
