// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SolutionCrawler;

[ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpDocumentDifferenceService() : AbstractDocumentDifferenceService
{
    protected override bool IsContainedInMemberBody(SyntaxNode node, TextSpan span)
    {
        switch (node)
        {
            case ConstructorDeclarationSyntax constructor:
                return (constructor.Body != null && GetBlockBodySpan(constructor.Body).Contains(span)) ||
                       (constructor.Initializer != null && constructor.Initializer.Span.Contains(span));
            case BaseMethodDeclarationSyntax method:
                return method.Body != null && GetBlockBodySpan(method.Body).Contains(span);
            case BasePropertyDeclarationSyntax property:
                return property.AccessorList != null && property.AccessorList.Span.Contains(span);
            case EnumMemberDeclarationSyntax @enum:
                return @enum.EqualsValue != null && @enum.EqualsValue.Span.Contains(span);
            case BaseFieldDeclarationSyntax field:
                return field.Declaration != null && field.Declaration.Span.Contains(span);
        }

        return false;
    }

    private static TextSpan GetBlockBodySpan(BlockSyntax body)
        => TextSpan.FromBounds(body.OpenBraceToken.Span.End, body.CloseBraceToken.SpanStart);
}
