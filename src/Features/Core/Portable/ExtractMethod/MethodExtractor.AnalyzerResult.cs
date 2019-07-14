// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
            private readonly IList<VariableInfo> _variables;
            private readonly VariableInfo _variableToUseAsReturnValue;

            public AnalyzerResult(
                SemanticDocument document,
                IEnumerable<ITypeParameterSymbol> typeParametersInDeclaration,
                IEnumerable<ITypeParameterSymbol> typeParametersInConstraintList,
                IList<VariableInfo> variables,
                VariableInfo variableToUseAsReturnValue,
                ITypeSymbol returnType,
                bool awaitTaskReturn,
                bool instanceMemberIsUsed,
                bool endOfSelectionReachable,
                OperationStatus status)
            {
                var semanticModel = document.SemanticModel;

                UseInstanceMember = instanceMemberIsUsed;
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
                    EndOfSelectionReachable,
                    Status);
            }

            /// <summary>
            /// used to determine whether static can be used
            /// </summary>
            public bool UseInstanceMember { get; }

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

            public IEnumerable<VariableInfo> GetVariablesToSplitOrMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                return _variables
                           .Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.SplitIn ||
                                       v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn);
            }

            public IEnumerable<VariableInfo> GetVariablesToMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                return _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn);
            }

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSite(CancellationToken cancellationToken)
            {
                return _variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut);
            }

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
