// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Operations
{
    public static class IBinaryOperationExtensions
    {
        /// <summary>
        /// Returns true if this is a bitwise-or (<c>|</c>) of two values, at least one of which is signed and 
        /// will be widened in the process.  For example <c>someLong | someInt</c>.  This sort of operation
        /// can lead to surprising results when negative values are involved, as sign extension of a negative integer
        /// value fills the extended bits all with <c>1</c>, and thus will overwrite all the high order bits as
        /// well in the result.  While legal, it is recommended that code explicitly cast the smaller value to
        /// clearly indicate the extending approach desired (either sign extended or not).
        /// </summary>
        public static bool IsBitwiseOrOfSignExtendedOperand(this IBinaryOperation binaryOperation, CancellationToken cancellationToken = default)
        {
            if (binaryOperation == null)
                throw new ArgumentNullException(nameof(binaryOperation));

            if (binaryOperation.SemanticModel == null)
                throw new ArgumentException(CSharpResources.Operation_only_valid_on_an_IOperation_with_a_non_null_SemanticModel);

            if (binaryOperation.SemanticModel is not CSharpSemanticModel semanticModel)
                throw new ArgumentException(string.Format(CSharpResources.WrongSemanticModelType, LanguageNames.CSharp));

            return semanticModel.IsBitwiseOrOfSignExtendedOperand((CSharpSyntaxNode)binaryOperation.Syntax, cancellationToken);
        }
    }

    public static class ICompoundAssignmentOperationExtensions
    {
        /// <summary>
        /// Returns true if this is a bitwise-or-assignment (<c>|=</c>) of two values, at least one of which is signed and 
        /// will be widened in the process.  For example <c>someLong |= someInt</c>.  This sort of operation
        /// can lead to surprising results when negative values are involved, as sign extension of a negative integer
        /// value fills the extended bits all with <c>1</c>, and thus will overwrite all the high order bits as
        /// well in the result.  While legal, it is recommended that code explicitly cast the smaller value to
        /// clearly indicate the extending approach desired (either sign extended or not).
        /// </summary>
        public static bool IsBitwiseOrOfSignExtendedOperand(this ICompoundAssignmentOperation assignmentOperation, CancellationToken cancellationToken = default)
        {
            if (assignmentOperation == null)
                throw new ArgumentNullException(nameof(assignmentOperation));

            if (assignmentOperation.SemanticModel == null)
                throw new ArgumentException(CSharpResources.Operation_only_valid_on_an_IOperation_with_a_non_null_SemanticModel);

            if (assignmentOperation.SemanticModel is not CSharpSemanticModel semanticModel)
                throw new ArgumentException(string.Format(CSharpResources.WrongSemanticModelType, LanguageNames.CSharp));

            return semanticModel.IsBitwiseOrOfSignExtendedOperand((CSharpSyntaxNode)assignmentOperation.Syntax, cancellationToken);
        }
    }
}
