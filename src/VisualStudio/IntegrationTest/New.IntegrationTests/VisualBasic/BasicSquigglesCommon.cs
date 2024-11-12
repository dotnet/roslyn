// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public abstract class BasicSquigglesCommon : AbstractEditorTest
{
    protected BasicSquigglesCommon(string projectTemplate)
        : base(nameof(BasicSquigglesCommon), projectTemplate)
    {
    }

    protected override string LanguageName => LanguageNames.VisualBasic;

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/72428"), WorkItem("https://github.com/dotnet/roslyn-project-system/issues/1825")]
    public async Task VerifySyntaxErrorSquiggles()
    {
        await TestServices.Editor.SetTextAsync("""
            Class A
                  Shared Sub S()
                    Dim x = 1 +
                  End Sub
            End Class
            """, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.ErrorTagsAsync(
            [("syntax error", new TextSpan(48, 0), "", "BC30201: Expression expected.")],
            HangMitigatingCancellationToken);
    }

    [WorkItem("https://github.com/dotnet/roslyn-project-system/issues/1825")]
    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/61367")]
    public async Task VerifySemanticErrorSquiggles()
    {
        await TestServices.Editor.SetTextAsync("""
            Class A
                  Shared Sub S(b as Bar)
                    Console.WriteLine(b)
                  End Sub
            End Class
            """, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.ErrorTagsAsync(
            [("syntax error", TextSpan.FromBounds(33, 36), "Bar", "BC30002: Type 'Bar' is not defined.")],
            HangMitigatingCancellationToken);
    }
}
