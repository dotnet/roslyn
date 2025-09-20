// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic;

public class BasicGenerateFromUsage : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicGenerateFromUsage()
        : base(nameof(BasicGenerateFromUsage))
    {
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
    public async Task GenerateLocal()
    {
        await SetUpEditorAsync(
            """
            Module Program
                Sub Main(args As String())
                    Dim x As String = $$xyz
                End Sub
            End Module
            """, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate local 'xyz'", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(
            """
            Module Program
                Sub Main(args As String())
                    Dim xyz As String = Nothing
                    Dim x As String = xyz
                End Sub
            End Module
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
    public async Task GenerateTypeInNewFile()
    {
        await SetUpEditorAsync(
            """
            Module Program
                Sub Main(args As String())
                    Dim x As New $$ClassInNewFile()
                End Sub
            End Module
            """, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate class 'ClassInNewFile' in new file", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "ClassInNewFile.vb", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(
            """
            Friend Class ClassInNewFile
                Public Sub New()
                End Sub
            End Class
            """, cancellationToken: HangMitigatingCancellationToken);
    }
}
