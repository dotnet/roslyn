// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ConvertTypeOfToNameOf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertTypeOfToNameOf), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpConvertTypeOfToNameOfCodeFixProvider() : AbstractConvertTypeOfToNameOfCodeFixProvider<
    MemberAccessExpressionSyntax>
{
    protected override string GetCodeFixTitle()
        => CSharpCodeFixesResources.Convert_typeof_to_nameof;

    protected override SyntaxNode GetSymbolTypeExpression(SemanticModel model, MemberAccessExpressionSyntax node, CancellationToken cancellationToken)
    {
        // The corresponding analyzer (CSharpConvertTypeOfToNameOfDiagnosticAnalyzer) validated the syntax
        var typeOfExpression = (TypeOfExpressionSyntax)node.Expression;
        var typeSymbol = model.GetSymbolInfo(typeOfExpression.Type, cancellationToken).Symbol.GetSymbolType();
        Contract.ThrowIfNull(typeSymbol);
        return typeSymbol.GenerateExpressionSyntax(nameSyntax: true);
    }
}
