// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            SemanticModel originalSemanticModel, CancellationToken cancellationToken)
        {
            // Can't remove casts in code that has syntax errors.
            if (castNode.WalkUpParentheses().ContainsDiagnostics)
                return false;

            // Quick syntactic checks we can do before semantic work.
            var isNullLiteralCast = castedExpressionNode.WalkDownParentheses().Kind() == SyntaxKind.NullLiteralExpression;
            var isDefaultLiteralCast = castedExpressionNode.WalkDownParentheses().Kind() == SyntaxKind.DefaultLiteralExpression;

            // Language does not allow `if (x is default)` ever.  So if we have `if (x is (Y)default)`
            // then we can't remove the cast.
            if (isDefaultLiteralCast && castNode.WalkUpParentheses().Parent is PatternSyntax or CaseSwitchLabelSyntax)
                return false;

            // If we don't have a conversion then we can't do anything with this as the code isn't
            // semantically valid. 
            var originalConversionOperation = originalSemanticModel.GetOperation(castNode, cancellationToken) as IConversionOperation;
            if (originalConversionOperation == null)
                return false;

            // If the conversion doesn't exist then we can't do anything with this as the code isn't
            // semantically valid.
            var originalConversion = originalConversionOperation.GetConversion();
            if (!originalConversion.Exists)
                return false;

            // Explicit conversions are conversions that cannot be proven to always succeed, conversions
            // that are known to possibly lose information.  As such, we need to preserve this as it 
            // has necessary runtime behavior that must be kept.
            if (originalConversion.IsExplicit)
                return false;

            // A conversion must either not exist, or it must be explciit or implicit. At this point we
            // have conversions that will always succeed, but which could have impact on the code by 
            // changing the types of things (which can affect other things like overload resolution),
            // or the runtime values of code.  We only want to remove the cast if it will do none of those
            // things.
            Contract.ThrowIfFalse(originalConversion.IsImplicit);

            // we are starting with code like `(X)expr` and converting to just `expr`. Post rewrite we need
            // to ensure that the final converted-type of `expr` matches the final converted type of `(X)expr`.
            var originalConvertedType = originalSemanticModel.GetTypeInfo(castNode.WalkUpParentheses(), cancellationToken).ConvertedType;
            if (originalConvertedType == null || originalConvertedType.TypeKind == TypeKind.Error)
                return false;

            // if the expression being casted is the `null` literal, then we can't remove the cast if the final
            // converted type is a value type.  This can happen with code like: 
            //
            // void Goo<T, S>() where T : class, S
            // {
            //     S y = (T)null;
            // }
            //
            // Effectively, this constrains S to be a reference type (as T could not otherwise derive from it).
            // However, such a invariant isn't understood by the compiler.  So if the (T) cast is removed it will
            // fail as 'null' cannot be converted to an unconstrained generic type.
            if (isNullLiteralCast && !originalConvertedType.IsReferenceType && !originalConvertedType.IsNullable())
                return false;

            var (rewrittenConvertedType, rewrittenConversion) = GetRewrittenInfo(castNode, castedExpressionNode, originalSemanticModel, cancellationToken);
            if (rewrittenConvertedType == null || rewrittenConvertedType.TypeKind == TypeKind.Error)
                return false;

            // If the types of the expressions are different, then removing the conversion changed semantics
            // and we can't remove it.
            if (!SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(originalConvertedType, rewrittenConvertedType))
                return false;

            // The final converted type may be the same even after removing the cast.  However, the cast may 
            // have been necessary to convert the type and/or value in a way that could be observable.  For example:
            //
            // object o1 = (long)expr; // or (long)0
            //
            // We need to keep the cast so that the stored value stays a 'long'.
            if (originalConversion.IsConstantExpression || originalConversion.IsNumeric)
            {
                if (rewrittenConversion.IsBoxing)
                    return false;
            }

            // We have to specially handle formattable string conversions.  If we remove them, we may end up with
            // a string value instead.  For example:
            //
            // object o2 = (IFormattable)$"";
            if (originalConversion.IsInterpolatedString && !rewrittenConversion.IsInterpolatedString)
                return false;

            // If we have:
            //
            //      public static implicit operator A(string x)
            //      A x = (string)null;
            //
            // Then the original code has an implicit user defined conversion in it.  We can only remove this
            // if the new code would have the same conversion as well.
            if (originalConversionOperation.Parent is IConversionOperation { IsImplicit: true, Conversion: { IsUserDefined: true } } originalParentImplicitConversion)
            {
                if (!rewrittenConversion.IsUserDefined)
                    return false;

                if (!SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(originalParentImplicitConversion.Conversion.MethodSymbol, rewrittenConversion.MethodSymbol))
                    return false;
            }

            return true;
        }

        private static (ITypeSymbol? rewrittenConvertedType, Conversion rewrittenConversion) GetRewrittenInfo(
            ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
            SemanticModel originalSemanticModel, CancellationToken cancellationToken)
        {
            if (castNode.WalkUpParentheses().Parent is InterpolationSyntax)
            {
                // Workaround https://github.com/dotnet/roslyn/issues/56934
                // Compiler does not give a conversion inside an interpolation. However, all values in the interpolation
                // holes are converted to object.
                //
                // Note: this may need to be revisited with improved interpolated strings (as they could take
                // strongly typed args and could avoid the object boxing).
                return (originalSemanticModel.Compilation.ObjectType, default);
            }

            var originalSyntaxTree = originalSemanticModel.SyntaxTree;
            var originalRoot = originalSyntaxTree.GetRoot(cancellationToken);
            var originalCompilation = originalSemanticModel.Compilation;

            var annotation = new SyntaxAnnotation();

            var rewrittenSyntaxTree = originalSyntaxTree.WithRootAndOptions(
                originalRoot.ReplaceNode(castNode, castedExpressionNode.WithAdditionalAnnotations(annotation)), originalSyntaxTree.Options);
            var rewrittenRoot = rewrittenSyntaxTree.GetRoot(cancellationToken);
            var rewrittenCompilation = originalCompilation.ReplaceSyntaxTree(originalSyntaxTree, rewrittenSyntaxTree);
            var rewrittenSemanticModel = rewrittenCompilation.GetSemanticModel(rewrittenSyntaxTree);

            var rewrittenExpression = rewrittenRoot.GetAnnotatedNodes(annotation).Single();
            var rewrittenConvertedType = rewrittenSemanticModel.GetTypeInfo(rewrittenExpression, cancellationToken).ConvertedType;
            var rewrittenConversion = rewrittenSemanticModel.GetConversion(rewrittenExpression, cancellationToken);

            return (rewrittenConvertedType, rewrittenConversion);
        }
    }
}
