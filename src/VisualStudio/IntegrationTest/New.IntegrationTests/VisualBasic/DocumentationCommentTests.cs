// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class DocumentationCommentTests : AbstractEditorTest
{
    public DocumentationCommentTests()
        : base(nameof(DocumentationCommentTests))
    {
    }

    protected override string LanguageName => LanguageNames.VisualBasic;

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/17383")]
    public async Task Paste_MultilineText()
    {
        await SetUpEditorAsync("""

            Public Class C
                ''' <summary>
                ''' $$
                ''' </summary>
            End Class

            """.ReplaceLineEndings("\r\n"), HangMitigatingCancellationToken);

        await TestServices.Editor.PasteAsync(
            "Line 1\r\nLine 2 with A & B", HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

            Public Class C
                ''' <summary>
                ''' Line 1
                ''' Line 2 with A &amp; B$$
                ''' </summary>
            End Class

            """.ReplaceLineEndings("\r\n"), assertCaretPosition: true, cancellationToken: HangMitigatingCancellationToken);
    }
}
