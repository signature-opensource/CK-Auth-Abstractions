namespace CK.Core;

/// <summary>
/// Immutable result of a login attempt by a <see cref="ILoginProvider"/>: either a successful <see cref="User"/>
/// or a failure with a positive <see cref="FailureCode"/> and a <see cref="FailureReason"/>.
/// </summary>
public sealed class UserLoginResult
{
    UserLoginResult( UserInfo? user, int failureCode, string? failureReason, bool isUnregisteredUser )
    {
        User = user;
        FailureCode = failureCode;
        FailureReason = failureReason;
        IsUnregisteredUser = isUnregisteredUser;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="user">
    /// The logged in user. Must not be the anonymous. Its <see cref="UserInfo.Level"/> is the highest level
    /// it can reach: the session clamps it.
    /// </param>
    /// <returns>The successful result.</returns>
    public static UserLoginResult Success( UserInfo user )
    {
        Throw.CheckNotNullArgument( user );
        Throw.CheckArgument( "A successful login cannot be the anonymous.", !user.IsAnonymous );
        return new UserLoginResult( user, 0, null, false );
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="failureCode">
    /// The failure code. Must be positive. The CK.DB.Auth implementation uses its <c>KnownLoginFailureCode</c>.
    /// </param>
    /// <param name="failureReason">The failure reason. Must not be null, empty or white space.</param>
    /// <param name="isUnregisteredUser">
    /// Whether the login failed because the user is not registered for the provider: this may be corrected by
    /// registering the user (see <see cref="IAutoCreateAccountService"/> and <see cref="IAutoBindingAccountService"/>).
    /// </param>
    /// <returns>The failed result.</returns>
    public static UserLoginResult Failure( int failureCode, string failureReason, bool isUnregisteredUser = false )
    {
        Throw.CheckArgument( "The failure code must be positive.", failureCode > 0 );
        Throw.CheckNotNullOrWhiteSpaceArgument( failureReason );
        return new UserLoginResult( null, failureCode, failureReason, isUnregisteredUser );
    }

    /// <summary>
    /// Gets the logged in user. Not null if and only if <see cref="IsSuccess"/> is true.
    /// </summary>
    public UserInfo? User { get; }

    /// <summary>
    /// Gets whether the login succeeded.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen( true, nameof( User ) )]
    public bool IsSuccess => User != null;

    /// <summary>
    /// Gets the failure code: positive on failure, 0 on success.
    /// </summary>
    public int FailureCode { get; }

    /// <summary>
    /// Gets the failure reason. Null on success.
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// Gets whether the failure may be corrected by registering the user for the provider.
    /// Always false on success.
    /// </summary>
    public bool IsUnregisteredUser { get; }

    /// <summary>
    /// Overridden to return a readable description (for debugging and logs).
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => IsSuccess
                                            ? $"Success: {User}"
                                            : $"Failure {FailureCode}: {FailureReason}{(IsUnregisteredUser ? " (unregistered user)" : "")}";
}
