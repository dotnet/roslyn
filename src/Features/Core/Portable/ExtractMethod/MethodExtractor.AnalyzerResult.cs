// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected sealed class AnalyzerResult(
            IEnumerable<ITypeParameterSymbol> typeParametersInDeclaration,
            IEnumerable<ITypeParameterSymbol> typeParametersInConstraintList,
            ImmutableArray<VariableInfo> variables,
            VariableInfo variableToUseAsReturnValue,
            ITypeSymbol returnType,
            bool awaitTaskReturn,
            bool instanceMemberIsUsed,
            bool shouldBeReadOnly,
            bool endOfSelectionReachable,
            OperationStatus status)
        {
            private readonly IList<ITypeParameterSymbol> _typeParametersInDeclaration = typeParametersInDeclaration.ToList();
            private readonly IList<ITypeParameterSymbol> _typeParametersInConstraintList = typeParametersInConstraintList.ToList();
            private readonly ImmutableArray<VariableInfo> _variables = variables;
            private readonly VariableInfo _variableToUseAsReturnValue = variableToUseAsReturnValue;

            //public AnalyzerResult With(SemanticDocument document)
            //{
            //    if (SemanticDocument == document)
            //    {
            //        return this;
            //    }

            //    return new AnalyzerResult(
            //        document,
            //        _typeParametersInDeclaration,
            //        _typeParametersInConstraintList,
            //        _variables,
            //        _variableToUseAsReturnValue,
            //        ReturnType,
            //        AwaitTaskReturn,
            //        UseInstanceMember,
            //        ShouldBeReadOnly,
            //        EndOfSelectionReachable,
            //        Status);
            //}

            /// <summary>
            /// used to determine whether static can be used
            /// </summary>
            public bool UseInstanceMember { get; } = instanceMemberIsUsed;

            /// <summary>
            /// Indicates whether the extracted method should have a 'readonly' modifier.
            /// </summary>
            public bool ShouldBeReadOnly { get; } = shouldBeReadOnly;

            /// <summary>
            /// used to determine whether "return" statement needs to be inserted
            /// </summary>
            public bool EndOfSelectionReachable { get; } = endOfSelectionReachable;

            ///// <summary>
            ///// document this result is based on
            ///// </summary>
            //public SemanticDocument SemanticDocument { get; } = document;

            /// <summary>
            /// flag to show whether task return type is due to await
            /// </summary>
            public bool AwaitTaskReturn { get; } = awaitTaskReturn;

            /// <summary>
            /// return type
            /// </summary>
            public ITypeSymbol ReturnType { get; } = returnType;

            /// <summary>
            /// analyzer result operation status
            /// </summary>
            public OperationStatus Status { get; } = status;

            public ImmutableArray<VariableInfo> Variables => _variables;

            public ReadOnlyCollection<ITypeParameterSymbol> MethodTypeParametersInDeclaration
            {
                get
                {
                    return new ReadOnlyCollection<ITypeParameterSymbol>(_typeParametersInDeclaration);
                }
            }

            public ReadOnlyCollection<ITypeParameterSymbol> MethodTypeParametersInConstraintList
            {
                get
                {
                    return new ReadOnlyCollection<ITypeParameterSymbol>(_typeParametersInConstraintList);
                }
            }

            public bool HasVariableToUseAsReturnValue
            {
                get
                {
                    return _variableToUseAsReturnValue != null;
                }
            }

            public VariableInfo VariableToUseAsReturnValue
            {
                get
                {
                    Contract.ThrowIfNull(_variableToUseAsReturnValue);
                    return _variableToUseAsReturnValue;
                }
            }

            public bool HasReturnType
            {
                get
                {
                    return ReturnType.SpecialType != SpecialType.System_Void && !AwaitTaskReturn;
                }
            }

            public IEnumerable<VariableInfo> MethodParameters
            {
                get
                {
                    return _variables.Where(v => v.UseAsParameter);
                }
            }

            public ImmutableArray<VariableInfo> GetVariablesToSplitOrMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                return _variables.WhereAsArray(
                    v => v.GetDeclarationBehavior(cancellationToken) is DeclarationBehavior.SplitIn or
                         DeclarationBehavior.MoveIn);
            }

            public IEnumerable<VariableInfo> GetVariablesToMoveIntoMethodDefinition(CancellationToken cancellationToken)
                => _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn);

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSite(CancellationToken cancellationToken)
                => _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut);

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSiteOrDelete(CancellationToken cancellationToken)
            {
                return _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) is DeclarationBehavior.MoveOut or
                                                 DeclarationBehavior.Delete);
            }

            public IEnumerable<VariableInfo> GetVariablesToSplitOrMoveOutToCallSite(CancellationToken cancellationToken)
            {
                return _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) is DeclarationBehavior.SplitOut or
                                                 DeclarationBehavior.MoveOut);
            }

            public VariableInfo GetOutermostVariableToMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<VariableInfo>.GetInstance(out var variables);
                variables.AddRange(this.GetVariablesToMoveIntoMethodDefinition(cancellationToken));
                if (variables.Count <= 0)
                    return null;

                VariableInfo.SortVariables(variables);
                return variables[0];
            }

            public async ValueTask<(SemanticDocument, InsertionPoint)> CreateAnnotatedDocumentAsync(
                SemanticDocument document, SyntaxNode insertionPointNode, CancellationToken cancellationToken)
            {
                var annotations = new List<Tuple<SyntaxToken, SyntaxAnnotation>>(_variables.Length);
                _variables.Do(v => v.AddIdentifierTokenAnnotationPair(annotations, cancellationToken));

                var tokenMap = annotations.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());

                var insertionPointAnnotation = new SyntaxAnnotation();
                // return new InsertionPoint(await document.WithSyntaxRootAsync(newRoot, cancellationToken).ConfigureAwait(false), annotation);

                var finalRoot = document.Root.ReplaceSyntax(
                    nodes: new[] { insertionPointNode },
                    computeReplacementNode: (o, n) => o.WithAdditionalAnnotations(insertionPointAnnotation),
                    tokens: tokenMap.Keys,
                    computeReplacementToken: (o, n) => o.WithAdditionalAnnotations(tokenMap[o]),
                    trivia: null,
                    computeReplacementTrivia: null);
                var finalDocument = await document.WithSyntaxRootAsync(finalRoot, cancellationToken).ConfigureAwait(false);
                var insertionPoint = new InsertionPoint(finalDocument, insertionPointAnnotation);
                return (finalDocument, insertionPoint);
            }
        }
    }
}
