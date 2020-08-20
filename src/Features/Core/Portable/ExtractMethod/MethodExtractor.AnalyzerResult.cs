// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected class AnalyzerResult
        {
            private readonly IList<ITypeParameterSymbol> _typeParametersInDeclaration;
            private readonly IList<ITypeParameterSymbol> _typeParametersInConstraintList;
            private readonly ImmutableArray<VariableInfo> _variables;
            private readonly VariableInfo _variableToUseAsReturnValue;

            public AnalyzerResult(
                SemanticDocument document,
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
                var semanticModel = document.SemanticModel;

                UseInstanceMember = instanceMemberIsUsed;
                ShouldBeReadOnly = shouldBeReadOnly;
                EndOfSelectionReachable = endOfSelectionReachable;
                AwaitTaskReturn = awaitTaskReturn;
                SemanticDocument = document;
                _typeParametersInDeclaration = typeParametersInDeclaration.Select(s => semanticModel.ResolveType(s)).ToList();
                _typeParametersInConstraintList = typeParametersInConstraintList.Select(s => semanticModel.ResolveType(s)).ToList();
                _variables = variables;
                ReturnType = semanticModel.ResolveType(returnType);
                _variableToUseAsReturnValue = variableToUseAsReturnValue;
                Status = status;
            }

            public AnalyzerResult With(SemanticDocument document)
            {
                if (SemanticDocument == document)
                {
                    return this;
                }

                return new AnalyzerResult(
                    document,
                    _typeParametersInDeclaration,
                    _typeParametersInConstraintList,
                    _variables,
                    _variableToUseAsReturnValue,
                    ReturnType,
                    AwaitTaskReturn,
                    UseInstanceMember,
                    ShouldBeReadOnly,
                    EndOfSelectionReachable,
                    Status);
            }

            /// <summary>
            /// used to determine whether static can be used
            /// </summary>
            public bool UseInstanceMember { get; }

            /// <summary>
            /// Indicates whether the extracted method should have a 'readonly' modifier.
            /// </summary>
            public bool ShouldBeReadOnly { get; }

            /// <summary>
            /// used to determine whether "return" statement needs to be inserted
            /// </summary>
            public bool EndOfSelectionReachable { get; }

            /// <summary>
            /// document this result is based on
            /// </summary>
            public SemanticDocument SemanticDocument { get; }

            /// <summary>
            /// flag to show whether task return type is due to await
            /// </summary>
            public bool AwaitTaskReturn { get; }

            /// <summary>
            /// return type
            /// </summary>
            public ITypeSymbol ReturnType { get; }

            /// <summary>
            /// analyzer result operation status
            /// </summary>
            public OperationStatus Status { get; }

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
                    v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.SplitIn ||
                         v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn);
            }

            public IEnumerable<VariableInfo> GetVariablesToMoveIntoMethodDefinition(CancellationToken cancellationToken)
                => _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn);

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSite(CancellationToken cancellationToken)
                => _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut);

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSiteOrDelete(CancellationToken cancellationToken)
            {
                return _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut ||
                                                 v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.Delete);
            }

            public IEnumerable<VariableInfo> GetVariablesToSplitOrMoveOutToCallSite(CancellationToken cancellationToken)
            {
                return _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.SplitOut ||
                                                 v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut);
            }
        }
    }
}
