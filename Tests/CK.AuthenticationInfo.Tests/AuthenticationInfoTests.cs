using NUnit.Framework;
using Shouldly;
using System;

namespace CK.Core.Tests;

[TestFixture]
public class AuthenticationInfoTests
{
    static readonly DateTime _time0 = new DateTime( 2000, 1, 1, 14, 35, 59, DateTimeKind.Utc );
    static readonly DateTime _time1 = _time0.AddHours( 1 );
    static readonly DateTime _time2 = _time0.AddHours( 2 );
    static readonly DateTime _time3 = _time0.AddHours( 3 );

    static readonly ActorInfo _albert = new ActorInfo( 3712, "Albert", AuthLevel.Critical );
    static readonly ActorInfo _robert = new ActorInfo( 12, "Robert", AuthLevel.Critical );
    static readonly ActorInfo _operator = new ActorInfo( 42, "Operator", AuthLevel.Unsafe, [new SchemeInfo( ActorInfo.WeakScheme, _time0 )] );

    [Test]
    public void None_is_the_anonymous_without_device()
    {
        var none = AuthenticationInfo.None;
        none.UnsafeActor.ShouldBeSameAs( ActorInfo.Anonymous );
        none.UnsafeActualActor.ShouldBeSameAs( ActorInfo.Anonymous );
        none.Expires.ShouldBeNull();
        none.CriticalExpires.ShouldBeNull();
        none.DeviceId.ShouldBe( "" );
        none.IsImpersonated.ShouldBeFalse();
        new AuthenticationInfoDefaultProvider().Default.ShouldBeSameAs( none );
        AuthenticationInfo.Create( ActorInfo.Anonymous, _time2, _time2, null, _time0 ).ShouldBeSameAs( none );
    }

    [Test]
    public void the_anonymous_has_no_dates_but_can_have_a_device()
    {
        var a = AuthenticationInfo.Create( ActorInfo.Anonymous, _time2, _time1, "D", _time0 );
        a.Expires.ShouldBeNull();
        a.CriticalExpires.ShouldBeNull();
        a.DeviceId.ShouldBe( "D" );
        a.UnsafeActor.Level.ShouldBe( AuthLevel.None );
    }

    [Test]
    public void utcNow_must_be_Utc_everywhere()
    {
        var a = AuthenticationInfo.Create( _albert, _time2, null, null, _time0 );
        var local = DateTime.Now;
        var unspecified = DateTime.SpecifyKind( _time0, DateTimeKind.Unspecified );
        Util.Invokable( () => AuthenticationInfo.Create( _albert, _time2, null, null, local ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => AuthenticationInfo.Create( _albert, _time2, null, null, unspecified ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => a.CheckExpiration( local ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => a.WithExpires( _time3, local ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => a.WithCriticalExpires( _time1, local ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => a.WithDeviceId( "D", local ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => a.Impersonate( _robert, local ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => a.ClearImpersonation( local ) ).ShouldThrow<ArgumentException>();
    }

    [Test]
    public void dates_must_not_be_Local_and_Unspecified_is_Utc()
    {
        Util.Invokable( () => AuthenticationInfo.Create( _albert, DateTime.Now.AddDays( 1 ), null, null, _time0 ) ).ShouldThrow<ArgumentException>();
        var a = AuthenticationInfo.Create( _albert, DateTime.SpecifyKind( _time2, DateTimeKind.Unspecified ), null, null, _time0 );
        a.Expires.ShouldBe( _time2 );
        a.Expires!.Value.Kind.ShouldBe( DateTimeKind.Utc );
    }

    [Test]
    public void levels_are_clamped_by_the_dates()
    {
        var unsafeInfo = AuthenticationInfo.Create( _albert, null, null, null, _time0 );
        unsafeInfo.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
        unsafeInfo.Actor.ShouldBeSameAs( ActorInfo.Anonymous );
        unsafeInfo.ActualActor.ShouldBeSameAs( ActorInfo.Anonymous );
        unsafeInfo.UnsafeActor.ActorId.ShouldBe( 3712 );

        var normal = AuthenticationInfo.Create( _albert, _time2, null, null, _time0 );
        normal.Actor.Level.ShouldBe( AuthLevel.Normal );
        normal.Actor.ActorId.ShouldBe( 3712 );

        var critical = AuthenticationInfo.Create( _albert, _time2, _time1, null, _time0 );
        critical.Actor.Level.ShouldBe( AuthLevel.Critical );

        var expired = AuthenticationInfo.Create( _albert, _time0, _time0, null, _time1 );
        expired.Expires.ShouldBeNull();
        expired.CriticalExpires.ShouldBeNull();
        expired.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
    }

    [Test]
    public void the_given_level_is_a_ceiling()
    {
        var normalAlbert = _albert.WithLevel( AuthLevel.Normal );
        var a = AuthenticationInfo.Create( normalAlbert, _time2, _time1, null, _time0 );
        a.Actor.Level.ShouldBe( AuthLevel.Normal );
        a.CriticalExpires.ShouldBe( _time1 );
    }

    [Test]
    public void CriticalExpires_is_clamped_and_never_extends_the_session()
    {
        var a = AuthenticationInfo.Create( _albert, _time1, _time3, null, _time0 );
        a.Expires.ShouldBe( _time1 );
        a.CriticalExpires.ShouldBe( _time1 );

        var b = AuthenticationInfo.Create( _albert, _time2, null, null, _time0 ).WithCriticalExpires( _time3, _time0 );
        b.Expires.ShouldBe( _time2 );
        b.CriticalExpires.ShouldBe( _time2 );
    }

    [Test]
    public void CheckExpiration_only_lowers_the_levels()
    {
        var a = AuthenticationInfo.Create( _albert, _time2, _time1, null, _time0 );
        a.UnsafeActor.Level.ShouldBe( AuthLevel.Critical );
        a.CheckExpiration( _time0 ).ShouldBeSameAs( a );

        var b = a.CheckExpiration( _time1 );
        b.UnsafeActor.Level.ShouldBe( AuthLevel.Normal );
        b.CriticalExpires.ShouldBeNull();
        b.Expires.ShouldBe( _time2 );

        var c = b.CheckExpiration( _time2 );
        c.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
        c.Expires.ShouldBeNull();
        c.Actor.ShouldBeSameAs( ActorInfo.Anonymous );
        c.UnsafeActor.ActorId.ShouldBe( 3712 );
    }

    [Test]
    public void no_updater_can_raise_a_level()
    {
        var expired = AuthenticationInfo.Create( _albert, null, null, null, _time0 );
        expired.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );

        var d = expired.WithExpires( _time3, _time0 ).WithCriticalExpires( _time2, _time0 );
        d.Expires.ShouldBe( _time3 );
        d.CriticalExpires.ShouldBe( _time2 );
        d.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
        d.Actor.ShouldBeSameAs( ActorInfo.Anonymous );

        var normal = AuthenticationInfo.Create( _albert, _time2, null, null, _time0 );
        normal.WithCriticalExpires( _time1, _time0 ).UnsafeActor.Level.ShouldBe( AuthLevel.Normal );

        // Only a new authentication raises the level.
        AuthenticationInfo.Create( _albert, _time2, _time1, null, _time0 ).UnsafeActor.Level.ShouldBe( AuthLevel.Critical );
    }

    [Test]
    public void updaters_return_the_same_instance_when_nothing_changes()
    {
        var a = AuthenticationInfo.Create( _albert, _time2, _time1, "D", _time0 );
        a.WithExpires( _time2, _time0 ).ShouldBeSameAs( a );
        a.WithCriticalExpires( _time1, _time0 ).ShouldBeSameAs( a );
        a.WithDeviceId( "D", _time0 ).ShouldBeSameAs( a );
        a.ClearImpersonation( _time0 ).ShouldBeSameAs( a );
        a.Impersonate( _albert, _time0 ).ShouldBeSameAs( a );

        var other = a.WithDeviceId( "E", _time0 );
        other.ShouldNotBeSameAs( a );
        other.DeviceId.ShouldBe( "E" );
        a.WithDeviceId( null, _time0 ).DeviceId.ShouldBe( "" );
    }

    [Test]
    public void impersonated_user_level_is_at_most_the_actual_user_level()
    {
        var a = AuthenticationInfo.Create( _albert, _time2, null, null, _time0 ).Impersonate( _robert, _time0 );
        a.IsImpersonated.ShouldBeTrue();
        a.Actor.ActorId.ShouldBe( 12 );
        a.Actor.Level.ShouldBe( AuthLevel.Normal );
        a.ActualActor.ActorId.ShouldBe( 3712 );
        a.ActualActor.Level.ShouldBe( AuthLevel.Normal );

        var cleared = a.ClearImpersonation( _time0 );
        cleared.IsImpersonated.ShouldBeFalse();
        cleared.Actor.ActorId.ShouldBe( 3712 );
    }

    [Test]
    public void impersonating_a_weak_actor_keeps_it_Unsafe()
    {
        var a = AuthenticationInfo.Create( _albert, _time2, _time1, null, _time0 ).Impersonate( _operator, _time0 );
        a.IsImpersonated.ShouldBeTrue();
        a.ActualActor.Level.ShouldBe( AuthLevel.Critical );
        a.UnsafeActor.ActorId.ShouldBe( 42 );
        a.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
        a.Actor.ShouldBeSameAs( ActorInfo.Anonymous );
        // The administrator can still end the impersonation.
        a.ClearImpersonation( _time0 ).Actor.ActorId.ShouldBe( 3712 );
    }

    [Test]
    public void impersonating_the_anonymous_gives_no_level()
    {
        var a = AuthenticationInfo.Create( _albert, _time2, null, null, _time0 ).Impersonate( ActorInfo.Anonymous, _time0 );
        a.IsImpersonated.ShouldBeTrue();
        a.UnsafeActor.Level.ShouldBe( AuthLevel.None );
        a.Actor.ShouldBeSameAs( ActorInfo.Anonymous );
        a.ActualActor.Level.ShouldBe( AuthLevel.Normal );
    }

    [Test]
    public void impersonation_requires_an_actual_user_at_Normal_level()
    {
        Util.Invokable( () => AuthenticationInfo.None.Impersonate( _robert, _time0 ) ).ShouldThrow<InvalidOperationException>();
        Util.Invokable( () => AuthenticationInfo.Create( _albert, null, null, null, _time0 ).Impersonate( _robert, _time0 ) )
            .ShouldThrow<InvalidOperationException>();
        Util.Invokable( () => AuthenticationInfo.Create( _operator, _time2, null, null, _time0 ).Impersonate( _robert, _time0 ) )
            .ShouldThrow<InvalidOperationException>();
        // The session expires between the creation and the impersonation.
        var a = AuthenticationInfo.Create( _albert, _time1, null, null, _time0 );
        Util.Invokable( () => a.Impersonate( _robert, _time2 ) ).ShouldThrow<InvalidOperationException>();
    }

    [Test]
    public void a_weak_actor_can_never_impersonate()
    {
        Util.Invokable( () => AuthenticationInfo.Create( _operator, _robert, _time2, null, null, _time0 ) ).ShouldThrow<ArgumentException>();
        AuthenticationInfo.Create( _operator, _operator, _time2, null, null, _time0 ).IsImpersonated.ShouldBeFalse();
    }

    [Test]
    public void an_expired_impersonation_is_kept_with_lowered_levels()
    {
        var a = AuthenticationInfo.Create( _albert, _time1, null, null, _time0 ).Impersonate( _robert, _time0 );
        var expired = a.CheckExpiration( _time2 );
        expired.IsImpersonated.ShouldBeTrue();
        expired.UnsafeActualActor.Level.ShouldBe( AuthLevel.Unsafe );
        expired.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
        expired.Actor.ShouldBeSameAs( ActorInfo.Anonymous );
        expired.ActualActor.ShouldBeSameAs( ActorInfo.Anonymous );
    }

    [TestCase( AuthLevel.Unsafe )]
    [TestCase( AuthLevel.Normal )]
    [TestCase( AuthLevel.Critical )]
    public void restored_authentication_is_at_the_required_level_for_RestoredValidity( AuthLevel level )
    {
        var r = AuthenticationInfo.CreateRestored( _albert, _albert, level, "D", _time0 );
        r.UnsafeActor.ActorId.ShouldBe( 3712 );
        r.UnsafeActor.Level.ShouldBe( level );
        r.UnsafeActualActor.Level.ShouldBe( level );
        r.DeviceId.ShouldBe( "D" );
        r.IsImpersonated.ShouldBeFalse();
        var end = _time0 + AuthenticationInfo.RestoredValidity;
        r.Expires.ShouldBe( level >= AuthLevel.Normal ? end : null );
        r.CriticalExpires.ShouldBe( level == AuthLevel.Critical ? end : null );
        // After the validity, the levels drop to Unsafe.
        r.CheckExpiration( end ).UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
    }

    [Test]
    public void RestoredValidity_is_one_hour()
    {
        AuthenticationInfo.RestoredValidity.ShouldBe( TimeSpan.FromHours( 1 ) );
    }

    [Test]
    public void restored_weak_actor_stays_Unsafe()
    {
        var r = AuthenticationInfo.CreateRestored( _operator, _operator, AuthLevel.Critical, null, _time0 );
        r.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );
        r.Actor.ShouldBeSameAs( ActorInfo.Anonymous );
    }

    [Test]
    public void restored_impersonation_is_clamped_by_the_level()
    {
        var r = AuthenticationInfo.CreateRestored( _albert, _operator, AuthLevel.Normal, null, _time0 );
        r.IsImpersonated.ShouldBeTrue();
        r.ActualActor.Level.ShouldBe( AuthLevel.Normal );
        r.UnsafeActor.ActorId.ShouldBe( 42 );
        r.UnsafeActor.Level.ShouldBe( AuthLevel.Unsafe );

        var r2 = AuthenticationInfo.CreateRestored( _albert, _robert, AuthLevel.Normal, null, _time0 );
        r2.Actor.ActorId.ShouldBe( 12 );
        r2.Actor.Level.ShouldBe( AuthLevel.Normal );
        r2.ActualActor.ActorId.ShouldBe( 3712 );
    }

    [Test]
    public void restoration_checks_its_arguments()
    {
        Util.Invokable( () => AuthenticationInfo.CreateRestored( _albert, _albert, AuthLevel.None, null, _time0 ) )
            .ShouldThrow<ArgumentException>();
        Util.Invokable( () => AuthenticationInfo.CreateRestored( _albert, _albert, (AuthLevel)7, null, _time0 ) )
            .ShouldThrow<ArgumentOutOfRangeException>();
        Util.Invokable( () => AuthenticationInfo.CreateRestored( _albert, _albert, AuthLevel.Normal, null, DateTime.Now ) )
            .ShouldThrow<ArgumentException>();
        // A weak actor can never impersonate, even through a restoration.
        Util.Invokable( () => AuthenticationInfo.CreateRestored( _operator, _robert, AuthLevel.Unsafe, null, _time0 ) )
            .ShouldThrow<ArgumentException>();
        // The anonymous is restored at None level.
        AuthenticationInfo.CreateRestored( ActorInfo.Anonymous, ActorInfo.Anonymous, AuthLevel.None, null, _time0 )
            .ShouldBeSameAs( AuthenticationInfo.None );
        AuthenticationInfo.CreateRestored( ActorInfo.Anonymous, ActorInfo.Anonymous, AuthLevel.Critical, null, _time0 )
            .ShouldBeSameAs( AuthenticationInfo.None );
    }

    [Test]
    public void same_user_identifier_means_no_impersonation()
    {
        var otherAlbert = new ActorInfo( 3712, "Albert (other instance)", AuthLevel.Critical );
        var a = AuthenticationInfo.Create( _albert, otherAlbert, _time2, null, null, _time0 );
        a.IsImpersonated.ShouldBeFalse();
        a.UnsafeActor.ShouldBeSameAs( a.UnsafeActualActor );
    }
}
