// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ForEachCast;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.ForEachCast
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpForEachCastDiagnosticAnalyzer : AbstractForEachCastDiagnosticAnalyzer<
        SyntaxKind,
        CommonForEachStatementSyntax>
    {
        protected override ISyntaxFacts SyntaxFacts
            => CSharpSyntaxFacts.Instance;

        protected override ImmutableArray<SyntaxKind> GetSyntaxKinds()
            => ImmutableArray.Create(SyntaxKind.ForEachStatement, SyntaxKind.ForEachVariableStatement);

        protected override (CommonConversion conversion, ITypeSymbol? collectionElementType) GetForEachInfo(
            SemanticModel semanticModel, CommonForEachStatementSyntax node)
        {
            var info = semanticModel.GetForEachStatementInfo(node);
            return (info.ElementConversion.ToCommonConversion(), info.ElementType);
        }
    }
}
