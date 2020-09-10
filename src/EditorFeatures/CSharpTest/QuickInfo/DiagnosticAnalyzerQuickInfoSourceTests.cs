// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    [UseExportProvider]
    public class DiagnosticAnalyzerQuickInfoSourceTests
    {
        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ErrorTileIsShownOnDisablePragma()
        {
            await TestInMethodAsync(
@"
#pragma warning disable CS0219$$
            var i = 0;
#pragma warning restore CS0219
", GetFormattedErrorTitle(ErrorCode.WRN_UnreferencedVarAssg));
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ErrorTileIsShownOnRestorePragma()
        {
            await TestInMethodAsync(
@"
#pragma warning disable CS0219
            var i = 0;
#pragma warning restore CS0219$$
", GetFormattedErrorTitle(ErrorCode.WRN_UnreferencedVarAssg));
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DisabledWarningNotExistingInCodeIsDisplayedByTitleWithoutCodeDetails()
        {
            await TestInMethodAsync(
@"
#pragma warning disable CS0219$$
", GetFormattedErrorTitle(ErrorCode.WRN_UnreferencedVarAssg));
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("#pragma warning disable $$CS0162", (int)ErrorCode.WRN_UnreachableCode)]
        [InlineData("#pragma warning disable $$CS0162, CS0219", (int)ErrorCode.WRN_UnreachableCode)]
        [InlineData("#pragma warning disable $$CS0219", (int)ErrorCode.WRN_UnreferencedVarAssg)]
        [InlineData("#pragma warning disable CS0162, $$CS0219", (int)ErrorCode.WRN_UnreferencedVarAssg)]
        [InlineData("#pragma warning disable CS0162, CS0219$$", (int)ErrorCode.WRN_UnreferencedVarAssg)]
        [InlineData("#pragma warning $$disable CS0162, CS0219", (int)ErrorCode.WRN_UnreachableCode)]
        [InlineData("#pragma warning $$disable CS0219, CS0162", (int)ErrorCode.WRN_UnreferencedVarAssg)]
        public async Task MultipleWarningsAreDisplayedDependingOnCursorPosition(string pragma, int errorCode)
        {
            await TestInMethodAsync(
@$"
{pragma}
        return;
        var i = 0;
", GetFormattedErrorTitle((ErrorCode)errorCode));
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ErrorTitleIsShwonInSupressMessageAttribute()
        {
            await TestAsync(
@"
using System.Diagnostics.CodeAnalysis;
namespace T
{
    [SuppressMessage(""CodeQuality"", ""IDE0051$$"")]
    public class C
    {
        private int _i;
    }
}
", GetFormattedIDEAnalyzerTitle(51, nameof(AnalyzersResources.Remove_unused_private_members)), ImmutableArray<TextSpan>.Empty);
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData(@"[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(""CodeQuality"", ""IDE0051$$"")]", true)]
        [InlineData(@"[System.Diagnostics.CodeAnalysis.SuppressMessage(""CodeQuality"", ""IDE0051$$"")]", true)]
        [InlineData(@"[System.Diagnostics.CodeAnalysis.SuppressMessage(""CodeQuality$$"", ""IDE0051"")]", false)]
        [InlineData(@"[System.Diagnostics.CodeAnalysis.SuppressMessage(""CodeQuality"", ""IDE0051"", Justification = ""WIP$$"")]", false)]
        [InlineData(@"[System.Diagnostics.CodeAnalysis.SuppressMessage$$(""CodeQuality"", ""IDE0051"")]", false)]
        [InlineData(@"[SuppressMessage(""CodeQuality"", ""$$IDE0051"")]", true)]
        [InlineData(@"[SuppressMessage(""CodeQuality"", ""$$IDE0051: Remove unused private member"")]", true)]
        [InlineData(@"[SuppressMessage(category: ""CodeQuality"", checkId$$: ""IDE0051: Remove unused private member"")]", true)]
        [InlineData(@"[SuppressMessage(category: ""CodeQuality"", $$checkId: ""IDE0051: Remove unused private member"")]", true)]
        [InlineData(@"[SuppressMessage(checkId$$: ""IDE0051: Remove unused private member"", category: ""CodeQuality"")]", true)]
        [InlineData(@"[SuppressMessage(checkId: ""IDE0051: Remove unused private member"", category$$: ""CodeQuality"")]", false)]
        [InlineData(@"[SuppressMessage(""CodeQuality"", DiagnosticIds.IDE0051 +$$ "": Remove unused private member"")]", true)]
        [InlineData(@"[SuppressMessage(""CodeQuality"", """" + (DiagnosticIds.IDE0051 +$$ "": Remove unused private member""))]", true)]
        [InlineData(@"[SuppressMessage(category: ""CodeQuality"", checkId$$: DiagnosticIds.IDE0051 + "": Remove unused private member"")]", true)]
        // False negative: Aliased attribute is not supported
        [InlineData(@"[SM(""CodeQuality"", ""IDE0051$$""", false)]
        public async Task QuickInfoSuppressMessageAttributeUseCases(string suppressMessageAttribute, bool shouldShowQuickInfo)
        {
            var description = shouldShowQuickInfo
                ? GetFormattedIDEAnalyzerTitle(51, nameof(AnalyzersResources.Remove_unused_private_members))
                : null;
            await TestAsync(
@$"
using System.Diagnostics.CodeAnalysis;
using SM = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
namespace T
{{
    public static class DiagnosticIds
    {{
        public const string IDE0051 = ""IDE0051"";
    }}

    {suppressMessageAttribute}
    public class C
    {{
        private int _i;
    }}
}}
", description, ImmutableArray<TextSpan>.Empty);
        }

        protected static async Task AssertContentIsAsync(TestWorkspace workspace, Document document, int position, string expectedDescription,
            ImmutableArray<TextSpan> relatedSpans)
        {
            var info = await GetQuickinfo(workspace, document, position);
            var description = info?.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
            Assert.NotNull(description);
            Assert.Equal(expectedDescription, description.Text);
            Assert.Collection(relatedSpans,
                info.RelatedSpans.Select(actualSpan => new Action<TextSpan>(expectedSpan => Assert.Equal(expectedSpan, actualSpan))).ToArray());
        }

        private static async Task<QuickInfoItem> GetQuickinfo(TestWorkspace workspace, Document document, int position)
        {
            var diagnosticAnalyzerService = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            var provider = new CSharpDiagnosticAnalyzerQuickInfoProvider(diagnosticAnalyzerService);
            var info = await provider.GetQuickInfoAsync(new QuickInfoContext(document, position, CancellationToken.None));
            return info;
        }

        protected static async Task AssertNoContentAsync(TestWorkspace workspace, Document document, int position)
        {
            var info = await GetQuickinfo(workspace, document, position);
            Assert.Null(info);
        }

        protected static async Task TestAsync(
            string code,
            string expectedDescription,
            ImmutableArray<TextSpan> relatedSpans,
            CSharpParseOptions parseOptions = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, parseOptions);
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(
                new CSharpCompilerDiagnosticAnalyzer(),
                new CSharpRemoveUnusedMembersDiagnosticAnalyzer()));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var testDocument = workspace.Documents.Single();
            var position = testDocument.CursorPosition.Value;
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            if (string.IsNullOrEmpty(expectedDescription))
            {
                await AssertNoContentAsync(workspace, document, position);
            }
            else
            {
                await AssertContentIsAsync(workspace, document, position, expectedDescription, relatedSpans);
            }
        }

        private static string GetFormattedErrorTitle(ErrorCode errorCode)
        {
            var localizable = MessageProvider.Instance.GetTitle((int)errorCode);
            return $"CS{(int)errorCode:0000}: {localizable}";
        }

        private static string GetFormattedIDEAnalyzerTitle(int ideDiagnosticId, string nameOfLocalizableStringResource)
        {
            var localizable = new LocalizableResourceString(nameOfLocalizableStringResource, AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
            return $"IDE{ideDiagnosticId:0000}: {localizable}";
        }

        protected static Task TestInClassAsync(string code, string expectedDescription, params TextSpan[] relatedSpans)
            => TestAsync(
@"class C
{" + code + "}", expectedDescription, relatedSpans.ToImmutableArray());

        protected static Task TestInMethodAsync(string code, string expectedDescription, params TextSpan[] relatedSpans)
            => TestInClassAsync(
@"void M()
{" + code + "}", expectedDescription, relatedSpans);
    }
}
