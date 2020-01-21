// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    [UseExportProvider]
    public class LifetimeTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(10358, "https://github.com/dotnet/roslyn/issues/10358")]
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
}
