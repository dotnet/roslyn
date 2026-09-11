// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class MefWorkspaceServicesTests
{
    [Fact]
    public void FactoryMayReturnNull()
    {
        using var workspace = new AdhocWorkspace(FeaturesTestCompositions.Features.GetHostServices());

        Assert.Null(workspace.Services.GetService<IWorkspaceEventListenerService>());
    }
}
