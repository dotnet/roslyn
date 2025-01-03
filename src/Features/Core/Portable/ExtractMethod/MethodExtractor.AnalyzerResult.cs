// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract partial class MethodExtractor
    {
        protected sealed class AnalyzerResult(
            ImmutableArray<ITypeParameterSymbol> typeParametersInDeclaration,
            ImmutableArray<ITypeParameterSymbol> typeParametersInConstraintList,
            ImmutableArray<VariableInfo> variables,
            ImmutableArray<VariableInfo> variablesToUseAsReturnValue,
            ITypeSymbol returnType,
            bool returnsByRef,
            bool awaitTaskReturn,
            bool instanceMemberIsUsed,
            bool shouldBeReadOnly,
            bool endOfSelectionReachable,
            OperationStatus status)
        {
            public ImmutableArray<ITypeParameterSymbol> MethodTypeParametersInDeclaration { get; } = typeParametersInDeclaration;
            public ImmutableArray<ITypeParameterSymbol> MethodTypeParametersInConstraintList { get; } = typeParametersInConstraintList;
            public ImmutableArray<VariableInfo> VariablesToUseAsReturnValue { get; } = variablesToUseAsReturnValue;

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

            /// <summary>
            /// flag to show whether task return type is due to await
            /// </summary>
            public bool AwaitTaskReturn { get; } = awaitTaskReturn;

            public ITypeSymbol ReturnType { get; } = returnType;
            public bool ReturnsByRef { get; } = returnsByRef;

            /// <summary>
            /// analyzer result operation status
            /// </summary>
            public OperationStatus Status { get; } = status;

            public ImmutableArray<VariableInfo> Variables { get; } = variables;

            public bool HasReturnType
            {
                get
                {
                    return ReturnType.SpecialType != SpecialType.System_Void && !AwaitTaskReturn;
                }
            }

            public ImmutableArray<VariableInfo> GetVariablesToSplitOrMoveIntoMethodDefinition()
            {
                return Variables.WhereAsArray(
                    v => v.GetDeclarationBehavior() is DeclarationBehavior.SplitIn or DeclarationBehavior.MoveIn);
            }

            public IEnumerable<VariableInfo> MethodParameters
                => Variables.Where(v => v.UseAsParameter);

            public IEnumerable<VariableInfo> GetVariablesToMoveIntoMethodDefinition()
                => Variables.Where(v => v.GetDeclarationBehavior() == DeclarationBehavior.MoveIn);

            public IEnumerable<VariableInfo> GetVariablesToMoveOutToCallSiteOrDelete()
                => Variables.Where(v => v.GetDeclarationBehavior() is DeclarationBehavior.MoveOut);

            public IEnumerable<VariableInfo> GetVariablesToSplitOrMoveOutToCallSite()
                => Variables.Where(v => v.GetDeclarationBehavior() is DeclarationBehavior.SplitOut or DeclarationBehavior.MoveOut);

            public VariableInfo GetOutermostVariableToMoveIntoMethodDefinition()
            {
                using var _ = ArrayBuilder<VariableInfo>.GetInstance(out var variables);
                variables.AddRange(this.GetVariablesToMoveIntoMethodDefinition());
                if (variables.Count <= 0)
                    return null;

                return variables.Min();
            }
        }
    }
}
