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
            private readonly IList<ITypeParameterSymbol> typeParametersInDeclaration;
            private readonly IList<ITypeParameterSymbol> typeParametersInConstraintList;
            private readonly IList<VariableInfo> variables;
            private readonly VariableInfo variableToUseAsReturnValue;

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

                this.UseInstanceMember = instanceMemberIsUsed;
                this.EndOfSelectionReachable = endOfSelectionReachable;
                this.AwaitTaskReturn = awaitTaskReturn;
                this.SemanticDocument = document;
                this.typeParametersInDeclaration = typeParametersInDeclaration.Select(s => semanticModel.ResolveType(s)).ToList();
                this.typeParametersInConstraintList = typeParametersInConstraintList.Select(s => semanticModel.ResolveType(s)).ToList();
                this.variables = variables;
                this.ReturnType = semanticModel.ResolveType(returnType);
                this.variableToUseAsReturnValue = variableToUseAsReturnValue;
                this.Status = status;
            }

            public AnalyzerResult With(SemanticDocument document)
            {
                if (this.SemanticDocument == document)
                {
                    return this;
                }

                return new AnalyzerResult(
                    document,
                    this.typeParametersInDeclaration,
                    this.typeParametersInConstraintList,
                    this.variables,
                    this.variableToUseAsReturnValue,
                    this.ReturnType,
                    this.AwaitTaskReturn,
                    this.UseInstanceMember,
                    this.EndOfSelectionReachable,
                    this.Status);
            }

            /// <summary>
            /// used to determine whether static can be used
            /// </summary>
            public bool UseInstanceMember { get; private set; }

            /// <summary>
            /// used to determine whether "return" statement needs to be inserted
            /// </summary>
            public bool EndOfSelectionReachable { get; private set; }

            /// <summary>
            /// document this result is based on
            /// </summary>
            public SemanticDocument SemanticDocument { get; private set; }

            /// <summary>
            /// flag to show whether task return type is due to await
            /// </summary>
            public bool AwaitTaskReturn { get; private set; }

            /// <summary>
            /// return type
            /// </summary>
            public ITypeSymbol ReturnType { get; private set; }

            /// <summary>
            /// analyzer result operation status
            /// </summary>
            public OperationStatus Status { get; private set; }

            public ReadOnlyCollection<ITypeParameterSymbol> MethodTypeParametersInDeclaration
            {
                get
                {
                    return new ReadOnlyCollection<ITypeParameterSymbol>(this.typeParametersInDeclaration);
                }
            }

            public ReadOnlyCollection<ITypeParameterSymbol> MethodTypeParametersInConstraintList
            {
                get
                {
                    return new ReadOnlyCollection<ITypeParameterSymbol>(this.typeParametersInConstraintList);
                }
            }

            public bool HasVariableToUseAsReturnValue
            {
                get
                {
                    return this.variableToUseAsReturnValue != null;
                }
            }

            public VariableInfo VariableToUseAsReturnValue
            {
                get
                {
                    Contract.ThrowIfNull(this.variableToUseAsReturnValue);
                    return this.variableToUseAsReturnValue;
                }
            }

            public bool HasReturnType
            {
                get
                {
                    return this.ReturnType.SpecialType != SpecialType.System_Void && !this.AwaitTaskReturn;
                }
            }

            public IEnumerable<VariableInfo> MethodParameters
            {
                get
                {
                    return this.variables.Where(v => v.UseAsParameter);
                }
            }

            public IEnumerable<VariableInfo> GetVariablesToSplitOrMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                return this.variables
                           .Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.SplitIn ||
                                       v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn);
            }

            public IEnumerable<VariableInfo> GetVariablesToMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                return this.variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn);
            }

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSite(CancellationToken cancellationToken)
            {
                return this.variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut);
            }

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSiteOrDelete(CancellationToken cancellationToken)
            {
                return this.variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut ||
                                                 v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.Delete);
            }

            public IEnumerable<VariableInfo> GetVariablesToSplitOrMoveOutToCallSite(CancellationToken cancellationToken)
            {
                return this.variables.Where(v => v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.SplitOut ||
                                                 v.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut);
            }
        }
    }
}
