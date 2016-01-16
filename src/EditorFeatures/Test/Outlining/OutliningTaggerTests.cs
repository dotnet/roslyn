// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
{
    public class OutliningTaggerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task CSharpOutliningTagger()
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

            using (var workspace = await TestWorkspaceFactory.CreateCSharpAsync(code))
            {
                var tags = await GetTagsFromWorkspaceAsync(workspace);

                // ensure all 4 outlining region tags were found
                Assert.Equal(4, tags.Count);

                // ensure the method and #region outlining spans are marked as implementation
                Assert.False(tags[0].IsImplementation);
                Assert.False(tags[1].IsImplementation);
                Assert.True(tags[2].IsImplementation);
                Assert.True(tags[3].IsImplementation);

                // verify line counts
                var hints = tags.Select(x => x.CollapsedHintForm).Cast<ViewHostingControl>().Select(vhc => vhc.TextView_TestOnly).ToList();
                Assert.Equal(12, hints[0].TextSnapshot.LineCount); // namespace
                Assert.Equal(7, hints[1].TextSnapshot.LineCount); // class
                Assert.Equal(9, hints[2].TextSnapshot.LineCount); // region
                Assert.Equal(4, hints[3].TextSnapshot.LineCount); // method
                hints.Do(v => v.Close());
            }
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

            using (var workspace = await TestWorkspaceFactory.CreateVisualBasicAsync(code))
            {
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
                Assert.Equal(5, hints[1].TextSnapshot.LineCount); // class
                Assert.Equal(7, hints[2].TextSnapshot.LineCount); // region
                Assert.Equal(3, hints[3].TextSnapshot.LineCount); // method
                hints.Do(v => v.Close());
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task OutliningTaggerTooltipText()
        {
            var code = @"Module Module1
    Sub Main(args As String())
    End Sub
End Module";

            using (var workspace = await TestWorkspaceFactory.CreateVisualBasicAsync(code))
            {
                var tags = await GetTagsFromWorkspaceAsync(workspace);

                var hints = tags.Select(x => x.CollapsedHintForm).Cast<ViewHostingControl>().ToArray();
                Assert.Equal("Sub Main(args As String())\r\nEnd Sub", hints[1].ToString()); // method
                hints.Do(v => v.TextView_TestOnly.Close());
            }
        }

        private static async Task<List<IOutliningRegionTag>> GetTagsFromWorkspaceAsync(TestWorkspace workspace)
        {
            var hostdoc = workspace.Documents.First();
            var view = hostdoc.GetTextView();
            var textService = workspace.GetService<ITextEditorFactoryService>();
            var editorService = workspace.GetService<IEditorOptionsFactoryService>();
            var projectionService = workspace.GetService<IProjectionBufferFactoryService>();

            var provider = new OutliningTaggerProvider(
                workspace.ExportProvider.GetExportedValue<IForegroundNotificationService>(),
                textService, editorService, projectionService,
                AggregateAsynchronousOperationListener.EmptyListeners);

            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);
            var context = new TaggerContext<IOutliningRegionTag>(document, view.TextSnapshot);
            await provider.ProduceTagsAsync_ForTestingPurposesOnly(context);

            return context.tagSpans.Select(x => x.Tag).ToList();
        }
    }
}