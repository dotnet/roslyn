// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task CSharpOutliningTagger_RegionIsDefinition()
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
                .WithChangedOption(BlockStructureOptions.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp, true)));

            var tags = await GetTagsFromWorkspaceAsync(workspace);

            // ensure all 4 outlining region tags were found
            Assert.Equal(4, tags.Count);

            // ensure the method and #region outlining spans are marked as implementation
            Assert.False(tags[0].IsImplementation);
            Assert.True(tags[1].IsImplementation);
            Assert.False(tags[2].IsImplementation);
            Assert.True(tags[3].IsImplementation);

            // verify line counts
            var hints = tags.Select(x => x.CollapsedHintForm).Cast<ViewHostingControl>().Select(vhc => vhc.TextView_TestOnly).ToList();
            Assert.Equal(12, hints[0].TextSnapshot.LineCount); // namespace
            Assert.Equal(9, hints[1].TextSnapshot.LineCount); // region
            Assert.Equal(7, hints[2].TextSnapshot.LineCount); // class
            Assert.Equal(4, hints[3].TextSnapshot.LineCount); // method
            hints.Do(v => v.Close());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task CSharpOutliningTagger_RegionIsNotDefinition()
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
            var tags = await GetTagsFromWorkspaceAsync(workspace);

            // ensure all 4 outlining region tags were found
            Assert.Equal(4, tags.Count);

            // ensure only the method is marked as implementation
            Assert.False(tags[0].IsImplementation);
            Assert.False(tags[1].IsImplementation);
            Assert.False(tags[2].IsImplementation);
            Assert.True(tags[3].IsImplementation);

            // verify line counts
            var hints = tags.Select(x => x.CollapsedHintForm).Cast<ViewHostingControl>().Select(vhc => vhc.TextView_TestOnly).ToList();
            Assert.Equal(12, hints[0].TextSnapshot.LineCount); // namespace
            Assert.Equal(9, hints[1].TextSnapshot.LineCount); // region
            Assert.Equal(7, hints[2].TextSnapshot.LineCount); // class
            Assert.Equal(4, hints[3].TextSnapshot.LineCount); // method
            hints.Do(v => v.Close());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task VisualBasicOutliningTagger()
        {
            var code = @"Imports System
Namespace MyNamespace
#Region ""MyRegion""
    Module MyClass
        Sub Main(args As String())
            Dim x As Integer = 5
        End Sub
    End Module
#End Region
End Namespace";

            using var workspace = TestWorkspace.CreateVisualBasic(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var tags = await GetTagsFromWorkspaceAsync(workspace);

            // ensure all 4 outlining region tags were found
            Assert.Equal(4, tags.Count);

            // ensure only the method outlining region is marked as an implementation
            Assert.False(tags[0].IsImplementation);
            Assert.False(tags[1].IsImplementation);
            Assert.False(tags[2].IsImplementation);
            Assert.True(tags[3].IsImplementation);

            // verify line counts
            var hints = tags.Select(x => x.CollapsedHintForm).Cast<ViewHostingControl>().Select(vhc => vhc.TextView_TestOnly).ToList();
            Assert.Equal(9, hints[0].TextSnapshot.LineCount); // namespace
            Assert.Equal(7, hints[1].TextSnapshot.LineCount); // region
            Assert.Equal(5, hints[2].TextSnapshot.LineCount); // class
            Assert.Equal(3, hints[3].TextSnapshot.LineCount); // method
            hints.Do(v => v.Close());
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

            var hints = tags.Select(x => x.CollapsedHintForm).Cast<ViewHostingControl>().ToArray();
            Assert.Equal("Sub Main(args As String())\r\nEnd Sub", hints[1].GetText_TestOnly()); // method
            hints.Do(v => v.TextView_TestOnly.Close());
        }

        private static async Task<List<IOutliningRegionTag>> GetTagsFromWorkspaceAsync(TestWorkspace workspace)
        {
            var hostdoc = workspace.Documents.First();
            var view = hostdoc.GetTextView();

            var provider = workspace.ExportProvider.GetExportedValue<VisualStudio14StructureTaggerProvider>();

            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);
            var context = new TaggerContext<IOutliningRegionTag>(document, view.TextSnapshot);
            await provider.GetTestAccessor().ProduceTagsAsync(context);

            return context.tagSpans.Select(x => x.Tag).ToList();
        }
    }
}
