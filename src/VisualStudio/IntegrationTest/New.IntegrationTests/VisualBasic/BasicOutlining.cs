// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public class BasicOutlining : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicOutlining()
        : base(nameof(BasicOutlining))
    {
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public async Task Outlining()
    {
        var input = @"
[|Imports System
Imports System.Text|]

[|Namespace Acme
    [|Module Module1
        [|Sub Main()
            
        End Sub|]
    End Module|]
End Namespace|]";
        MarkupTestFile.GetSpans(input, out var text, out var spans);
        await TestServices.Editor.SetTextAsync(text, HangMitigatingCancellationToken);
        var actualSpansWithState = await TestServices.Editor.GetOutliningSpansAsync(HangMitigatingCancellationToken);
        var actualSpans = actualSpansWithState.Select(span => span.Span);
        Assert.Equal(spans.OrderBy(s => s.Start), actualSpans);
    }
}
