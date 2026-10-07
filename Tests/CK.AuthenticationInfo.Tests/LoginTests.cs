using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;

namespace CK.Core.Tests;

[TestFixture]
public class LoginTests
{
    static readonly UserInfo _albert = new UserInfo( 3712, "Albert", AuthLevel.Critical );

    [Test]
    public void UserLoginResult_success_and_failure()
    {
        var s = UserLoginResult.Success( _albert );
        s.IsSuccess.ShouldBeTrue();
        s.User.ShouldBeSameAs( _albert );
        s.FailureCode.ShouldBe( 0 );
        s.FailureReason.ShouldBeNull();
        s.IsUnregisteredUser.ShouldBeFalse();

        var f = UserLoginResult.Failure( 5, "Unregistered user.", isUnregisteredUser: true );
        f.IsSuccess.ShouldBeFalse();
        f.User.ShouldBeNull();
        f.FailureCode.ShouldBe( 5 );
        f.FailureReason.ShouldBe( "Unregistered user." );
        f.IsUnregisteredUser.ShouldBeTrue();
        f.ToString().ShouldBe( "Failure 5: Unregistered user. (unregistered user)" );
    }

    [Test]
    public void UserLoginResult_checks_its_arguments()
    {
        Util.Invokable( () => UserLoginResult.Success( UserInfo.Anonymous ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => UserLoginResult.Failure( 0, "Reason" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => UserLoginResult.Failure( -1, "Reason" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => UserLoginResult.Failure( 1, " " ) ).ShouldThrow<ArgumentException>();
    }

    [Test]
    public void BasicLoginPayload_never_shows_the_password()
    {
        var p = new BasicLoginPayload( "Albert", "secret-password" );
        p.ToString().ShouldBe( "BasicLoginPayload { UserName = Albert, Password = *** }" );
        p.ToString().ShouldNotContain( "secret-password" );
        p.Password.ShouldBe( "secret-password" );
        BasicLoginPayload.Scheme.ShouldBe( "Basic" );
    }

    [TestCase( "Basic", "Basic" )]
    [TestCase( "Oidc.Azure", "Oidc" )]
    [TestCase( "Oidc.Azure.Tenant", "Oidc" )]
    public void LoginScheme_provider_name( string scheme, string provider )
    {
        LoginScheme.GetProviderName( scheme ).ShouldBe( provider );
    }

    [Test]
    public void LoginScheme_checks_its_argument()
    {
        Util.Invokable( () => LoginScheme.GetProviderName( "" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => LoginScheme.GetProviderName( ".Azure" ) ).ShouldThrow<ArgumentException>();
    }

    [Test]
    public void LoginContext_keeps_the_first_error()
    {
        var c = new LoginContext( "Google", new object(), AuthenticationInfo.None );
        c.UserData.ShouldBeEmpty();
        c.HasError.ShouldBeFalse();
        c.SetError( "User.NoAutoRegistration" ).ShouldBeNull();
        c.HasError.ShouldBeTrue();
        c.ErrorId.ShouldBe( "User.NoAutoRegistration" );
        c.ErrorText.ShouldBe( "User.NoAutoRegistration" );
        c.SetError( "Other", "Ignored." );
        c.ErrorId.ShouldBe( "User.NoAutoRegistration" );

        var e = new LoginContext( "Google", new object(), AuthenticationInfo.None, new Dictionary<string, string?> { { "InvitationToken", "T" } } );
        e.UserData["InvitationToken"].ShouldBe( "T" );
        e.SetError( new InvalidOperationException( "Boom." ) );
        e.ErrorId.ShouldBe( "System.InvalidOperationException" );
        e.ErrorText.ShouldBe( "Boom." );
    }
}
