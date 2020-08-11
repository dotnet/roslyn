// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class FeaturesTestCompositions
    {
        public static readonly TestComposition Features = TestComposition.Empty
            .AddAssemblies(MefHostServices.DefaultAssemblies)
            .AddParts(typeof(MockWorkspaceEventListenerProvider)); // by default, avoid running Solution Crawler and other services that start in workspace event listeners

        public static readonly TestComposition RemoteHostFeatures = TestComposition.Empty
            .AddAssemblies(RoslynServices.RemoteHostAssemblies);
    }
}
