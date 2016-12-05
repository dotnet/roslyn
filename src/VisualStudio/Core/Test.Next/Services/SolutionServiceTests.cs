// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class SolutionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCreation()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var solution = workspace.CurrentSolution;
                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);

                var map = solution.GetAssetMap();

                var sessionId = 0;
                var storage = new AssetStorage(enableCleanup: false);
                var source = new MyAssetSource(storage, sessionId, map);
                var service = new SolutionService(new AssetService(sessionId, storage));

                var synched = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

                Assert.Equal(solutionChecksum, await synched.State.GetChecksumAsync(CancellationToken.None));
            }
        }

        private class MyAssetSource : AssetSource
        {
            private readonly Dictionary<Checksum, object> _map;

            public MyAssetSource(AssetStorage assetStorage, int sessionId, Dictionary<Checksum, object> map) :
                base(assetStorage, sessionId)
            {
                _map = map;
            }

            public override Task<IList<(Checksum, object)>> RequestAssetsAsync(int serviceId, ISet<Checksum> checksums, CancellationToken cancellationToken)
            {
                var list = new List<(Checksum, object)>();

                foreach (var checksum in checksums)
                {
                    object data;
                    Assert.True(_map.TryGetValue(checksum, out data));

                    list.Add(ValueTuple.Create(checksum, data));
                }

                return Task.FromResult<IList<(Checksum, object)>>(list);
            }
        }
    }
}
