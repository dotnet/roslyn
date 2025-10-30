// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class DocumentationCommentTests : AbstractEditorTest
{
    public DocumentationCommentTests()
        : base(nameof(DocumentationCommentTests))
    {

    }

    protected override string LanguageName => LanguageNames.CSharp;

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/54391")]
    public async Task TypingCharacter_MultiCaret()
    {
        await SetUpEditorAsync("""

            //{|selection:|}
            class C1 { }

            //{|selection:|}
            class C2 { }

            //{|selection:|}
            class C3 { }

            """, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync('/', HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            /// <summary>
            /// $$
            /// </summary>
            class C1 { }

            /// <summary>
            /// 
            /// </summary>
            class C2 { }

            /// <summary>
            /// 
            /// </summary>
            class C3 { }

            """, assertCaretPosition: true, cancellationToken: HangMitigatingCancellationToken);
    }
}
