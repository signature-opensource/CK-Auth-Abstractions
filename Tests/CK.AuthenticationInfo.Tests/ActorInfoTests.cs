using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Immutable;

namespace CK.Core.Tests;

[TestFixture]
public class ActorInfoTests
{
    static readonly DateTime _time0 = new DateTime( 2000, 1, 1, 14, 35, 59, DateTimeKind.Utc );

    [Test]
    public void UserName_is_empty_if_and_only_if_UserId_is_0()
    {
        Util.Invokable( () => new ActorInfo( 0, "Name for Anonymous", AuthLevel.None ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new ActorInfo( 3712, "", AuthLevel.Normal ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new ActorInfo( 3712, null, AuthLevel.Normal ) ).ShouldThrow<ArgumentException>();
        new ActorInfo( 0, null, AuthLevel.None ).ActorName.ShouldBe( "" );
        new ActorInfo( -1, "Negative", AuthLevel.Normal ).ActorId.ShouldBe( -1 );
    }

    [Test]
    public void the_anonymous_is_always_at_None_level()
    {
        ActorInfo.Anonymous.Level.ShouldBe( AuthLevel.None );
        ActorInfo.Anonymous.IsAnonymous.ShouldBeTrue();
        ActorInfo.Anonymous.Schemes.IsDefault.ShouldBeFalse();
        ActorInfo.Anonymous.Schemes.ShouldBeEmpty();
        new ActorInfo( 0, null, AuthLevel.Critical ).Level.ShouldBe( AuthLevel.None );
        ActorInfo.Anonymous.WithLevel( AuthLevel.Critical ).ShouldBeSameAs( ActorInfo.Anonymous );
    }

    [Test]
    public void only_the_anonymous_can_be_at_None_level()
    {
        Util.Invokable( () => new ActorInfo( 3712, "Albert", AuthLevel.None ) ).ShouldThrow<ArgumentException>();
        var albert = new ActorInfo( 3712, "Albert", AuthLevel.Normal );
        Util.Invokable( () => albert.WithLevel( AuthLevel.None ) ).ShouldThrow<ArgumentException>();
    }

    [Test]
    public void invalid_levels_are_rejected()
    {
        Util.Invokable( () => new ActorInfo( 3712, "Albert", (AuthLevel)4 ) ).ShouldThrow<ArgumentOutOfRangeException>();
        Util.Invokable( () => new ActorInfo( 3712, "Albert", (AuthLevel)(-1) ) ).ShouldThrow<ArgumentOutOfRangeException>();
    }

    [TestCase( "Weak" )]
    [TestCase( "weak" )]
    [TestCase( "WEAK" )]
    public void a_weak_actor_is_at_most_Unsafe( string schemeName )
    {
        var weak = new ActorInfo( 42, "Operator", AuthLevel.Critical, [new SchemeInfo( schemeName, _time0 )] );
        weak.IsWeak.ShouldBeTrue();
        weak.Level.ShouldBe( AuthLevel.Unsafe );
        weak.WithLevel( AuthLevel.Normal ).ShouldBeSameAs( weak );
    }

    [Test]
    public void WithLevel_returns_the_same_instance_when_nothing_changes()
    {
        var albert = new ActorInfo( 3712, "Albert", AuthLevel.Normal, [new SchemeInfo( "Basic", _time0 )] );
        albert.WithLevel( AuthLevel.Normal ).ShouldBeSameAs( albert );
        var critical = albert.WithLevel( AuthLevel.Critical );
        critical.Level.ShouldBe( AuthLevel.Critical );
        critical.ActorId.ShouldBe( 3712 );
        critical.ActorName.ShouldBe( "Albert" );
        critical.Schemes.ShouldBe( albert.Schemes );
    }

    [Test]
    public void schemes_cannot_contain_null()
    {
        Util.Invokable( () => new ActorInfo( 3712, "Albert", AuthLevel.Normal, ImmutableArray.Create<SchemeInfo>( new SchemeInfo( "Basic", _time0 ), null! ) ) )
            .ShouldThrow<ArgumentException>();
    }

    [Test]
    public void UserSchemeInfo_requires_a_name_and_a_non_local_date()
    {
        Util.Invokable( () => new SchemeInfo( " ", _time0 ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new SchemeInfo( "Basic", DateTime.Now ) ).ShouldThrow<ArgumentException>();
        var s = new SchemeInfo( "Basic", new DateTime( 2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified ) );
        s.LastUsed.Kind.ShouldBe( DateTimeKind.Utc );
    }
}
