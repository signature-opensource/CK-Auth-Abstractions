The authentication contracts of the CK stack: who is logged in, how strongly, and on whose behalf.

`IUserInfo` and `IAuthenticationInfo` model the user, the impersonation and the expiration dates, and
the four `AuthLevel` values - None, Unsafe, Normal, Critical - are derived from those dates rather than
set. At Unsafe level the `User` property is the anonymous, so reading the real identity means writing
`UnsafeUser` explicitly.

`StdUserInfo`, `StdAuthenticationInfo` and the extensible `StdAuthenticationTypeSystem` provide the
standard immutable implementation, with claims, JSON and binary round trips.
