// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageService(typeof(SnippetFunctionService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSnippetFunctionService() : SnippetFunctionService
{
    public override async Task<string?> GetContainingClassNameAsync(Document document, int position, CancellationToken cancellationToken)
    {
        // Find the nearest enclosing type declaration and use its name
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var type = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken).GetAncestor<TypeDeclarationSyntax>();

        return type?.Identifier.ToString();
    }

    protected override async Task<ITypeSymbol?> GetEnumSymbolAsync(Document document, TextSpan switchExpressionSpan, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var token = syntaxTree.FindTokenOnRightOfPosition(switchExpressionSpan.Start, cancellationToken);
        var expressionNode = token.GetAncestor(n => n.Span == switchExpressionSpan);

        if (expressionNode == null)
        {
            return null;
        }

        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var typeSymbol = model.GetTypeInfo(expressionNode, cancellationToken).Type;

        return typeSymbol;
    }

    protected override async Task<(Document, TextSpan)> GetDocumentWithEnumCaseAsync(
        Document document,
        string fullyQualifiedTypeName,
        string firstEnumMemberName,
        TextSpan caseGenerationLocation,
        CancellationToken cancellationToken)
    {
        var str = "case " + fullyQualifiedTypeName + "." + firstEnumMemberName + ":" + Environment.NewLine + " break;";
        var textChange = new TextChange(caseGenerationLocation, str);
        var typeSpan = new TextSpan(caseGenerationLocation.Start + "case ".Length, fullyQualifiedTypeName.Length);

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var documentWithCaseAdded = document.WithText(text.WithChanges(textChange));

        return (documentWithCaseAdded, typeSpan);
    }

    public override string SwitchCaseFormat => @"case {0}.{1}:
 break;
";

    public override string SwitchDefaultCaseForm => @"default:
 break;";
}
