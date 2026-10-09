using NUnit.Framework;
using Shouldly;
using System;

namespace CK.Core.Tests;

[TestFixture]
public class PasskeyTests
{
    static readonly byte[] _id = [1, 2, 3];
    static readonly byte[] _handle = [4, 5];
    static readonly byte[] _key = [6, 7, 8, 9];

    static PasskeyCredential Create( byte[]? id = null,
                                     int actorId = 3712,
                                     byte[]? handle = null,
                                     byte[]? key = null,
                                     bool backupEligible = true,
                                     bool backupState = true,
                                     string[]? transports = null,
                                     string name = "My phone" )
    {
        return new PasskeyCredential( id ?? _id, actorId, handle ?? _handle, key ?? _key, 0, backupEligible, backupState, Guid.Empty, transports ?? ["internal", "hybrid"], name );
    }

    [Test]
    public void PasskeyCredential_keeps_its_values()
    {
        var aaguid = Guid.NewGuid();
        var c = new PasskeyCredential( _id, 3712, _handle, _key, 42, true, false, aaguid, ["usb"], "Key" );
        c.CredentialId.ToArray().ShouldBe( _id );
        c.ActorId.ShouldBe( 3712 );
        c.UserHandle.ToArray().ShouldBe( _handle );
        c.PublicKey.ToArray().ShouldBe( _key );
        c.SignCount.ShouldBe( 42u );
        c.BackupEligible.ShouldBeTrue();
        c.BackupState.ShouldBeFalse();
        c.Aaguid.ShouldBe( aaguid );
        c.Transports.ShouldBe( ["usb"] );
        c.Name.ShouldBe( "Key" );
    }

    [Test]
    public void PasskeyCredential_copies_its_bytes()
    {
        var id = (byte[])_id.Clone();
        var handle = (byte[])_handle.Clone();
        var key = (byte[])_key.Clone();
        var transports = new[] { "internal" };
        var c = Create( id, handle: handle, key: key, transports: transports );
        id[0] = handle[0] = key[0] = 0;
        transports[0] = "changed";
        c.CredentialId.ToArray().ShouldBe( _id );
        c.UserHandle.ToArray().ShouldBe( _handle );
        c.PublicKey.ToArray().ShouldBe( _key );
        c.Transports.ShouldBe( ["internal"] );
    }

    [Test]
    public void PasskeyCredential_checks_its_arguments()
    {
        Util.Invokable( () => Create( id: [] ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => Create( id: new byte[PasskeyCredential.MaxCredentialIdLength + 1] ) ).ShouldThrow<ArgumentException>();
        Create( id: new byte[PasskeyCredential.MaxCredentialIdLength] ).CredentialId.Length.ShouldBe( 1023 );
        Util.Invokable( () => Create( actorId: 0 ) ).ShouldThrow<ArgumentException>( "Never the anonymous." );
        Util.Invokable( () => Create( handle: [] ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => Create( handle: new byte[PasskeyCredential.MaxUserHandleLength + 1] ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => Create( key: [] ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => Create( backupEligible: false, backupState: true ) ).ShouldThrow<ArgumentException>();
        Create( backupEligible: false, backupState: false ).BackupEligible.ShouldBeFalse();
        Util.Invokable( () => Create( transports: [" "] ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => Create( name: " " ) ).ShouldThrow<ArgumentException>();
    }

    [Test]
    public void PasskeyLoginPayload_keeps_and_copies_its_values()
    {
        PasskeyLoginPayload.Scheme.ShouldBe( "Passkey" );
        var id = (byte[])_id.Clone();
        var p = new PasskeyLoginPayload( id, 12, true );
        id[0] = 0;
        p.CredentialId.ToArray().ShouldBe( _id );
        p.SignCount.ShouldBe( 12u );
        p.BackupState.ShouldBeTrue();
        Util.Invokable( () => new PasskeyLoginPayload( ReadOnlyMemory<byte>.Empty, 0, false ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => new PasskeyLoginPayload( new byte[PasskeyCredential.MaxCredentialIdLength + 1], 0, false ) ).ShouldThrow<ArgumentException>();
    }
}
