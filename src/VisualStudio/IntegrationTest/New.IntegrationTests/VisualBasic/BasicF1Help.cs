// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.F1Help)]
public class BasicF1Help : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicF1Help()
        : base(nameof(BasicF1Help))
    {
    }

    [IdeFact]
    public async Task F1Help()
    {
        await SetUpEditorAsync("""

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program$$
                Sub Main(args As String())
                    Dim query = From arg In args
                                Select args.Any(Function(a) a.Length > 5)
                    Dim x = 0
                    x += 1
                End Sub
                Public Function F() As Object
                    Return Nothing
                End Function
            End Module
            """, HangMitigatingCancellationToken);
        await VerifyAsync("Linq", "System.Linq", HangMitigatingCancellationToken);
        await VerifyAsync("String", "vb.String", HangMitigatingCancellationToken);
        await VerifyAsync("Any", "System.Linq.Enumerable.Any", HangMitigatingCancellationToken);
        await VerifyAsync("From", "vb.QueryFrom", HangMitigatingCancellationToken);
        await VerifyAsync("+=", "vb.+=", HangMitigatingCancellationToken);
        await VerifyAsync("Nothing", "vb.Nothing", HangMitigatingCancellationToken);

    }

    private async Task VerifyAsync(string word, string expectedKeyword, CancellationToken cancellationToken)
    {
        await TestServices.Editor.PlaceCaretAsync(word, charsOffset: -1, cancellationToken);
        Assert.Contains(expectedKeyword, await TestServices.Editor.GetF1KeywordsAsync(cancellationToken));
    }
}
