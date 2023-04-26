// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class ProjectsHandlerTests : AbstractLiveShareRequestHandlerTests
    {
        public ProjectsHandlerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestProjectsAsync(bool mutatingLspWorkspace)
        {
            await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
            var solution = testLspServer.GetCurrentSolution();
            var expected = solution.Projects.Select(p => CreateLspProject(p)).ToArray();

            var results = (CustomProtocol.Project[])await TestHandleAsync<object, object[]>(solution, null, CustomProtocol.RoslynMethods.ProjectsName);
            AssertJsonEquals(expected, results);
        }

        private static CustomProtocol.Project CreateLspProject(Project project)
            => new CustomProtocol.Project()
            {
                Language = project.Language,
                Name = project.Name,
                SourceFiles = project.Documents.Select(document => document.GetURI()).ToArray()
            };
    }
}
