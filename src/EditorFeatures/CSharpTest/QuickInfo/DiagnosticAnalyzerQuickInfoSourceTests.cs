// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.QuickInfo)]
public sealed class DiagnosticAnalyzerQuickInfoSourceTests
{
    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/46604")]
    public Task ErrorTitleIsShownOnDisablePragma()
        => TestInMethodAsync(
            """
            #pragma warning disable CS0219$$
                        var i = 0;
            #pragma warning restore CS0219
            """, GetFormattedErrorTitle(ErrorCode.WRN_UnreferencedVarAssg));

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/46604")]
    public Task ErrorTitleIsShownOnRestorePragma()
        => TestInMethodAsync(
            """
            #pragma warning disable CS0219
                        var i = 0;
            #pragma warning restore CS0219$$
            """, GetFormattedErrorTitle(ErrorCode.WRN_UnreferencedVarAssg));

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/46604")]
    public Task DisabledWarningNotExistingInCodeIsDisplayedByTitleWithoutCodeDetails()
        => TestInMethodAsync(
            """
            #pragma warning disable CS0219$$
            """, GetFormattedErrorTitle(ErrorCode.WRN_UnreferencedVarAssg));

    [WorkItem("https://github.com/dotnet/roslyn/issues/49102")]
    [WpfTheory]
    [InlineData("CS0219$$")]
    [InlineData("219$$")]
    [InlineData("0219$$")]
    [InlineData("CS02$$19")]
    [InlineData("2$$19")]
    [InlineData("02$$19")]
    public Task PragmaWarningCompilerWarningSyntaxKinds(string warning)
        => TestInMethodAsync(
@$"
#pragma warning disable {warning}
", GetFormattedErrorTitle(ErrorCode.WRN_UnreferencedVarAssg));

    [WorkItem("https://github.com/dotnet/roslyn/issues/49102")]
    [WpfTheory]
    [InlineData("#pragma warning $$CS0219", null)]
    [InlineData("#pragma warning disable$$", null)]
    [InlineData("#pragma warning disable $$true", null)]
    [InlineData("#pragma warning disable $$219.0", (int)ErrorCode.WRN_UnreferencedVarAssg)]
    [InlineData("#pragma warning disable $$219.5", (int)ErrorCode.WRN_UnreferencedVarAssg)]
    public async Task PragmaWarningDoesNotThrowInBrokenSyntax(string pragma, int? errorCode)
    {
        var expectedDescription = errorCode is int errorCodeValue
            ? GetFormattedErrorTitle((ErrorCode)errorCodeValue)
            : "";
        await TestInMethodAsync(
@$"
{pragma}
", expectedDescription);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/46604")]
    [WpfTheory]
    [InlineData("#pragma warning disable $$CS0162", (int)ErrorCode.WRN_UnreachableCode)]
    [InlineData("#pragma warning disable $$CS0162, CS0219", (int)ErrorCode.WRN_UnreachableCode)]
    [InlineData("#pragma warning disable $$CS0219", (int)ErrorCode.WRN_UnreferencedVarAssg)]
    [InlineData("#pragma warning disable CS0162, $$CS0219", (int)ErrorCode.WRN_UnreferencedVarAssg)]
    [InlineData("#pragma warning disable CS0162, CS0219$$", (int)ErrorCode.WRN_UnreferencedVarAssg)]
    [InlineData("#pragma warning $$disable CS0162, CS0219", (int)ErrorCode.WRN_UnreachableCode)]
    [InlineData("#pragma warning $$disable CS0219, CS0162", (int)ErrorCode.WRN_UnreferencedVarAssg)]
    public Task MultipleWarningsAreDisplayedDependingOnCursorPosition(string pragma, int errorCode)
        => TestInMethodAsync(
@$"
{pragma}
        return;
        var i = 0;
", GetFormattedErrorTitle((ErrorCode)errorCode));

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/46604")]
    public Task ErrorTitleIsShwonInSupressMessageAttribute()
        => TestAsync(
            """
            using System.Diagnostics.CodeAnalysis;
            namespace T
            {
                [SuppressMessage("CodeQuality", "IDE0051$$")]
                public class C
                {
                    private int _i;
                }
            }
            """, GetFormattedIDEAnalyzerTitle(51, nameof(AnalyzersResources.Remove_unused_private_members)), []);

    [WorkItem("https://github.com/dotnet/roslyn/issues/46604")]
    [WpfTheory]
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
    [InlineData("""
        [SM("CodeQuality", "IDE0051$$"
        """, false)]
    public async Task QuickInfoSuppressMessageAttributeUseCases(string suppressMessageAttribute, bool shouldShowQuickInfo)
    {
        var description = shouldShowQuickInfo
            ? GetFormattedIDEAnalyzerTitle(51, nameof(AnalyzersResources.Remove_unused_private_members))
            : null;
        await TestAsync(
            $$"""
            using System.Diagnostics.CodeAnalysis;
            using SM = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
            namespace T
            {
                public static class DiagnosticIds
                {
                    public const string IDE0051 = "IDE0051";
                }

                {{suppressMessageAttribute}}
                public class C
                {
                    private int _i;
                }
            }
            """, description, []);
    }

    private static async Task AssertContentIsAsync(Document document, int position, string expectedDescription,
        ImmutableArray<TextSpan> relatedSpans)
    {
        var info = await GetQuickInfo(document, position);
        var description = info?.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
        Assert.NotNull(description);
        Assert.Equal(expectedDescription, description.Text);
        Assert.Collection(relatedSpans,
            [.. info.RelatedSpans.Select(actualSpan => new Action<TextSpan>(expectedSpan => Assert.Equal(expectedSpan, actualSpan)))]);
    }

    private static async Task<QuickInfoItem> GetQuickInfo(Document document, int position)
    {
        var provider = new CSharpDiagnosticAnalyzerQuickInfoProvider();
        var info = await provider.GetQuickInfoAsync(new QuickInfoContext(document, position, SymbolDescriptionOptions.Default, CancellationToken.None));
        return info;
    }

    private static async Task AssertNoContentAsync(Document document, int position)
    {
        var info = await GetQuickInfo(document, position);
        Assert.Null(info);
    }

    private static async Task TestAsync(
        string code,
        string expectedDescription,
        ImmutableArray<TextSpan> relatedSpans,
        CSharpParseOptions parseOptions = null)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions);
        var analyzerReference = new AnalyzerImageReference([new CSharpCompilerDiagnosticAnalyzer(), new CSharpRemoveUnusedMembersDiagnosticAnalyzer()]);
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([analyzerReference]));

        var testDocument = workspace.Documents.Single();
        var position = testDocument.CursorPosition.Value;
        var document = workspace.CurrentSolution.Projects.First().Documents.First();
        if (string.IsNullOrEmpty(expectedDescription))
        {
            await AssertNoContentAsync(document, position);
        }
        else
        {
            await AssertContentIsAsync(document, position, expectedDescription, relatedSpans);
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

    private static Task TestInClassAsync(string code, string expectedDescription, params TextSpan[] relatedSpans)
        => TestAsync(
            $$"""
            class C
            {
            {{code}}
            }
            """, expectedDescription, [.. relatedSpans]);

    private static Task TestInMethodAsync(string code, string expectedDescription, params TextSpan[] relatedSpans)
        => TestInClassAsync(
            $$"""
            void M()
            {
            {{code}}
            }
            """, expectedDescription, relatedSpans);
}
