// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal static class CastSimplifier2
    {
        private static readonly SyntaxAnnotation s_annotation = new();

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

            // So far, this looks potentially possible to remove.  Now, actually do the removal and get the
            // semantic model for the rewritten code so we can check it to make sure semantics were preserved.
            var rewrittenSemanticModel = GetSemanticModelWithCastRemoved(
                castNode, castedExpressionNode, originalSemanticModel, cancellationToken);

            var (rewrittenConvertedType, rewrittenConversion) = GetRewrittenInfo(castNode, originalSemanticModel, rewrittenSemanticModel, cancellationToken);
            if (rewrittenConvertedType == null || rewrittenConvertedType.TypeKind == TypeKind.Error)
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

            // In code like `((X)y).Z()` the cast to (X) can be removed if the same 'Z' method would be called.
            // The rules here can be subtle.  For example, if Z is virtual, and (X) is a cast up the inheritance
            // hierarchy then this is *normally* ok.  HOwever, the language resolve default parameter values 
            // from the overridden method.  So if they differ, we can't actually remove the cast.
            //
            // Similarly, if (X) is a cast to an interface, and Z is an impl of that interface method, it might
            // be possible to remove, but only if y's type is sealed, as otherwise the interface method could be
            // reimplemented in a derived type.
            if (castNode.WalkUpParentheses().Parent is MemberAccessExpressionSyntax memberAccessExpression)
            {
                if (IsComplimentaryMemberAfterCastRemoval(
                        memberAccessExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken))
                {
                    return true;
                }
            }

            // If the types of the expressions are different, then removing the conversion changed semantics
            // and we can't remove it.
            if (!SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(originalConvertedType, rewrittenConvertedType))
                return false;

            return true;
        }

        private static bool IsComplimentaryMemberAfterCastRemoval(
            MemberAccessExpressionSyntax memberAccessExpression,
            SemanticModel originalSemanticModel,
            SemanticModel rewrittenSemanticModel,
            CancellationToken cancellationToken)
        {
            var originalMemberSymbol = originalSemanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol;
            if (originalMemberSymbol == null)
                return false;

            var rewrittenSyntaxTree = rewrittenSemanticModel.SyntaxTree;
            var rewrittenRoot = rewrittenSyntaxTree.GetRoot(cancellationToken);

            var rewrittenExpression = (ExpressionSyntax)rewrittenRoot.GetAnnotatedNodes(s_annotation).Single();
            var rewrittenMemberAccessExpression = (MemberAccessExpressionSyntax)rewrittenExpression.WalkUpParentheses().GetRequiredParent();
            var rewrittenMemberSymbol = rewrittenSemanticModel.GetSymbolInfo(rewrittenMemberAccessExpression, cancellationToken).Symbol;
            if (rewrittenMemberSymbol == null)
                return false;

            // Ok, we had two good member symbols before/after the cast removal.  In other words we have:
            //
            //      ((X)expr).Y
            //      (expr).Y

            // Map the original member that was called over to the new compilation so we can do proper symbol
            // checks against it.
            var originalMemberSymbolInRewrittenCompilation = originalMemberSymbol.GetSymbolKey(cancellationToken).Resolve(
                rewrittenSemanticModel.Compilation, cancellationToken: cancellationToken).Symbol;
            if (originalMemberSymbolInRewrittenCompilation == null)
                return false;

            originalMemberSymbol = originalMemberSymbolInRewrittenCompilation;

            // Next, see if this is a call to an interface method.
            if (originalMemberSymbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                var rewrittenType = rewrittenSemanticModel.GetTypeInfo(rewrittenExpression, cancellationToken).Type;
                if (rewrittenType is null or IErrorTypeSymbol)
                    return false;

                // If we don't have a reference type, then we can't remove the cast.  It would box the 
                // value and the semantics could change (operating on a copy instead of the original).
                //
                // Note: intrinsics and enums are also safe as we know they don't have state and thus
                // will have the same semantics whether or not they're boxed.
                if (!rewrittenType.IsReferenceType && !rewrittenType.IsIntrinsicType() && !rewrittenType.IsEnumType())
                    return false;

                // if we are still calling through to the same interface method, then this is safe to call.
                if (originalMemberSymbol.Equals(rewrittenMemberSymbol))
                    return true;

                // Ok, we have a type casted to an interface.  It may be safe to remove this interface cast
                // if we still call into the implementation of that interface member afterwards.  Note: the
                // type has to be sealed, otherwise the interface method may have been reimplemented lower
                // in the inheritance hierarchy.

                var isSealed = rewrittenType.IsSealed || rewrittenType.TypeKind == TypeKind.Array;
                if (!isSealed)
                    return false;

                // Then look for the current implementation of that interface member.
                var rewrittenContainingType = rewrittenMemberSymbol.ContainingType;
                var implementationMember = rewrittenContainingType.FindImplementationForInterfaceMember(originalMemberSymbolInRewrittenCompilation);
                if (implementationMember == null)
                    return false;

                // if that's not the method we're currently calling, then this definitely isn't safe to remove.
                return implementationMember.Equals(rewrittenMemberSymbol) &&
                    ParameterNamesAndDefaultValuesMatch(originalMemberSymbolInRewrittenCompilation, rewrittenMemberSymbol);
            }

            // Second, check if this is a virtual call to a different location in the inheritance hierarchy.
            for (var current = rewrittenMemberSymbol; current != null; current = current.GetOverriddenMember())
            {
                if (SymbolEquivalenceComparer.Instance.Equals(
                        originalMemberSymbolInRewrittenCompilation, rewrittenMemberSymbol))
                {
                    // we're calling into a override of a higher up virtual in the original code.
                    // This is safe as long as the names of the parameters and all default values
                    // are the same.  This is because the compiler uses the names and default
                    // values of the overridden member, even though it emits a virtual call to the
                    // the highest in the inheritance chain.
                    return ParameterNamesAndDefaultValuesMatch(originalMemberSymbolInRewrittenCompilation, rewrittenMemberSymbol);
                }
            }

            return false;
        }

        private static bool ParameterNamesAndDefaultValuesMatch(ISymbol originalMemberSymbol, ISymbol rewrittenMemberSymbol)
        {
            if (originalMemberSymbol is IMethodSymbol originalMethodSymbol &&
                rewrittenMemberSymbol is IMethodSymbol rewrittenMethodSymbol)
            {
                var originalParameters = originalMethodSymbol.Parameters;
                var rewrittenParameters = rewrittenMethodSymbol.Parameters;
                if (originalParameters.Length != rewrittenParameters.Length)
                    return false;

                for (var i = 0; i < originalParameters.Length; i++)
                {
                    var originalParameter = originalParameters[i];
                    var rewrittenParameter = rewrittenParameters[i];
                    if (originalParameter.Name != rewrittenParameter.Name)
                        return false;

                    if (originalParameter.HasExplicitDefaultValue &&
                        rewrittenParameter.HasExplicitDefaultValue &&
                        !Equals(originalParameter.ExplicitDefaultValue, rewrittenParameter.ExplicitDefaultValue))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static (ITypeSymbol? rewrittenConvertedType, Conversion rewrittenConversion) GetRewrittenInfo(
            ExpressionSyntax castNode, SemanticModel originalSemanticModel, SemanticModel rewrittenSemanticModel, CancellationToken cancellationToken)
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

            var rewrittenSyntaxTree = rewrittenSemanticModel.SyntaxTree;
            var rewrittenRoot = rewrittenSyntaxTree.GetRoot(cancellationToken);

            var rewrittenExpression = rewrittenRoot.GetAnnotatedNodes(s_annotation).Single();
            var rewrittenConvertedType = rewrittenSemanticModel.GetTypeInfo(rewrittenExpression, cancellationToken).ConvertedType;
            var rewrittenConversion = rewrittenSemanticModel.GetConversion(rewrittenExpression, cancellationToken);

            return (rewrittenConvertedType, rewrittenConversion);
        }

        private static SemanticModel GetSemanticModelWithCastRemoved(
            ExpressionSyntax castNode,
            ExpressionSyntax castedExpressionNode,
            SemanticModel originalSemanticModel,
            CancellationToken cancellationToken)
        {
            var originalSyntaxTree = originalSemanticModel.SyntaxTree;
            var originalRoot = originalSyntaxTree.GetRoot(cancellationToken);
            var originalCompilation = originalSemanticModel.Compilation;

            var rewrittenSyntaxTree = originalSyntaxTree.WithRootAndOptions(
                originalRoot.ReplaceNode(castNode, castedExpressionNode.WithAdditionalAnnotations(s_annotation)), originalSyntaxTree.Options);
            var rewrittenCompilation = originalCompilation.ReplaceSyntaxTree(originalSyntaxTree, rewrittenSyntaxTree);
            var rewrittenSemanticModel = rewrittenCompilation.GetSemanticModel(rewrittenSyntaxTree);
            return rewrittenSemanticModel;
        }
    }
}
