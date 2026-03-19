// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceDocCommentTextWithTag;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag), Shared]
internal sealed class CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider :
    AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider()
    {
    }

    protected override bool IsXmlTextToken(SyntaxToken token)
        => token.Kind() is SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlTextLiteralNewLineToken;

    protected override bool IsInXMLAttribute(SyntaxToken token)
        => token.GetRequiredParent().Kind() is SyntaxKind.XmlCrefAttribute or SyntaxKind.XmlNameAttribute or SyntaxKind.XmlTextAttribute;

    protected override bool IsKeyword(string text)
        => SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(text)) || SyntaxFacts.IsContextualKeyword(SyntaxFacts.GetContextualKeywordKind(text));

    protected override SyntaxNode ParseExpression(string text)
        => SyntaxFactory.ParseExpression(text);
}
