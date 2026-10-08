Minimal immutable authentication model of the CK stack: who is logged in, how strongly, and on whose behalf.

`ActorInfo` carries its own `AuthLevel` (the anonymous is always `None`, a weak actor is at most `Unsafe`).
`AuthenticationInfo` is the session: actual actor, impersonated actor, expiration dates and device identifier.
It clamps the levels with the dates, and its functional updaters can only lower them.

No interfaces, no type system, no serialization: building an `AuthenticationInfo` authenticates nobody.
