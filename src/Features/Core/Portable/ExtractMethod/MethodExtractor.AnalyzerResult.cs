// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

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
            ITypeSymbol returnType,
            bool returnsByRef,
            bool instanceMemberIsUsed,
            bool shouldBeReadOnly,
            ExtractMethodFlowControlInformation flowControlInformation,
            OperationStatus status)
        {
            public ImmutableArray<ITypeParameterSymbol> MethodTypeParametersInDeclaration { get; } = typeParametersInDeclaration;
            public ImmutableArray<ITypeParameterSymbol> MethodTypeParametersInConstraintList { get; } = typeParametersInConstraintList;
            public ImmutableArray<VariableInfo> VariablesToUseAsReturnValue { get; } = variables.WhereAsArray(v => v.UseAsReturnValue);

            /// <summary>
            /// used to determine whether static can be used
            /// </summary>
            public bool UseInstanceMember { get; } = instanceMemberIsUsed;

            /// <summary>
            /// Indicates whether the extracted method should have a 'readonly' modifier.
            /// </summary>
            public bool ShouldBeReadOnly { get; } = shouldBeReadOnly;

            /// <summary>
            /// Information about the flow control constructs found in the selection.  For for many purposes, including
            /// determining whether a final "return" statement needs to be inserted.
            /// </summary>
            public ExtractMethodFlowControlInformation FlowControlInformation { get; } = flowControlInformation;

            /// <summary>
            /// Initial computed return type for the extract method.  This does not include any wrapping in a type like
            /// <see cref="Task{TResult}"/> for async methods.
            /// </summary>
            public ITypeSymbol CoreReturnType { get; } = returnType;
            public bool ReturnsByRef { get; } = returnsByRef;

            /// <summary>
            /// analyzer result operation status
            /// </summary>
            public OperationStatus Status { get; } = status;

            public ImmutableArray<VariableInfo> Variables { get; } = variables;

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
