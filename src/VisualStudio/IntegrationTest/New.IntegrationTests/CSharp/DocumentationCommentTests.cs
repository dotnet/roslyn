// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.DocumentationComments)]
    public class DocumentationCommentTests : AbstractEditorTest
    {
        public DocumentationCommentTests()
            : base(nameof(DocumentationCommentTests))
        {

        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact]
        [WorkItem(54391, "https://github.com/dotnet/roslyn/issues/54391")]
        public async Task TypingCharacter_MultiCaret()
        {
            var code =
@"
//{|selection:|}
class C1 { }

//{|selection:|}
class C2 { }

//{|selection:|}
class C3 { }
";
            await SetUpEditorAsync(code, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync('/');
            var expected =
@"
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
";

            await TestServices.EditorVerifier.TextContainsAsync(expected, assertCaretPosition: true, cancellationToken: HangMitigatingCancellationToken);
        }
    }
}
