// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpCodeActions_NetCore : AbstractEditorTest
    {
        public CSharpCodeActions_NetCore()
            : base(nameof(CSharpCodeActions_NetCore), WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllOccurrences_CodeRefactoring_ContainingMember()
        {
            var markup = @"
class C1
{
    void M()
    {
        var singleLine1 = $$""a"";
        var singleLine2 = @""goo""""bar"";
    }

    void M2()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";
            var expectedText = @"
class C1
{
    void M()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }

    void M2()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";

            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

            MarkupTestFile.GetSpans(markup, out _, out ImmutableArray<TextSpan> _);
            await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                new[]
                {
                    FeatureAttribute.Workspace,
                    FeatureAttribute.SolutionCrawler,
                    FeatureAttribute.DiagnosticService,
                    FeatureAttribute.ErrorSquiggles
                },
                HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.CodeActionAsync(
                "Convert to raw string",
                applyFix: true,
                fixAllScope: FixAllScope.ContainingMember,
                cancellationToken: HangMitigatingCancellationToken);

            AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllOccurrences_CodeRefactoring_ContainingType()
        {
            var markup1 = @"
partial class C1
{
    void M()
    {
        var singleLine1 = $$""a"";
        var singleLine2 = @""goo""""bar"";
    }

    void M2()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

partial class C1
{
    void M4()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";
            var expectedText1 = @"
partial class C1
{
    void M()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }

    void M2()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

partial class C1
{
    void M4()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }
}";

            var markup2 = @"
partial class C1
{
    void M5()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M6()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";
            var expectedText2 = @"
partial class C1
{
    void M5()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }
}

class C2
{
    void M6()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";

            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", markup2, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

            MarkupTestFile.GetSpans(markup1, out _, out ImmutableArray<TextSpan> _);
            await SetUpEditorAsync(markup1, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                new[]
                {
                    FeatureAttribute.Workspace,
                    FeatureAttribute.SolutionCrawler,
                    FeatureAttribute.DiagnosticService,
                    FeatureAttribute.ErrorSquiggles
                },
                HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.CodeActionAsync(
                "Convert to raw string",
                applyFix: true,
                fixAllScope: FixAllScope.ContainingType,
                cancellationToken: HangMitigatingCancellationToken);

            AssertEx.EqualOrDiff(expectedText1, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

            AssertEx.EqualOrDiff(expectedText2, await TestServices.SolutionExplorer.GetFileContentsAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken));
        }
    }
}
