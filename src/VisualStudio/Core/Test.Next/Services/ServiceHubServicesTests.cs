// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.Remote;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class ServiceHubServicesTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestRemoteHostCreation()
        {
            var remoteHostService = CreateService();
            Assert.NotNull(remoteHostService);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestRemoteHostConnect()
        {
            var remoteHostService = CreateService();

            var input = "Test";
            var output = remoteHostService.Connect(input);

            Assert.Equal(input, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteHostSynchronize()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var solution = workspace.CurrentSolution;

                var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, CancellationToken.None));
                using (var session = await client.CreateServiceSessionAsync(WellKnownRemoteHostServices.RemoteHostService, solution, CancellationToken.None))
                {
                    await session.InvokeAsync(WellKnownRemoteHostServices.RemoteHostService_SynchronizeAsync);
                }

                var storage = client.AssetStorage;

                var map = solution.GetAssetMap();

                object data;
                foreach (var kv in map)
                {
                    Assert.True(storage.TryGetAsset(kv.Key, out data));
                }
            }
        }

        private static RemoteHostService CreateService()
        {
            var stream = new MemoryStream();
            return new RemoteHostService(stream, new InProcRemoteHostClient.ServiceProvider());
        }
    }
}
