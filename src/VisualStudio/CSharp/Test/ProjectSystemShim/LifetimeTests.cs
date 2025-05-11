// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim;

[UseExportProvider]
public sealed class LifetimeTests
{
    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/10358")]
    public void DisconnectingAProjectDoesNotLeak()
    {
        using var environment = new TestEnvironment();
        var project = ObjectReference.CreateFromFactory(() => CSharpHelpers.CreateCSharpProject(environment, "Test"));

        Assert.Single(environment.Workspace.CurrentSolution.Projects);

        project.UseReference(p => p.Disconnect());
        project.AssertReleased();

        Assert.Empty(environment.Workspace.CurrentSolution.Projects);
    }
}
