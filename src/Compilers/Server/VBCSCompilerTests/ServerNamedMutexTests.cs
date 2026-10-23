// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
#if NETFRAMEWORK
using System.Security.AccessControl;
using System.Security.Principal;
#endif
using System.Threading;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public sealed class ServerNamedMutexTests
    {
        [ConditionalTheory(typeof(NotOnAnyMono)), CombinatorialData]
        public void ServerMutexCreationAndDiscovery(bool client)
        {
            var pipeName = ServerUtil.GetPipeName();
            var mutexName = client
                ? BuildServerConnection.GetClientMutexName(pipeName)
                : BuildServerConnection.GetServerMutexName(pipeName);
            Assert.False(BuildServerConnection.WasServerMutexOpen(mutexName));

            using (var mutex = client
                ? BuildServerConnection.OpenOrCreateClientMutex(mutexName, out _)
                : BuildServerConnection.OpenOrCreateMutex(mutexName, out _))
            {
                Assert.True(BuildServerConnection.WasServerMutexOpen(mutexName));
#if NET
                using var peer = new Mutex(initiallyOwned: false, mutexName, new NamedWaitHandleOptions
                {
                    CurrentUserOnly = true,
                    CurrentSessionOnly = false,
                }, out var createdNew);
                Assert.False(createdNew);
#else
                using var peer = Mutex.OpenExisting(mutexName, MutexRights.Synchronize | MutexRights.Modify | MutexRights.ReadPermissions);
                using var identity = WindowsIdentity.GetCurrent();
                var security = peer.GetAccessControl();
                Assert.Equal(identity.User, security.GetOwner(typeof(SecurityIdentifier)));
                var rule = Assert.Single(security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)));
                var accessRule = Assert.IsType<MutexAccessRule>(rule);
                Assert.Equal(identity.User, accessRule.IdentityReference);
                Assert.Equal(AccessControlType.Allow, accessRule.AccessControlType);
                Assert.Equal(MutexRights.FullControl, accessRule.MutexRights);
#endif
            }

            Assert.False(BuildServerConnection.WasServerMutexOpen(mutexName));
        }

#if NET
        [ConditionalFact(typeof(UnixLikeOnly))]
        public void UserScopesAreIndependentOnUnix()
        {
            var mutexName = BuildServerConnection.GetServerMutexName(ServerUtil.GetPipeName());
            var crossUserOptions = new NamedWaitHandleOptions
            {
                CurrentUserOnly = false,
                CurrentSessionOnly = false,
            };
            using var crossUserMutex = new Mutex(initiallyOwned: false, mutexName, crossUserOptions, out var createdNew);
            Assert.True(createdNew);
            Assert.False(ServerNamedMutex.WasOpen(mutexName));

            using var userMutex = new ServerNamedMutex(mutexName, out createdNew);
            Assert.True(createdNew);
            Assert.True(ServerNamedMutex.WasOpen(mutexName));
            Assert.True(Mutex.TryOpenExisting(mutexName, crossUserOptions, out var peer));
            peer.Dispose();
        }
#else
        [ConditionalTheory(typeof(WindowsOnly)), CombinatorialData]
        public void RejectsIncompatibleExistingMutex(bool nullDacl)
        {
            var mutexName = BuildServerConnection.GetServerMutexName(ServerUtil.GetPipeName());
            using var identity = WindowsIdentity.GetCurrent();
            var security = new MutexSecurity();
            security.SetOwner(identity.User);
            if (nullDacl)
            {
                security.SetSecurityDescriptorSddlForm("D:NO_ACCESS_CONTROL", AccessControlSections.Access);
            }
            else
            {
                security.AddAccessRule(new MutexAccessRule(identity.User, MutexRights.FullControl, AccessControlType.Allow));
                security.AddAccessRule(new MutexAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                    MutexRights.Synchronize,
                    AccessControlType.Allow));
            }

            using var mutex = new Mutex(initiallyOwned: false, mutexName, out var createdNew, security);
            Assert.True(createdNew);
            Assert.True(Mutex.TryOpenExisting(mutexName, out var peer));
            peer.Dispose();
            Assert.False(ServerNamedMutex.WasOpen(mutexName));
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => new ServerNamedMutex(mutexName, out _));
        }
#endif
    }
}
