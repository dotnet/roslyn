// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public class BasicCodeActions : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicCodeActions()
        : base(nameof(BasicCodeActions))
    {
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
    public async Task GenerateMethodInClosedFile()
    {
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Goo.vb", """

            Class Goo
            End Class

            """, cancellationToken: HangMitigatingCancellationToken);

        await SetUpEditorAsync("""

            Imports System;

            Class Program
                Sub Main(args As String())
                    Dim f as Goo = new Goo()
                    f.Bar()$$
                End Sub
            End Class

            """, HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate method 'Bar'", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(
            """

            Class Goo
                Friend Sub Bar()
                    Throw New NotImplementedException()
                End Sub
            End Class

            """,
            await TestServices.SolutionExplorer.GetFileContentsAsync(ProjectName, "Goo.vb", HangMitigatingCancellationToken));
    }
}
