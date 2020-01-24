﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class ProjectsHandlerTests : AbstractLiveShareRequestHandlerTests
    {
        [Fact]
        public async Task TestProjectsAsync()
        {
            var (solution, ranges) = CreateTestSolution(string.Empty);
            var expected = solution.Projects.Select(p => CreateLspProject(p)).ToArray();

            var results = (CustomProtocol.Project[])await TestHandleAsync<object, object[]>(solution, null);
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
