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
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Outlining)]
    public class StructureTaggerTests
    {
        [WpfTheory]
        [CombinatorialData]
        public async Task CSharpOutliningTagger(
            bool collapseRegionsWhenCollapsingToDefinitions,
            bool showBlockStructureGuidesForDeclarationLevelConstructs,
            bool showBlockStructureGuidesForCodeLevelConstructs)
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
            if (false)
            {
                return;
            }

            int x = 5;
        }
    }
#endregion
}";

            using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var globalOptions = workspace.GlobalOptions;

            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp, collapseRegionsWhenCollapsingToDefinitions);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForDeclarationLevelConstructs);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForCodeLevelConstructs);

            var tags = await GetTagsFromWorkspaceAsync(workspace);

            Assert.Collection(tags,
                namespaceTag =>
                {
                    Assert.False(namespaceTag.IsImplementation);
                    Assert.Equal(17, GetCollapsedHintLineCount(namespaceTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Namespace : PredefinedStructureTagTypes.Nonstructural, namespaceTag.Type);
                    Assert.Equal("namespace MyNamespace", GetHeaderText(namespaceTag));
                },
                regionTag =>
                {
                    Assert.Equal(collapseRegionsWhenCollapsingToDefinitions, regionTag.IsImplementation);
                    Assert.Equal(14, GetCollapsedHintLineCount(regionTag));
                    Assert.Equal(PredefinedStructureTagTypes.Nonstructural, regionTag.Type);
                    Assert.Equal("#region MyRegion", GetHeaderText(regionTag));
                },
                classTag =>
                {
                    Assert.False(classTag.IsImplementation);
                    Assert.Equal(12, GetCollapsedHintLineCount(classTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Type : PredefinedStructureTagTypes.Nonstructural, classTag.Type);
                    Assert.Equal("public class MyClass", GetHeaderText(classTag));
                },
                methodTag =>
                {
                    Assert.True(methodTag.IsImplementation);
                    Assert.Equal(9, GetCollapsedHintLineCount(methodTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Member : PredefinedStructureTagTypes.Nonstructural, methodTag.Type);
                    Assert.Equal("static void Main(string[] args)", GetHeaderText(methodTag));
                },
                ifTag =>
                {
                    Assert.False(ifTag.IsImplementation);
                    Assert.Equal(4, GetCollapsedHintLineCount(ifTag));
                    Assert.Equal(showBlockStructureGuidesForCodeLevelConstructs ? PredefinedStructureTagTypes.Conditional : PredefinedStructureTagTypes.Nonstructural, ifTag.Type);
                    Assert.Equal("if (false)", GetHeaderText(ifTag));
                });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task CSharpImportsFileScopedNamespaceTest(
            bool collapseRegionsWhenCollapsingToDefinitions,
            bool showBlockStructureGuidesForDeclarationLevelConstructs,
            bool showBlockStructureGuidesForCodeLevelConstructs)
        {
            var code =
@"
namespace Foo;

using System;
using System.Linq;
public class Bar
{

}
";

            using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var globalOptions = workspace.GlobalOptions;

            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp, collapseRegionsWhenCollapsingToDefinitions);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForDeclarationLevelConstructs);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForCodeLevelConstructs);

            var tags = await GetTagsFromWorkspaceAsync(workspace);

            Assert.Collection(tags,
                importsTag =>
                {
                    Assert.Equal(2, GetCollapsedHintLineCount(importsTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Imports : PredefinedStructureTagTypes.Nonstructural, importsTag.Type);
                    Assert.Equal("using ", GetHeaderText(importsTag));
                },
                classTag =>
                {
                    Assert.Equal(4, GetCollapsedHintLineCount(classTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Type : PredefinedStructureTagTypes.Nonstructural, classTag.Type);
                    Assert.Equal("public class Bar", GetHeaderText(classTag));
                });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task CSharpCommentsFileScopedNamespace(
            bool collapseRegionsWhenCollapsingToDefinitions,
            bool showBlockStructureGuidesForDeclarationLevelConstructs,
            bool showBlockStructureGuidesForCodeLevelConstructs,
            bool showBlockStructureGuidesForCommentsAndPreprocessorRegions)
        {
            var code =
@"
namespace Foo;
/// <summary>
/// 
/// </summary>

public class Bar
{

}
";

            using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var globalOptions = workspace.GlobalOptions;

            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp, collapseRegionsWhenCollapsingToDefinitions);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForDeclarationLevelConstructs);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForCodeLevelConstructs);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, LanguageNames.CSharp, showBlockStructureGuidesForCommentsAndPreprocessorRegions);

            var tags = await GetTagsFromWorkspaceAsync(workspace);

            Assert.Collection(tags,
                commentsTag =>
                {
                    Assert.Equal(3, GetCollapsedHintLineCount(commentsTag));
                    Assert.Equal(showBlockStructureGuidesForCommentsAndPreprocessorRegions ? PredefinedStructureTagTypes.Comment : PredefinedStructureTagTypes.Nonstructural, commentsTag.Type);
                    Assert.Equal("/// <summary>", GetHeaderText(commentsTag));
                },
                classTag =>
                {
                    Assert.Equal(4, GetCollapsedHintLineCount(classTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Type : PredefinedStructureTagTypes.Nonstructural, classTag.Type);
                    Assert.Equal("public class Bar", GetHeaderText(classTag));
                });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task CSharpImportsNormalNamespaceTest(
            bool collapseRegionsWhenCollapsingToDefinitions,
            bool showBlockStructureGuidesForDeclarationLevelConstructs,
            bool showBlockStructureGuidesForCodeLevelConstructs)
        {
            var code =
@"
namespace Foo
{
    using System;
    using System.Linq;
    public class Bar
    {

    }
}
";

            using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var globalOptions = workspace.GlobalOptions;

            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp, collapseRegionsWhenCollapsingToDefinitions);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForDeclarationLevelConstructs);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp, showBlockStructureGuidesForCodeLevelConstructs);

            var tags = await GetTagsFromWorkspaceAsync(workspace);

            Assert.Collection(tags,
                namespaceTag =>
                {
                    Assert.Equal(9, GetCollapsedHintLineCount(namespaceTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Namespace : PredefinedStructureTagTypes.Nonstructural, namespaceTag.Type);
                    Assert.Equal("namespace Foo", GetHeaderText(namespaceTag));
                },
                importsTag =>
                {
                    Assert.Equal(2, GetCollapsedHintLineCount(importsTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Imports : PredefinedStructureTagTypes.Nonstructural, importsTag.Type);
                    Assert.Equal("using ", GetHeaderText(importsTag));
                },
                classTag =>
                {
                    Assert.Equal(4, GetCollapsedHintLineCount(classTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Type : PredefinedStructureTagTypes.Nonstructural, classTag.Type);
                    Assert.Equal("public class Bar", GetHeaderText(classTag));
                });
        }

        [WpfTheory]
        [CombinatorialData]
        public async Task VisualBasicOutliningTagger(
            bool collapseRegionsWhenCollapsingToDefinitions,
            bool showBlockStructureGuidesForDeclarationLevelConstructs,
            bool showBlockStructureGuidesForCodeLevelConstructs)
        {
            var code = @"Imports System
Namespace MyNamespace
#Region ""MyRegion""
    Module M
        Sub Main(args As String())
            If False Then
                Return
            End If

            Dim x As Integer = 5
        End Sub
    End Module
#End Region
End Namespace";

            using var workspace = EditorTestWorkspace.CreateVisualBasic(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var globalOptions = workspace.GlobalOptions;

            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.VisualBasic, collapseRegionsWhenCollapsingToDefinitions);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.VisualBasic, showBlockStructureGuidesForDeclarationLevelConstructs);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.VisualBasic, showBlockStructureGuidesForCodeLevelConstructs);

            var tags = await GetTagsFromWorkspaceAsync(workspace);

            Assert.Collection(tags,
                namespaceTag =>
                {
                    Assert.False(namespaceTag.IsImplementation);
                    Assert.Equal(13, GetCollapsedHintLineCount(namespaceTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Namespace : PredefinedStructureTagTypes.Nonstructural, namespaceTag.Type);
                    Assert.Equal("Namespace MyNamespace", GetHeaderText(namespaceTag));
                },
                regionTag =>
                {
                    Assert.Equal(collapseRegionsWhenCollapsingToDefinitions, regionTag.IsImplementation);
                    Assert.Equal(11, GetCollapsedHintLineCount(regionTag));
                    Assert.Equal(PredefinedStructureTagTypes.Nonstructural, regionTag.Type);
                    Assert.Equal(@"#Region ""MyRegion""", GetHeaderText(regionTag));
                },
                moduleTag =>
                {
                    Assert.False(moduleTag.IsImplementation);
                    Assert.Equal(9, GetCollapsedHintLineCount(moduleTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Type : PredefinedStructureTagTypes.Nonstructural, moduleTag.Type);
                    Assert.Equal("Module M", GetHeaderText(moduleTag));
                },
                methodTag =>
                {
                    Assert.True(methodTag.IsImplementation);
                    Assert.Equal(7, GetCollapsedHintLineCount(methodTag));
                    Assert.Equal(showBlockStructureGuidesForDeclarationLevelConstructs ? PredefinedStructureTagTypes.Member : PredefinedStructureTagTypes.Nonstructural, methodTag.Type);
                    Assert.Equal("Sub Main(args As String())", GetHeaderText(methodTag));
                },
                ifTag =>
                {
                    Assert.False(ifTag.IsImplementation);
                    Assert.Equal(3, GetCollapsedHintLineCount(ifTag));
                    Assert.Equal(showBlockStructureGuidesForCodeLevelConstructs ? PredefinedStructureTagTypes.Conditional : PredefinedStructureTagTypes.Nonstructural, ifTag.Type);
                    Assert.Equal("If False Then", GetHeaderText(ifTag));
                });

        }

        [WpfFact]
        public async Task OutliningTaggerTooltipText()
        {
            var code = @"Module Module1
    Sub Main(args As String())
    End Sub
End Module";

            using var workspace = EditorTestWorkspace.CreateVisualBasic(code, composition: EditorTestCompositions.EditorFeaturesWpf);
            var tags = await GetTagsFromWorkspaceAsync(workspace);

            var hints = tags.Select(x => x.GetCollapsedHintForm()).Cast<ViewHostingControl>().ToArray();
            Assert.Equal("Sub Main(args As String())\r\nEnd Sub", hints[1].GetText_TestOnly()); // method
            hints.Do(v => v.TextView_TestOnly.Close());
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private static async Task<List<IStructureTag2>> GetTagsFromWorkspaceAsync(EditorTestWorkspace workspace)
        {
            var hostdoc = workspace.Documents.First();
            var view = hostdoc.GetTextView();

            var provider = workspace.ExportProvider.GetExportedValue<AbstractStructureTaggerProvider>();

            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);
            var context = new TaggerContext<IStructureTag2>(document, view.TextSnapshot);
            await provider.GetTestAccessor().ProduceTagsAsync(context);

            return context.TagSpans.Select(x => x.Tag).OrderBy(t => t.OutliningSpan.Value.Start).ToList();
        }
#pragma warning restore CS0618 // Type or member is obsolete

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
