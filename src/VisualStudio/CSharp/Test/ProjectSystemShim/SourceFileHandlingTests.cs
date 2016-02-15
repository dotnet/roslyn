// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    public class SourceFileHandlingTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(1100114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100114")]
        public void IgnoreAdditionsOfXomlFiles()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.OnSourceFileAdded("Foo.xoml");

                // Even though we added a source file, since it has a .xoml extension we'll ignore it
                Assert.Empty(environment.Workspace.CurrentSolution.Projects.Single().Documents);

                // Try removing it to make sure it doesn't throw
                project.OnSourceFileRemoved("Foo.xoml");
            }
        }
    }
}
