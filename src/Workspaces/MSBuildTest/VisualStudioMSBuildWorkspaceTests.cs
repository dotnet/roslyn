// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class VisualStudioMSBuildWorkspaceTests : MSBuildWorkspaceTestBase
    {
        // On .NET Core this tests fails with "CodePape Not Found"
        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [WorkItem(991528, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991528")]
        public async Task MSBuildProjectShouldHandleCodePageProperty()
        {
            var files = new FileSet(
                ("Encoding.csproj", Resources.ProjectFiles.CSharp.Encoding.Replace("<CodePage>ReplaceMe</CodePage>", "<CodePage>1254</CodePage>")),
                ("class1.cs", "//\u201C"));

            CreateFiles(files);

            var projPath = GetSolutionFileName("Encoding.csproj");
            using var workspace = CreateMSBuildWorkspace();
            var project = await workspace.OpenProjectAsync(projPath);
            var document = project.Documents.First(d => d.Name == "class1.cs");
            var text = await document.GetTextAsync();
            Assert.Equal(Encoding.GetEncoding(1254), text.Encoding);

            // The smart quote (“) in class1.cs shows up as "â€œ" in codepage 1254. Do a sanity
            // check here to make sure this file hasn't been corrupted in a way that would
            // impact subsequent asserts.
            Assert.Equal(5, "//\u00E2\u20AC\u0153".Length);
            Assert.Equal("//\u00E2\u20AC\u0153".Length, text.Length);
        }
    }
}
