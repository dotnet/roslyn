// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal static class CastSimplifier2
    {
        public static bool IsUnnecessaryCast(ExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => cast is CastExpressionSyntax castExpression ? IsUnnecessaryCast(castExpression, semanticModel, cancellationToken) :
               cast is BinaryExpressionSyntax binaryExpression ? IsUnnecessaryAsCast(binaryExpression, semanticModel, cancellationToken) : false;

        public static bool IsUnnecessaryCast(CastExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => IsCastSafeToRemove(cast, cast.Expression, semanticModel, cancellationToken);

        public static bool IsUnnecessaryAsCast(BinaryExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
            => cast.Kind() == SyntaxKind.AsExpression &&
               IsCastSafeToRemove(cast, cast.Left, semanticModel, cancellationToken);

        private static bool IsCastSafeToRemove(
            ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
            SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Can't remove casts in code that has syntax errors.
            if (castNode.WalkUpParentheses().ContainsDiagnostics)
                return false;

            // If we don't have a conversion then we can't do anything with this as the code isn't
            // semantically valid. 
            var conversionOperation = semanticModel.GetOperation(castNode, cancellationToken) as IConversionOperation;
            if (conversionOperation == null)
                return false;

            // If the conversion doesn't exist then we can't do anything with this as the code isn't
            // semantically valid.
            var conversion = conversionOperation.GetConversion();
            if (!conversion.Exists)
                return false;

            // Explicit conversions are conversions that cannot be proven to always succeed, conversions
            // that are known to possibly lose information.  As such, we need to preserve this as it 
            // has necessary runtime behavior that must be kept.
            if (conversion.IsExplicit)
                return false;

            // A conversion must either not exist, or it must be explciit or implicit. At this point we
            // have conversions that will always succeed, but which could have impact on the code by 
            // changing the types of things (which can affect other things like overload resolution),
            // or the runtime values of code.  We only want to remove the cast if it will do none of those
            // things.
            Contract.ThrowIfFalse(conversion.IsImplicit);

            // we are starting with code like `(X)expr` and converting to just `expr`. Post rewrite we need
            // to ensure that the final converted-type of `expr` matches the final converted type of `(X)expr`.
            var originalConvertedType = semanticModel.GetTypeInfo(castNode.WalkUpParentheses(), cancellationToken).ConvertedType;
            if (originalConvertedType == null || originalConvertedType.TypeKind == TypeKind.Error)
                return false;

            return true;
        }
    }
}
