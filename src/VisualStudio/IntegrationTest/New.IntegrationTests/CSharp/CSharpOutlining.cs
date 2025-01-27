// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public class CSharpOutlining : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpOutlining()
        : base(nameof(CSharpOutlining))
    {
    }

    [IdeFact]
    public async Task Outlining()
    {
        var input = @"
using [|System;
using System.Collections.Generic;
using System.Text;|]

namespace ConsoleApplication1[|
{
    public class Program[|
    {
        public static void Main(string[] args)[|
        {
            Console.WriteLine(""Hello World"");
        }|]
    }|]
}|]";
        MarkupTestFile.GetSpans(input, out var text, out var spans);
        await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);
        var actualSpansWithState = await TestServices.Editor.GetOutliningSpansAsync(HangMitigatingCancellationToken);
        var actualSpans = actualSpansWithState.Select(span => span.Span);
        Assert.Equal(spans.OrderBy(s => s.Start), actualSpans);
    }

    [IdeFact]
    public async Task OutliningConfigChange()
    {
        var input = @"
namespace ClassLibrary1[|
{
    public class Class1[|
    {
#if DEBUG
{|Debug:{|Release:
        void Goo(){|Debug:
        {
        }|}
        
        void Goo2(){|Debug:
        {
        }|}
|}|}
#else
{|Release:{|Debug:
        void Bar(){|Release:
        {
        }|}
|}|}
#endif
    }|]
}|]";
        MarkupTestFile.GetSpans(input, out var text, out IDictionary<string, ImmutableArray<TextSpan>> spans);
        await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);

        await VerifySpansInConfigurationAsync(spans, "Release", HangMitigatingCancellationToken);
        await VerifySpansInConfigurationAsync(spans, "Debug", HangMitigatingCancellationToken);
    }

    private async Task VerifySpansInConfigurationAsync(IDictionary<string, ImmutableArray<TextSpan>> spans, string configuration, CancellationToken cancellationToken)
    {
        await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.SolutionCfg, configuration, cancellationToken);

        var expectedSpans = spans[""].Concat(spans[configuration]).OrderBy(s => s.Start);
        var actualSpansWithState = await TestServices.Editor.GetOutliningSpansAsync(cancellationToken);
        var actualSpans = actualSpansWithState.Select(span => span.Span);
        Assert.Equal(expectedSpans, actualSpans);
    }
}
