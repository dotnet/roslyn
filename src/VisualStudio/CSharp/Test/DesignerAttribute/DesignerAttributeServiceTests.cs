// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.DesignerAttributes
{
    [UseExportProvider]
    public class DesignerAttributeServiceTests
    {
        private static readonly TestComposition s_inProcessComposition = EditorTestCompositions.EditorFeatures;
        private static readonly TestComposition s_outOffProcessComposition = s_inProcessComposition.WithTestHostParts(TestHost.OutOfProcess);

        [Theory, CombinatorialData]
        public async Task NoDesignerTest1(TestHost host)
        {
            var code = @"class Test { }";

            await TestAsync(code, category: null, host);
        }

        [Theory, CombinatorialData]
        public async Task NoDesignerOnSecondClass(TestHost host)
        {

            await TestAsync(
@"class Test1 { }

[System.ComponentModel.DesignerCategory(""Form"")]
class Test2 { }", category: null, host);
        }

        [Theory, CombinatorialData]
        public async Task NoDesignerOnStruct(TestHost host)
        {

            await TestAsync(
@"
[System.ComponentModel.DesignerCategory(""Form"")]
struct Test1 { }", category: null, host);
        }

        [Theory, CombinatorialData]
        public async Task NoDesignerOnNestedClass(TestHost host)
        {

            await TestAsync(
@"class Test1
{
    [System.ComponentModel.DesignerCategory(""Form"")]
    class Test2 { }
}", category: null, host);
        }

        [Theory, CombinatorialData]
        public async Task SimpleDesignerTest(TestHost host)
        {

            await TestAsync(
@"[System.ComponentModel.DesignerCategory(""Form"")]
class Test { }", "Form", host);
        }

        [Theory, CombinatorialData]
        public async Task SimpleDesignerTest2(TestHost host)
        {

            await TestAsync(
@"using System.ComponentModel;

[DesignerCategory(""Form"")]
class Test { }", "Form", host);
        }

        private static async Task TestAsync(string codeWithMarker, string? category, TestHost host)
        {
            using var workspace = TestWorkspace.CreateCSharp(
                codeWithMarker, openDocuments: false, composition: host == TestHost.OutOfProcess ? s_outOffProcessComposition : s_inProcessComposition);

            var solution = workspace.CurrentSolution;

            var hostDocument = workspace.Documents.First();
            var documentId = hostDocument.Id;

            var service = solution.Services.GetRequiredService<IDesignerAttributeDiscoveryService>();
            var stream = service.ProcessProjectAsync(solution.GetRequiredProject(documentId.ProjectId), priorityDocumentId: null, CancellationToken.None);

            var items = new List<DesignerAttributeData>();
            await foreach (var item in stream)
                items.Add(item);

            if (category != null)
            {
                Assert.Equal(1, items.Count);
                Assert.Equal(category, items.Single().Category);
                Assert.Equal(documentId, items.Single().DocumentId);
            }
            else
            {
                Assert.Empty(items);
            }
        }
    }
}
