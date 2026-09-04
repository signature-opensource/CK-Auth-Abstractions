# CK.Auth.Abstractions

Who is logged in, how strongly, and on whose behalf. This package defines the contracts and ships a
standard, extensible implementation of them - it authenticates nobody, it only describes the result.

## Four levels, and you cannot set them

[`AuthLevel`](AuthLevel.cs) grades how much the authentication can be trusted:

| Level | Meaning |
|-------|---------|
| `None` | no authentication - the user is necessarily the anonymous |
| `Unsafe` | *"issued from a long lived cookie or other not very secure means"* |
| `Normal` | ordinary authentication |
| `Critical` | *"MUST be short term and rely on strong authentication mechanisms (re-authentication, two-factor authentication, etc.)"* |

`Level` is a read-only property, and no constructor takes one. It is **derived from the expiration
dates**, in the [`StdAuthenticationInfo`](StdAuthenticationInfo.cs) constructor:

- actual user id is `0` → `None` (and expirations are wiped);
- no `Expires`, or an `Expires` already in the past → `Unsafe`;
- `Expires` still valid, no `CriticalExpires` → `Normal`;
- both still valid → `Critical`.

So a level cannot be claimed: to be `Critical` you must hold a critical expiration date that has not
passed. `CheckExpiration( utcNow )` re-runs the same evaluation and returns `this` when nothing moved,
so calling it on every request costs an allocation only when the level actually drops. It does throw an
`ArgumentException` if `utcNow.Kind` is not `Utc`.

Local times never enter the model at all: `Expires`, `CriticalExpires` and `UserSchemeInfo.LastUsed` all
refuse a `DateTimeKind.Local` date and promote `Unspecified` to `Utc`.

## `User` versus `UnsafeUser`

[`IAuthenticationInfo`](IAuthenticationInfo.cs) exposes the user twice, and this is the point of the
whole interface:

```csharp
public IUserInfo User => _level != AuthLevel.Unsafe ? _user : _typeSystem.UserInfo.Anonymous;
public IUserInfo UnsafeUser => _user;
```

At `Unsafe` level, `User` **is the anonymous**. The identity is still there, but reaching it means
writing `UnsafeUser`, and the word stays in the call site forever. The interface comment says it
plainly: *"This interface has been designed so that using `AuthLevel.Unsafe` requires an explicit use
of `UnsafeUser`."*

The same pair exists for impersonation: `ActualUser` / `UnsafeActualUser`.

The user behind them is plain: `IUserInfo.UserId` has *"no constraints on this value (except that 0 is
the Anonymous identifier by design). It may be a negative value."*, and `UserName` is empty if and only
if the id is 0. A fifth accessor, [`IActualUser`](IActualUser.cs), is declared but unused -
*"(This is not implemented yet.)"*

## Impersonation

`User` is who the system acts as, `ActualUser` is who actually authenticated, and `IsImpersonated` is
simply `_user != _actualUser`. Two rules:

- `Impersonate` on an anonymous actual user **throws `InvalidOperationException`** - you cannot borrow
  an identity you do not have. The contract says so as well: *"Calling this on the anonymous MUST throw
  an `InvalidOperationException`."*
- `UnsafeActualUser` survives expiration on purpose: *"This enables the impersonation to be effective
  when the authentication expired so that an impersonated administrator/tester can continue to
  challenge a system in this case."*

Everything is immutable: `Impersonate`, `ClearImpersonation`, `SetExpires`, `SetCriticalExpires` and
`SetDeviceId` build a new instance through the `protected virtual Clone` that specializations override
to carry their extra fields. When the value they are given is the one already held they do **not**
return `this` - they return `CheckExpiration( utcNow )`, which may itself hand back a level-downgraded
instance. "Nothing changed" and "same object" are not the same thing here.

Note one asymmetry between the two ways a critical date can exceed the normal one. The constructor
clamps down - `if( criticalExpires.Value > expires.Value ) criticalExpires = expires` - while
`SetCriticalExpires` pushes up, raising `Expires` to the new critical date when it is lower or absent.

## `DeviceId` is not a credential

`IAuthenticationInfo.DeviceId` can be empty, and its comment is a warning rather than a description:

> A device identifier is not trustable in any way. Any information that may be sent to a user via a
> device should actually be sent to a couple (DeviceId, UserId) and the UserId should be eventually
> challenged to avoid any kind of phishing.

## The type system

C# static methods cannot be overridden, so the builders and converters that would naturally be static
are gathered into interfaces instead. [`IUserInfoType`](IUserInfoType.cs) and
[`IAuthenticationInfoType`](IAuthenticationInfoType.cs) each *"defines `"non instance"` functionalities
(that would have been non extensible static methods) like builders and converters"*, and
[`IAuthenticationTypeSystem`](IAuthenticationTypeSystem.cs) unifies the two.

[`StdAuthenticationTypeSystem`](StdAuthenticationTypeSystem.cs) implements all three at once, with a
`protected virtual` for every step - `CreateAnonymous`, `UserInfoToJObject`, `UserInfoToClaims`,
`AuthenticationInfoToClaimsIdentity`, `WriteUserInfoRemainder`... A derived type system that adds a
field to `IUserInfo` overrides the pairs it needs, and the three serialization formats follow.

Three formats, each a round trip, and the pairs are not named the same on both interfaces:

| | `IUserInfoType` | `IAuthenticationInfoType` |
|--|--|--|
| claims | `ToClaims` / `FromClaims` | `ToClaimsIdentity` / `FromClaimsIdentity` |
| JSON | `ToJObject` / `FromJObject` | `ToJObject` / `FromJObject` |
| binary | `Write` / `Read` | `Write` / `Read` |

The JSON and binary readers carry an explicit contract: malformed input must raise
`InvalidDataException` rather than return a half-built value, and `StdAuthenticationTypeSystem` wraps
them to honour it. **The claims readers do not.** They parse with bare `int.Parse`, `long.Parse` and
`JArray.Parse`, so a malformed claim surfaces as a `FormatException` or a `JsonReaderException`. Guard
accordingly when the claims come from outside.

### Claim types

| Constant | Claim type |
|----------|-----------|
| `UserNameKeyType` | `name` |
| `UserIdKeyType` | `id` |
| `SchemesKeyType` | `schemes` |
| `AuthLevelKeyType` | `acr` |
| `ExpirationKeyType` | `exp` |
| `CriticalExpirationKeyType` | `cexp` |
| `DeviceIdKeyType` | `device` |
| `UserKeyType` / `ActualUserKeyType` | `user` / `actualUser` (JSON only) |

`ToClaimsIdentity( info, userInfoOnly )` produces one of two shapes, told apart by the
`AuthenticationType`:

- **`CKA-S`** (`ClaimAuthenticationTypeSimple`) - `userInfoOnly: true`. A flat identity with the safe
  user claims only; impersonation is dropped and no `acr` is written. Expirations and device id sit on
  that identity.
- **`CKA`** (`ClaimAuthenticationType`, the default) - `userInfoOnly: false`. The unsafe user is the
  primary identity and the `acr` level is written. `ClaimsIdentity.Actor` appears **only when the
  information is impersonated**; it then carries the actual user, and the level, expirations and device
  id move onto it. Without impersonation there is no Actor and all of those stay on the primary
  identity.

That last point is worth stating because the XML comment on `IAuthenticationInfoType.ToClaimsIdentity`
does not: it says *"Expirations and device identifier appear on the subordinated Actor identity"* flatly,
while the code writes them to a `propertyBearer` that is the primary identity unless `IsImpersonated`.
`FromClaimsIdentity` reads it back the same way - `id.Actor == null` means "read the claims off the
identity itself". The comment is stale; the round trip is correct.

`FromClaimsIdentity` returns **null** - it does not throw - for anything whose `AuthenticationType` is
neither `CKA` nor `CKA-S`. And the `acr` claim is **ignored on the way back in**: the level is recomputed
from the dates, exactly as above.

## A `ClaimsIdentity` that says no

[`ClaimsIdentityAnonymousNotAuthenticated`](ClaimsIdentityAnonymousNotAuthenticated.cs) exists because
`ClaimsIdentity.IsAuthenticated` is true as soon as `AuthenticationType` is non-empty - a user with no
name passes. Since that property is what `[Authorize]` reads, this specialization adds one condition:
`Name` must not be null or empty either. Every identity this package produces is of that type.

## Automatic DI

- [`IAuthenticationInfo`](IAuthenticationInfo.cs) is an `IAmbientAutoService`: it flows through the
  container as ambient data, with no parameter to thread manually.
- [`IAuthenticationTypeSystem`](IAuthenticationTypeSystem.cs) is the
  `IAmbientServiceDefaultProvider<IAuthenticationInfo>`, so an unauthenticated context still resolves
  to a valid value - `AuthenticationInfo.None`, *"semantically the same as a null reference"*.
- [`IUserInfoProvider`](IUserInfoProvider.cs) is an `ISingletonAutoService` that reads or synthesizes an
  `IUserInfo` from an identifier.

## Requires.

- `CK.Abstractions` for the DI markers, `CK.ActivityMonitor.SimpleSender`, and `Newtonsoft.Json` for the
  `JObject` form.
