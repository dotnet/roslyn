// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddFileBanner;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CSharp.AddFileBanner;

[ExportCodeRefactoringProvider(LanguageNames.CSharp,
    Name = PredefinedCodeRefactoringProviderNames.AddFileBanner), Shared]
internal class CSharpAddFileBannerCodeRefactoringProvider : AbstractAddFileBannerCodeRefactoringProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpAddFileBannerCodeRefactoringProvider()
    {
    }

    protected override bool IsCommentStartCharacter(char ch)
        => ch == '/';

    protected override SyntaxTrivia CreateTrivia(SyntaxTrivia trivia, string text)
    {
        switch (trivia.Kind())
        {
            case SyntaxKind.SingleLineCommentTrivia:
            case SyntaxKind.MultiLineCommentTrivia:
            case SyntaxKind.SingleLineDocumentationCommentTrivia:
            case SyntaxKind.MultiLineDocumentationCommentTrivia:
                return SyntaxFactory.Comment(text);
        }

        return trivia;
    }
}
