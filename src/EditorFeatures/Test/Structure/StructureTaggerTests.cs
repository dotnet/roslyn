// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    [UseExportProvider]
    public class StructureTaggerTests
    {
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Outlining)]
        [CombinatorialData]
        public async Task CSharpOutliningTagger(bool collapseRegionsWhenCollapsingToDefinitions)
        {
            var code =
@"using System;
namespace MyNamespace
{
#region MyRegion
    public class MyClass
    {
        static void Main(string[] args)
        {
            int x = 5;
        }
    }
#endregion
}";

            using var workspace = TestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                .WithChangedOption(BlockStructureOptions.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp, collapseRegionsWhenCollapsingToDefinitions)));

            var tags = await GetTagsFromWorkspaceAsync(workspace);

            Assert.Collection(tags,
                namespaceTag =>
                {
                    Assert.False(namespaceTag.IsImplementation);
                    Assert.Equal(12, GetCollapsedHintLineCount(namespaceTag));
                    Assert.Equal("namespace MyNamespace", GetHeaderText(namespaceTag));
                },
                regionTag =>
                {
                    Assert.Equal(collapseRegionsWhenCollapsingToDefinitions, regionTag.IsImplementation);
                    Assert.Equal(9, GetCollapsedHintLineCount(regionTag));
                    Assert.Equal("#region MyRegion", GetHeaderText(regionTag));
                },
                classTag =>
                {
                    Assert.False(classTag.IsImplementation);
                    Assert.Equal(7, GetCollapsedHintLineCount(classTag));
                    Assert.Equal("public class MyClass", GetHeaderText(classTag));
                },
                methodTag =>
                {
                    Assert.True(methodTag.IsImplementation);
                    Assert.Equal(4, GetCollapsedHintLineCount(methodTag));
                    Assert.Equal("static void Main(string[] args)", GetHeaderText(methodTag));
                });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task VisualBasicOutliningTagger()
        {
            var code = @"Imports System
Namespace MyNamespace
#Region ""MyRegion""
    Module M
        Sub Main(args As String())
            Dim x As Integer = 5
        End Sub
    End Module
#End Region
End Namespace";

            using var workspace = TestWorkspace.CreateVisualBasic(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var tags = await GetTagsFromWorkspaceAsync(workspace);

            Assert.Collection(tags,
                namespaceTag =>
                {
                    Assert.False(namespaceTag.IsImplementation);
                    Assert.Equal(9, GetCollapsedHintLineCount(namespaceTag));
                    Assert.Equal("Namespace MyNamespace", GetHeaderText(namespaceTag));
                },
                regionTag =>
                {
                    Assert.False(regionTag.IsImplementation);
                    Assert.Equal(7, GetCollapsedHintLineCount(regionTag));
                    Assert.Equal(@"#Region ""MyRegion""", GetHeaderText(regionTag));
                },
                moduleTag =>
                {
                    Assert.False(moduleTag.IsImplementation);
                    Assert.Equal(5, GetCollapsedHintLineCount(moduleTag));
                    Assert.Equal("Module M", GetHeaderText(moduleTag));
                },
                methodTag =>
                {
                    Assert.True(methodTag.IsImplementation);
                    Assert.Equal(3, GetCollapsedHintLineCount(methodTag));
                    Assert.Equal("Sub Main(args As String())", GetHeaderText(methodTag));
                });

        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task OutliningTaggerTooltipText()
        {
            var code = @"Module Module1
    Sub Main(args As String())
    End Sub
End Module";

            using var workspace = TestWorkspace.CreateVisualBasic(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var tags = await GetTagsFromWorkspaceAsync(workspace);

            var hints = tags.Select(x => x.GetCollapsedHintForm()).Cast<ViewHostingControl>().ToArray();
            Assert.Equal("Sub Main(args As String())\r\nEnd Sub", hints[1].GetText_TestOnly()); // method
            hints.Do(v => v.TextView_TestOnly.Close());
        }

        private static async Task<List<IStructureTag>> GetTagsFromWorkspaceAsync(TestWorkspace workspace)
        {
            var hostdoc = workspace.Documents.First();
            var view = hostdoc.GetTextView();

            var provider = workspace.ExportProvider.GetExportedValue<AbstractStructureTaggerProvider>();

            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);
            var context = new TaggerContext<IStructureTag>(document, view.TextSnapshot);
            await provider.GetTestAccessor().ProduceTagsAsync(context);

            return context.tagSpans.Select(x => x.Tag).OrderBy(t => t.OutliningSpan.Value.Start).ToList();
        }

        private static string GetHeaderText(IStructureTag namespaceTag)
        {
            return namespaceTag.Snapshot.GetText(namespaceTag.HeaderSpan.Value);
        }

        private static int GetCollapsedHintLineCount(IStructureTag tag)
        {
            var control = Assert.IsType<ViewHostingControl>(tag.GetCollapsedHintForm());
            var view = control.TextView_TestOnly;
            try
            {
                return view.TextSnapshot.LineCount;
            }
            finally
            {
                view.Close();
            }
        }
    }
}
