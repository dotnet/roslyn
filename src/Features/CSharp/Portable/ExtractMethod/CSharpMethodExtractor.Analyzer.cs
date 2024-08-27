// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal partial class CSharpMethodExtractor
{
    private class CSharpAnalyzer(CSharpSelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken) : Analyzer(selectionResult, localFunction, cancellationToken)
    {
        private static readonly HashSet<int> s_nonNoisySyntaxKindSet = new HashSet<int>([(int)SyntaxKind.WhitespaceTrivia, (int)SyntaxKind.EndOfLineTrivia]);

        public static AnalyzerResult Analyze(CSharpSelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
        {
            var analyzer = new CSharpAnalyzer(selectionResult, localFunction, cancellationToken);
            return analyzer.Analyze();
        }

        protected override bool TreatOutAsRef => false;

        protected override VariableInfo CreateFromSymbol(
            Compilation compilation,
            ISymbol symbol,
            ITypeSymbol type,
            VariableStyle style,
            bool variableDeclared)
        {
            return CreateFromSymbolCommon<LocalDeclarationStatementSyntax>(compilation, symbol, type, style, s_nonNoisySyntaxKindSet);
        }

        protected override ITypeSymbol GetRangeVariableType(SemanticModel model, IRangeVariableSymbol symbol)
        {
            var info = model.GetSpeculativeTypeInfo(SelectionResult.FinalSpan.Start, SyntaxFactory.ParseName(symbol.Name), SpeculativeBindingOption.BindAsExpression);
            if (Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions.IsErrorType(info.Type))
            {
                return null;
            }

            return info.Type == null || info.Type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object
                ? info.Type
                : info.ConvertedType;
        }

        protected override bool ContainsReturnStatementInSelectedCode(IEnumerable<SyntaxNode> jumpOutOfRegionStatements)
            => jumpOutOfRegionStatements.Where(n => n is ReturnStatementSyntax).Any();

        protected override bool ReadOnlyFieldAllowed()
        {
            var scope = SelectionResult.GetContainingScopeOf<ConstructorDeclarationSyntax>();
            return scope == null;
        }

        protected override ITypeSymbol GetSymbolType(SemanticModel semanticModel, ISymbol symbol)
        {
            var selectionOperation = semanticModel.GetOperation(SelectionResult.GetContainingScope());

            // Check if null is possibly assigned to the symbol. If it is, leave nullable annotation as is, otherwise
            // we can modify the annotation to be NotAnnotated to code that more likely matches the user's intent.
            if (selectionOperation is not null &&
                NullableHelpers.IsSymbolAssignedPossiblyNullValue(semanticModel, selectionOperation, symbol) == false)
            {
                return base.GetSymbolType(semanticModel, symbol).WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            return base.GetSymbolType(semanticModel, symbol);
        }
    }
}
