﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor : MethodExtractor
    {
        private class CSharpAnalyzer : Analyzer
        {
            private static readonly HashSet<int> s_nonNoisySyntaxKindSet = new HashSet<int>(new int[] { (int)SyntaxKind.WhitespaceTrivia, (int)SyntaxKind.EndOfLineTrivia });

            public static Task<AnalyzerResult> AnalyzeAsync(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
            {
                var analyzer = new CSharpAnalyzer(selectionResult, localFunction, cancellationToken);
                return analyzer.AnalyzeAsync();
            }

            public CSharpAnalyzer(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
                : base(selectionResult, localFunction, cancellationToken)
            {
            }

            protected override VariableInfo CreateFromSymbol(
                Compilation compilation,
                ISymbol symbol,
                ITypeSymbol type,
                VariableStyle style,
                bool variableDeclared)
            {
                return CreateFromSymbolCommon<LocalDeclarationStatementSyntax>(compilation, symbol, type, style, s_nonNoisySyntaxKindSet);
            }

            protected override int GetIndexOfVariableInfoToUseAsReturnValue(IList<VariableInfo> variableInfo)
            {
                var numberOfOutParameters = 0;
                var numberOfRefParameters = 0;

                var outSymbolIndex = -1;
                var refSymbolIndex = -1;

                for (var i = 0; i < variableInfo.Count; i++)
                {
                    var variable = variableInfo[i];

                    // there should be no-one set as return value yet
                    Contract.ThrowIfTrue(variable.UseAsReturnValue);

                    if (!variable.CanBeUsedAsReturnValue)
                    {
                        continue;
                    }

                    // check modifier
                    if (variable.ParameterModifier == ParameterBehavior.Ref)
                    {
                        numberOfRefParameters++;
                        refSymbolIndex = i;
                    }
                    else if (variable.ParameterModifier == ParameterBehavior.Out)
                    {
                        numberOfOutParameters++;
                        outSymbolIndex = i;
                    }
                }

                // if there is only one "out" or "ref", that will be converted to return statement.
                if (numberOfOutParameters == 1)
                {
                    return outSymbolIndex;
                }

                if (numberOfRefParameters == 1)
                {
                    return refSymbolIndex;
                }

                return -1;
            }

            protected override ITypeSymbol GetRangeVariableType(SemanticModel model, IRangeVariableSymbol symbol)
            {
                var info = model.GetSpeculativeTypeInfo(this.SelectionResult.FinalSpan.Start, SyntaxFactory.ParseName(symbol.Name), SpeculativeBindingOption.BindAsExpression);
                if (Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions.IsErrorType(info.Type))
                {
                    return null;
                }

                return info.Type == null || info.Type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object
                    ? info.Type
                    : info.ConvertedType;
            }

            protected override Tuple<SyntaxNode, SyntaxNode> GetFlowAnalysisNodeRange()
            {
                var csharpSelectionResult = this.SelectionResult as CSharpSelectionResult;

                var first = csharpSelectionResult.GetFirstStatement();
                var last = csharpSelectionResult.GetLastStatement();

                // single statement case
                if (first == last ||
                    first.Span.Contains(last.Span))
                {
                    return new Tuple<SyntaxNode, SyntaxNode>(first, first);
                }

                // multiple statement case
                var firstUnderContainer = csharpSelectionResult.GetFirstStatementUnderContainer();
                var lastUnderContainer = csharpSelectionResult.GetLastStatementUnderContainer();
                return new Tuple<SyntaxNode, SyntaxNode>(firstUnderContainer, lastUnderContainer);
            }

            protected override bool ContainsReturnStatementInSelectedCode(IEnumerable<SyntaxNode> jumpOutOfRegionStatements)
            {
                return jumpOutOfRegionStatements.Where(n => n is ReturnStatementSyntax).Any();
            }

            protected override bool ReadOnlyFieldAllowed()
            {
                var scope = this.SelectionResult.GetContainingScopeOf<ConstructorDeclarationSyntax>();
                return scope == null;
            }

            protected override ITypeSymbol GetSymbolType(SemanticModel semanticModel, ISymbol symbol)
            {
                var selectionOperation = semanticModel.GetOperation(this.SelectionResult.GetContainingScope());

                switch (symbol)
                {
                    case ILocalSymbol localSymbol when localSymbol.NullableAnnotation == NullableAnnotation.Annotated:
                    case IParameterSymbol parameterSymbol when parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated:

                        // For local symbols and parameters, we can check what the flow state 
                        // for refences to the symbols are and determine if we can change 
                        // the nullability to a less permissive state.
                        var references = selectionOperation.DescendantsAndSelf()
                            .Where(IsSymbolReferencedByOperation);

                        if (AreAllReferencesNotNull(references))
                        {
                            return base.GetSymbolType(semanticModel, symbol).WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                        }

                        return base.GetSymbolType(semanticModel, symbol);

                    default:
                        return base.GetSymbolType(semanticModel, symbol);
                }

                bool AreAllReferencesNotNull(IEnumerable<IOperation> references)
                => references.All(r => semanticModel.GetTypeInfo(r.Syntax).Nullability.FlowState == NullableFlowState.NotNull);

                bool IsSymbolReferencedByOperation(IOperation operation)
                    => operation switch
                    {
                        ILocalReferenceOperation localReference => localReference.Local.Equals(symbol),
                        IParameterReferenceOperation parameterReference => parameterReference.Parameter.Equals(symbol),
                        IAssignmentOperation assignment => IsSymbolReferencedByOperation(assignment.Target),
                        _ => false
                    };
            }
        }
    }
}
