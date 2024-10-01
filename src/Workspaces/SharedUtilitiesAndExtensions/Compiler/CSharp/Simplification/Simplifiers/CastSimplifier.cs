// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;

/// <summary>
/// By default the cast simplifier operates under several main principles:
/// <list type="number">
/// <item>The final type that a cast-expression was converted to should be the same as the final
/// type that the underlying expression should convert to without the cast.  This tells us that 
/// the compiler thinks that value should convert to that type implicitly, not just explicitly.</item>
/// <item>Static semantics of the code should remain the same.  This means that things like overload
/// resolution of the invocations the casted expression is contained within should not change.</item>
/// <item>Runtime types and values should not observably change.  This means that if casting the 
/// value would cause a different type to be seen in a <see cref="System.Object.GetType()"/> call, 
/// or a different value could be observed at runtime, then it must remain.</item>
/// </list>
/// 
/// These rules serve as a good foundational intuition about when casts should be kept and when 
/// they should be removable.  However, they are not entirely complete.  There are cases when we
/// can weaken some of the above rules if it would not be observable at runtime.  For example,
/// if it can be proven that calling through an interface method would lead to the exact same
/// call at runtime to a specific implementation of that interface method, then it can be legal to
/// remove such a cast as at runtime this would not be observable.  This does in effect mean that
/// the emitted IL will be different, but this matches the user expectation that the *end* behavior
/// of their code remains the same.
/// </summary>
internal static class CastSimplifier
{
    public static bool IsUnnecessaryCast(ExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
        => cast switch
        {
            CastExpressionSyntax castExpression => IsUnnecessaryCast(castExpression, semanticModel, cancellationToken),
            BinaryExpressionSyntax binaryExpression => IsUnnecessaryAsCast(binaryExpression, semanticModel, cancellationToken),
            _ => false,
        };

    public static bool IsUnnecessaryAsCast(BinaryExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return cast.Kind() == SyntaxKind.AsExpression &&
            !cast.WalkUpParentheses().ContainsDiagnostics &&
            IsCastSafeToRemove(cast, cast.Left, semanticModel, cancellationToken);
    }

    public static bool IsUnnecessaryCast(CastExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        // Can't remove casts in code that has syntax errors.
        if (cast.WalkUpParentheses().ContainsDiagnostics)
            return false;

        // First handle very special cases where casts are safe to remove, but where we violate 
        // the general rules of CastSimplifier.  Specifically, look for cases where there are multiple
        // casts in an expression, which push values out of, but back into the same initial domain,
        // and which can be proven to generate the same resultant values with some of the casts
        // removed.
        //
        // This violates the rule that the same set of instructions would be emitted at runtime.
        // However, it follows the spirit of the rule that this is not observable, and so removing
        // the cast is beneficial to avoid unnecessary work at runtime.

        // Special case for: (int)E == 0 case.  Enums can always compare against the constant
        // 0 without needing a cast.
        if (IsEnumCastWithZeroCompare(cast, semanticModel, cancellationToken))
            return true;

        // Special case for: (E)~(int)e case.  Enums don't need to be converted to ints to get bitwise negated.
        if (IsRemovableBitwiseEnumNegation(cast, semanticModel, cancellationToken))
            return true;

        // Special case for converting a method group to object. The compiler issues a warning if the cast is removed:
        // warning CS8974: Converting method group 'ToString' to non-delegate type 'object'. Did you intend to invoke the method?
        var castExpressionOperation = semanticModel.GetOperation(cast.Expression, cancellationToken);
        if (castExpressionOperation is
            {
                Kind: OperationKind.MethodReference,
                Parent.Kind: OperationKind.DelegateCreation,
                Parent.Parent: IConversionOperation { Type.SpecialType: SpecialType.System_Object } conversionOperation
            })
        {
            // If we have a double cast, report as unnecessary, e.g:
            // (object)(object)MethodGroup
            // (Delegate)(object)MethodGroup
            // If we have a single object cast, don't report as unnecessary e.g:
            // (object)MethodGroup
            if (conversionOperation.Parent is IConversionOperation { Type: { } parentConversionType } &&
                semanticModel.ClassifyConversion(cast.Expression, parentConversionType).Exists)
            {
                return true;
            }

            return false;
        }

        return IsCastSafeToRemove(cast, cast.Expression, semanticModel, cancellationToken);
    }

    private static bool IsEnumCastWithZeroCompare(
        CastExpressionSyntax castExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var leftOrRightChild = castExpression.WalkUpParentheses();
        if (leftOrRightChild.Parent is BinaryExpressionSyntax(SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression) binary)
        {
            var enumType = semanticModel.GetTypeInfo(castExpression.Expression, cancellationToken).Type as INamedTypeSymbol;
            var castedType = semanticModel.GetTypeInfo(castExpression.Type, cancellationToken).Type;

            if (Equals(enumType?.EnumUnderlyingType, castedType))
            {
                if (leftOrRightChild == binary.Left && IsConstantZero(binary.Right) ||
                    leftOrRightChild == binary.Right && IsConstantZero(binary.Left))
                {
                    return true;
                }
            }
        }

        return false;

        bool IsConstantZero(ExpressionSyntax child)
        {
            var constantValue = semanticModel.GetConstantValue(child, cancellationToken);
            return constantValue.HasValue &&
                   IntegerUtilities.IsIntegral(constantValue.Value) &&
                   IntegerUtilities.ToInt64(constantValue.Value) == 0;
        }
    }

    private static bool IsRemovableBitwiseEnumNegation(
        CastExpressionSyntax castExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Special case for: (E)~(int)e case or (E?)~(int)e case.  Enums don't need to be converted to ints to get
        // bitwise negated. The above is equivalent to `~e` as that keeps the same value and the same type. 

        if (castExpression.WalkUpParentheses().Parent is PrefixUnaryExpressionSyntax(SyntaxKind.BitwiseNotExpression) parent &&
            parent.WalkUpParentheses().Parent is CastExpressionSyntax parentCast)
        {
            var enumType = semanticModel.GetTypeInfo(castExpression.Expression, cancellationToken).Type as INamedTypeSymbol;
            var castedType = semanticModel.GetTypeInfo(castExpression.Type, cancellationToken).Type;

            if (Equals(enumType?.EnumUnderlyingType, castedType))
            {
                var parentCastType = semanticModel.GetTypeInfo(parentCast.Type, cancellationToken).Type;
                if (Equals(enumType, parentCastType.RemoveNullableIfPresent()))
                    return true;
            }
        }

        return false;
    }

    private static bool IsCastSafeToRemove(
        ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
        SemanticModel originalSemanticModel, CancellationToken cancellationToken)
    {
        #region blocked cases that disqualify this cast from being removed.

        // callers should have checked this.
        Contract.ThrowIfTrue(castNode.WalkUpParentheses().ContainsDiagnostics);

        // Quick syntactic checks we can do before semantic work.
        var isDefaultLiteralCast = castedExpressionNode.WalkDownParentheses().IsKind(SyntaxKind.DefaultLiteralExpression);

        // Language does not allow `if (x is default)` ever.  So if we have `if (x is (Y)default)`
        // then we can't remove the cast.  This was special cased in the language due to user 
        // confusion, and so we have to preserve this despite the standard conversion rules
        // indicating this should be fine.
        if (isDefaultLiteralCast && castNode.WalkUpParentheses().Parent is PatternSyntax or CaseSwitchLabelSyntax)
            return false;

        #endregion blocked cases

        #region allowed cases

        // There are cases in the roslyn API where a direct cast does not result in a conversion operation
        // (for example, casting a anonymous-method to a delegate type).  We have to handle these cases
        // specially.

        var originalOperation = originalSemanticModel.GetOperation(castNode, cancellationToken);
        if (originalOperation is IConversionOperation originalConversionOperation)
        {
            return IsConversionCastSafeToRemove(
                castNode, castedExpressionNode, originalSemanticModel, originalConversionOperation, cancellationToken);
        }

        if (originalOperation is IDelegateCreationOperation originalDelegateCreationOperation)
        {
            return IsDelegateCreationCastSafeToRemove(
                castNode, castedExpressionNode, originalSemanticModel, originalDelegateCreationOperation, cancellationToken);
        }

        #endregion allowed cases

        return false;
    }

    private static bool CastRemovalCouldCauseSignExtensionWarning(ExpressionSyntax castSyntax, IConversionOperation conversionOperation)
    {
        // if we have  `... | (T)x` then disallow this cast if we have a widening numeric cast and both T and x are
        // signed integers.  This can often lead to confusing situations due to sign extension bits getting padded
        // to the front of the value.  The compiler even warns here in many cases.  We don't want to reimplement the
        // entire complex compiler algorithm.  So just look for the general case and disallow entirely.
        //
        // Note: it is intentional that this only triggers when both types are integral, and the value that is 
        // being cast is a signed integer.  In other words, the compiler warns both for (long)int, as well as (ulong)int.
        if (castSyntax.WalkUpParentheses().GetRequiredParent().Kind() is SyntaxKind.BitwiseOrExpression or SyntaxKind.OrAssignmentExpression)
        {
            var conversion = conversionOperation.GetConversion();
            if (conversion.IsImplicit &&
                (conversion.IsNumeric || conversion.IsNullable) &&
                conversionOperation.Type.RemoveNullableIfPresent() is var type1 &&
                conversionOperation.Operand.Type.RemoveNullableIfPresent() is var type2 &&
                type1.IsIntegralType() &&
                type2.IsSignedIntegralType())
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDelegateCreationCastSafeToRemove(
        ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
        SemanticModel originalSemanticModel, IDelegateCreationOperation originalDelegateCreationOperation,
        CancellationToken cancellationToken)
    {
        if (originalDelegateCreationOperation.Type?.TypeKind != TypeKind.Delegate)
            return false;

        // for a cast of an anonymous method to a delegate, we have to make sure that after cast-removal
        // that we still have the same.
        var (rewrittenSemanticModel, rewrittenExpression) = GetSemanticModelWithCastRemoved(
            castNode, castedExpressionNode, originalSemanticModel, cancellationToken);
        if (rewrittenSemanticModel is null || rewrittenExpression is null)
            return false;

        var rewrittenOperation = rewrittenSemanticModel.GetOperation(rewrittenExpression.WalkDownParentheses(), cancellationToken);
        if (rewrittenOperation is not (IAnonymousFunctionOperation or IMethodReferenceOperation))
            return false;

        if (rewrittenOperation.Parent is not IDelegateCreationOperation rewrittenDelegateCreationOperation)
            return false;

        if (rewrittenDelegateCreationOperation.Type?.TypeKind != TypeKind.Delegate)
            return false;

        // having to be converting to the same delegate type.
        if (!Equals(originalDelegateCreationOperation.Type, rewrittenDelegateCreationOperation.Type))
            return false;

        // If there are two conversions applied on the same node, ensure both are legal.
        if (originalDelegateCreationOperation.Parent is IConversionOperation conversionOperation &&
            conversionOperation.Syntax == castNode &&
            !IsConversionCastSafeToRemove(castNode, castedExpressionNode, originalSemanticModel, conversionOperation, cancellationToken))
        {
            return false;
        }

        return true;
    }

    private static bool IsConversionCastSafeToRemove(
        ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
        SemanticModel originalSemanticModel, IConversionOperation originalConversionOperation,
        CancellationToken cancellationToken)
    {
        #region blocked cases

        // If the conversion doesn't exist then we can't do anything with this as the code isn't
        // semantically valid.
        var originalConversion = originalConversionOperation.GetConversion();
        if (!originalConversion.Exists)
            return false;

        // A conversion must either not exist, or it must be explicit or implicit. At this point we
        // have conversions that will always succeed, but which could have impact on the code by 
        // changing the types of things (which can affect other things like overload resolution),
        // or the runtime values of code.  We only want to remove the cast if it will do none of those
        // things.

        // Explicit conversions are conversions that cannot be proven to always succeed, conversions
        // that are known to possibly lose information.  As such, we need to preserve this as it 
        // has necessary runtime behavior that must be kept.
        if (IsExplicitCastThatMustBePreserved(originalSemanticModel, castNode, originalConversion, cancellationToken))
            return false;

        // we are starting with code like `(X)expr` and converting to just `expr`. Post rewrite we need
        // to ensure that the final converted-type of `expr` matches the final converted type of `(X)expr`.
        var originalConvertedType = originalSemanticModel.GetTypeInfo(castNode.WalkUpParentheses(), cancellationToken).ConvertedType;
        if (originalConvertedType is null || originalConvertedType.TypeKind == TypeKind.Error)
            return false;

        // If removing the cast could cause the compiler to issue a new warning, then we have to preserve it.
        if (CastRemovalCouldCauseSignExtensionWarning(castNode, originalConversionOperation))
            return false;

        // if the expression being casted is the `null` literal, then we can't remove the cast if the final
        // converted type isn't known to be a reference type.  This can happen with code like: 
        //
        // void Goo<T, S>() where T : class, S
        // {
        //     S y = (T)null;
        // }
        //
        // Effectively, this constrains S to be a reference type (as T could not otherwise derive from it).
        // However, such a invariant isn't understood by the compiler.  So if the (T) cast is removed it will
        // fail as 'null' cannot be converted to an unconstrained generic type.
        var isNullLiteralCast = castedExpressionNode.WalkDownParentheses().IsKind(SyntaxKind.NullLiteralExpression);
        if (isNullLiteralCast && !originalConvertedType.IsReferenceType && !originalConvertedType.IsNullable())
            return false;

        // SomeType s = (Action)(() => {});  // Where there's a user defined conversion from Action->SomeType.
        //
        // This cast is necessary.  The language does not allow lambdas to be directly converted to the destination
        // type without explicitly stating the intermediary reified delegate type.
        var isAnonymousFunctionCast = castedExpressionNode.WalkDownParentheses() is AnonymousFunctionExpressionSyntax;
        if (isAnonymousFunctionCast && originalConversion.IsUserDefined)
            return false;

        // So far, this looks potentially possible to remove.  Now, actually do the removal and get the
        // semantic model for the rewritten code so we can check it to make sure semantics were preserved.
        var (rewrittenSemanticModel, rewrittenExpression) = GetSemanticModelWithCastRemoved(
            castNode, castedExpressionNode, originalSemanticModel, cancellationToken);
        if (rewrittenSemanticModel is null || rewrittenExpression is null)
            return false;

        var (rewrittenConvertedType, rewrittenConversion) = GetRewrittenInfo(
            castNode, rewrittenExpression,
            originalSemanticModel, rewrittenSemanticModel,
            originalConversion, originalConvertedType, cancellationToken);
        if (rewrittenConvertedType is null || rewrittenConvertedType.TypeKind == TypeKind.Error || !rewrittenConversion.Exists)
            return false;

        if (CastRemovalWouldCauseUnintendedReferenceComparisonWarning(rewrittenExpression, rewrittenSemanticModel, cancellationToken))
            return false;

        // The final converted type may be the same even after removing the cast.  However, the cast may 
        // have been necessary to convert the type and/or value in a way that could be observable.  For example:
        //
        // object o1 = (long)expr;  // or (long)0
        // object o1 = (long?)expr; // or (long?)0
        //
        // We need to keep the cast so that the stored value stays the right type.
        if (originalConversion.IsConstantExpression ||
            originalConversion.IsNumeric ||
            originalConversion.IsEnumeration ||
            originalConversion.IsNullable)
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
        //
        // One special case of this is Span<T> => ReadOnlySpan<T>.  This is technically a user-defined-conversion on
        // Span<T>, but it can be removed for a collection expression as the compiler knows directly how to make that 
        // into a ReadOnlySpan
        if (originalConversionOperation.Parent is IConversionOperation { Conversion.IsUserDefined: true } originalParentConversionOperation)
        {
            var originalParentConversion = originalParentConversionOperation.GetConversion();
            if (originalParentConversion.IsImplicit)
            {
                var isAcceptableSpanConversion = originalConversionOperation.Type.IsSpan() && originalParentConversionOperation.Type.IsReadOnlySpan();

                if (!isAcceptableSpanConversion)
                {
                    if (!rewrittenConversion.IsUserDefined)
                        return false;

                    if (!Equals(originalParentConversion.MethodSymbol, rewrittenConversion.MethodSymbol))
                        return false;
                }
            }
        }

        // Identity fp-casts can actually change the runtime value of the fp number.  This can happen because the
        // runtime is allowed to perform the operations with wider precision than the actual specified fp-precision.
        // i.e. 64-bit doubles can actually be 80 bits at runtime.  Even though the language considers this to be an
        // identity cast, we don't want to remove these because the user may be depending on that truncation.
        if (IsIdentityFloatingPointCastThatMustBePreserved(castNode, castedExpressionNode, originalSemanticModel, cancellationToken))
            return false;

        // Identity struct casts will make a copy.  This copy may need to be kept to preserve semantics that only
        // the copy is being manipulated and not the original struct.
        if (IsIdentityStructCastThatMustBePreserved(castNode, castedExpressionNode, originalSemanticModel, cancellationToken))
            return false;

        #endregion blocked cases

        #region allowed cases that allow this cast to be removed.

        // In code like `((X)y).Z()` the cast to (X) can be removed if the same 'Z' method would be called.
        // The rules here can be subtle.  For example, if Z is virtual, and (X) is a cast up the inheritance
        // hierarchy then this is *normally* ok.  HOwever, the language resolve default parameter values 
        // from the overridden method.  So if they differ, we can't actually remove the cast.
        //
        // Similarly, if (X) is a cast to an interface, and Z is an impl of that interface method, it might
        // be possible to remove, but only if y's type is sealed, as otherwise the interface method could be
        // reimplemented in a derived type.
        //
        // Note: this path is fundamentally different from the other forms of cast removal we perform.  The
        // casts are removed because statically they make no difference to the meaning of the code.  Here,
        // the code statically changes meaning.  However, we can use our knowledge of how the language/runtime
        // works to know at *runtime* that the user will get the exact same behavior.
        if (castNode.WalkUpParentheses().Parent is MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (IsComplementaryMemberAccessAfterCastRemoval(
                    memberAccessExpression, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken))
            {
                return true;
            }
        }

        // In code like `((X)y)()` the cast to (X) can be removed if this was an implicit reference conversion
        // to a complementary delegate (because of delegate variance) *and* the return type of the delegate
        // invoke methods are the same.  For example:
        //
        //      Action<object> a = Console.WriteLine;
        //      ((Action<string>)a)("A");
        //
        // This is safe as delegate variance ensures that any parameter type has an implicit ref conversion to
        // the original delegate type.  However, the following would not be safe:
        //
        //      Func<object, string> a = ...;
        //      var v = ((Func<string, object>)a)("A");
        //
        // Here the type of 'v' would change to 'object' from 'string'.
        //
        // Note: this path is fundamentally different from the other forms of cast removal we perform.  The
        // casts are removed because statically they make no difference to the meaning of the code.  Here,
        // the code statically changes meaning.  However, we can use our knowledge of how the language/runtime
        // works to know at *runtime* that the user will get the exact same behavior.
        if (castNode.WalkUpParentheses().Parent is InvocationExpressionSyntax invocationExpression)
        {
            if (IsComplementaryInvocationAfterCastRemoval(
                    invocationExpression, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken))
            {
                return true;
            }
        }

        // If we have an implicit reference conversion in an 'is' expression then we remove the cast.  For example
        //
        //  if ((object)someRefType is string)
        //
        // However if we have:
        //
        //   List<int> list = null;
        //   if ((object)list is string)
        //
        // then we don't want to remove the cast as it can cause an error.
        if (castNode.WalkUpParentheses().Parent is BinaryExpressionSyntax(SyntaxKind.IsExpression) isExpression &&
            originalConversion.IsIdentityOrImplicitReference())
        {
            var castedExpressionType = originalSemanticModel.GetTypeInfo(castedExpressionNode, cancellationToken).Type;
            var isType = originalSemanticModel.GetTypeInfo(isExpression.Right, cancellationToken).Type;

            if (castedExpressionType != null && isType != null &&
                originalSemanticModel.Compilation.ClassifyConversion(castedExpressionType, isType).Exists)
            {
                return true;
            }
        }

        // Casts on collection can fundamentally change the runtime representation of the collection.  We do not
        // want to ever remove them in that case as that's precisely the reason a user may have provided them.
        if (originalConversion.IsCollectionExpression || rewrittenConversion.IsCollectionExpression)
        {
            // Both need to be collection expression conversions, both before and after the cast removal.  If not,
            // we have no idea what is going on and should not remove.
            if (!originalConversion.IsCollectionExpression || !rewrittenConversion.IsCollectionExpression)
                return false;

            if (IsCollectionExpressionCastThatMustBePreserved(castNode, originalSemanticModel, originalConvertedType, cancellationToken))
                return false;
        }

        // If the types of the expressions are the same, then we can remove safely.
        if (originalConvertedType.Equals(rewrittenConvertedType, SymbolEqualityComparer.IncludeNullability))
            return true;

        // We can safely remove convertion to object in interpolated strings regardless of nullability
        if (castNode.IsParentKind(SyntaxKind.Interpolation) && originalConversionOperation.Type?.SpecialType is SpecialType.System_Object)
            return true;

        // There are cases where the types change but things may still be safe to remove.

        // Case1.  A value type casted to `object` is safe if it's now getting converted to `dynamic`.
        // At runtime `dynamic` is just an `object` as well, and precasting to `object` will end up
        // with the same value and type still in the `dynamic` final location.
        if (originalConversion.IsBoxing && rewrittenConversion.IsBoxing &&
            originalConvertedType.IsReferenceType && rewrittenConvertedType.TypeKind == TypeKind.Dynamic)
        {
            return true;
        }

        // There are cases where a cast does have runtime meaning, but can be removed from one location because
        // the same effective conversion would happen in the code in a different location.  For example:
        //
        //      int? a = b ? (int?)0 : 1
        //
        // remove this cast will change the meaning of that conditional.  It will now produce an int instead of
        // an int?.  However, we know the same integral value will be produced by the conditional, but will then
        // be wrapped with a final conversion back into an int?.
        if (IsConditionalCastSafeToRemove(
                castNode, originalSemanticModel,
                rewrittenExpression, rewrittenSemanticModel, cancellationToken))
        {
            return true;
        }

        // Widening a value before bitwise negation produces the same value as bitwise negation
        // followed by the same widening.  For example:
        //
        // public static long P(long a, int b)
        //     => a & ~[|(long)|]b;
        if (IsRemovableWideningSignedBitwiseNegation(
                castNode, originalConversionOperation,
                rewrittenExpression, rewrittenSemanticModel, cancellationToken))
        {
            return true;
        }

        // (float?)(int?)2147483647
        //
        // The inner cast is not necessary here because there is already a lifted nullable conversion
        // of the innermost expression to the outer conversion type.
        if (IsMultipleImplicitNullableConversion(originalConversionOperation))
            return true;

        #endregion allowed cases.

        return false;
    }

    private static bool IsCollectionExpressionCastThatMustBePreserved(
        ExpressionSyntax castNode,
        SemanticModel originalSemanticModel,
        ITypeSymbol originalConvertedType,
        CancellationToken cancellationToken)
    {
        // If we can't figure out the type we were casting to, then preserve this cast.
        var castedType = originalSemanticModel.GetTypeInfo(castNode, cancellationToken).Type;
        if (castedType is null)
            return true;

        // If the types are exactly the same, like:
        //
        // int[] x = (int[])[1, 2, 3];
        //
        // then this is fine to remove.
        if (originalConvertedType.Equals(castedType, SymbolEqualityComparer.IncludeNullability))
            return false;

        // the original and resultant types are *not* the same.  This is sometimes ok to remove depending on which
        // types it is.  Currently, we only support special behavior for named types.

        if (castedType is not INamedTypeSymbol namedCastedType ||
            originalConvertedType is not INamedTypeSymbol originalNamedConvertedType)
        {
            return true;
        }

        if (namedCastedType.TypeArguments.Length != 1 && originalNamedConvertedType.TypeArguments.Length != 1)
            return true;

        if (!originalNamedConvertedType.TypeArguments[0].Equals(namedCastedType.TypeArguments[0], SymbolEqualityComparer.IncludeNullability))
            return true;

        // ReadOnlySpan<T> x = (Span<T>)[a, b, c];
        //
        // unnecessary cast to widened span type.  Because it narrows again,
        // this is safe to elide as the compiler will go directly to ReadOnlySpan<T> itself.
        if (originalConvertedType.IsReadOnlySpan() && castedType.IsSpan())
            return false;

        // IEnumerable<T> x = (DerivedReadOnlyInterfaceType<T>)[a, b, c]; // also for IReadOnlyCollection and IReadOnlyList
        if (originalNamedConvertedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T &&
            namedCastedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IReadOnlyCollection_T or SpecialType.System_Collections_Generic_IReadOnlyList_T)
        {
            return false;
        }

        // ICollection<T> x = (IList<T>)[a, b, c];
        if (originalNamedConvertedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_ICollection_T &&
            namedCastedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IList_T)
        {
            return false;
        }

        // ICollection<T> x = (List<T>)[a, b, c]; // also for IList
        if (originalNamedConvertedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_ICollection_T or SpecialType.System_Collections_Generic_IList_T &&
            namedCastedType.OriginalDefinition.Equals(originalSemanticModel.Compilation.ListOfTType()))
        {
            return false;
        }

        return true;
    }

    private static bool IsIdentityStructCastThatMustBePreserved(
        ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        // Identity struct casts will make a copy.  This copy may need to be kept to preserve semantics that only
        // the copy is being manipulated and not the original struct.
        //
        // Note: this is an innacurate heuristic.  Generally speaking, practically any member accessed off of a
        // struct might mutate it (like accessing .Length on an ImmutableArray).  But practically speaking that is
        // highly unlikely to actually mutate.  To avoid many false negatives from allowing us to simplify pointless
        // struct casts, we only look for a very narrow case that just rises up to be potentially problematic.
        // Specifically, the invocation of a non-known method on a non-known struct type where neitehr the struct
        // nor method are readonly.

        var conversion = semanticModel.GetConversion(castedExpressionNode, cancellationToken);
        if (!conversion.IsIdentity)
            return false;

        var castType = semanticModel.GetTypeInfo(castNode, cancellationToken).Type;

        if (castType is null)
            return false;

        // we presume all the built-in types are immutable, so we can skip copying them.
        if (castType.IsSpecialType())
            return false;

        // if it's not a struct, then we're not making a copy and can safely remove the cast.
        if (!castType.IsStructType())
            return false;

        // if the struct is readonly, then we can safely remove the cast as it's not mutable.
        if (castType.IsReadOnly)
            return false;

        // Only have to care if we're actually casting a location (an LVALUE).  If we're operating on an rvalue then
        // we already have a copy.
        var castedSymbol = semanticModel.GetSymbolInfo(castedExpressionNode, cancellationToken).GetAnySymbol();
        if (castedSymbol is not IFieldSymbol and not ILocalSymbol and not IParameterSymbol and not IParameterSymbol { RefKind: RefKind.Ref })
            return false;

        // ok, we have some *potentially* mutable struct.  In practice these are rare, but do exist. We'll
        // optimistically presume it's ok to remove thist cast *unless* it's the form: `((X)x).SomeMethod()` (where
        // SomeMethod is not an override from System.Object and is not readonly itself).
        if (castNode.WalkUpParentheses().Parent is not MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } memberAccessExpression)
            return false;

        var memberSymbol = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).GetAnySymbol();
        if (memberSymbol is not IMethodSymbol methodSymbol)
            return false;

        // if it's a readonly method, it's fine to call on the original without copying.
        if (methodSymbol.IsReadOnly)
            return false;

        for (var current = methodSymbol; current != null; current = current.OverriddenMethod)
        {
            if (current.ContainingType.SpecialType == SpecialType.System_Object)
                return false;
        }

        // Ok, calling some method that could mutate this struct.  have to keep this cast.
        return true;
    }

    private static bool IsMultipleImplicitNullableConversion(IConversionOperation originalConversionOperation)
    {
        // (float?)(int?)2147483647

        var innerOriginalConversion = originalConversionOperation.GetConversion();
        if (!innerOriginalConversion.IsImplicit || !innerOriginalConversion.IsNullable)
            return false;

        // if the inner conversion was user defined, we need to keep it as it may have executed user code.
        if (innerOriginalConversion.IsUserDefined)
            return false;

        if (originalConversionOperation.Parent is not IConversionOperation outerOriginalConversionOperation)
            return false;

        var outerOriginalConversion = outerOriginalConversionOperation.GetConversion();
        if (!outerOriginalConversion.IsImplicit || !outerOriginalConversion.IsNullable)
            return false;

        return true;
    }

    private static bool IsRemovableWideningSignedBitwiseNegation(
        ExpressionSyntax castNode, IConversionOperation originalConversionOperation,
        ExpressionSyntax rewrittenExpression, SemanticModel rewrittenSemanticModel,
        CancellationToken cancellationToken)
    {
        // Can potentially remove the cast in:
        //
        // public static long P(long a, int b)
        //     => a & ~[|(long)|]b;
        //
        // We need to have an implicit numeric conversion.  Parented by a ~. After removing the cast, we should
        // have the same conversion now implicitly on the outside of the `~`.
        // 
        // Similarly, the casted type needs to be the same type we get post rewrite outside the `~`.
        //
        // Note: this removal only works with signed integers.  With unsigned integers the distinction matters.
        // Consider ~(ulong)uintVal vs (ulong)~uintVal.  the former will extend out the value with 0s, which
        // will all be flipped to 1s.  The latter will flip any leading 0s to 1s, but will then extend out the
        // rest with 1s.

        var originalConversion = originalConversionOperation.GetConversion();
        if (!originalConversion.IsImplicit || !originalConversion.IsNumeric)
            return false;

        if (!IsSignedIntegralOrIntPtrType(originalConversionOperation.Type) ||
            !IsSignedIntegralOrIntPtrType(originalConversionOperation.Operand.Type))
        {
            return false;
        }

        var parent = castNode.WalkUpParentheses().GetRequiredParent();
        if (parent is not PrefixUnaryExpressionSyntax(SyntaxKind.BitwiseNotExpression))
            return false;

        // If we were parented by a bitwise negation before, we must also be afterwards.
        var rewrittenBitwiseNotExpression = (PrefixUnaryExpressionSyntax)rewrittenExpression.WalkUpParentheses().GetRequiredParent();
        Debug.Assert(rewrittenBitwiseNotExpression.Kind() == SyntaxKind.BitwiseNotExpression);

        var rewrittenOperation = rewrittenSemanticModel.GetOperation(rewrittenBitwiseNotExpression, cancellationToken);
        if (rewrittenOperation is not IUnaryOperation { OperatorKind: UnaryOperatorKind.BitwiseNegation })
            return false;

        // Post rewrite we need to have the same conversion outside that `~` that we had inside.
        if (rewrittenOperation.Parent is not IConversionOperation rewrittenBitwiseNotConversionOperation)
            return false;

        var rewrittenBitwiseNotConversion = rewrittenBitwiseNotConversionOperation.GetConversion();
        if (originalConversion != rewrittenBitwiseNotConversion)
            return false;

        // Ensure the types of the cast-inside is the same as the type outside the rewritten `~`.
        var originalConvertedType = originalConversionOperation.Type;
        var rewrittenBitwiseNotConversionType = rewrittenBitwiseNotConversionOperation.Type;
        if (IsNullOrErrorType(originalConvertedType) ||
            IsNullOrErrorType(rewrittenBitwiseNotConversionType))
        {
            return false;
        }

        if (!originalConvertedType.Equals(rewrittenBitwiseNotConversionType, SymbolEqualityComparer.IncludeNullability))
            return false;

        return true;
    }

    private static bool IsSignedIntegralOrIntPtrType(ITypeSymbol? type)
        => type.IsSignedIntegralType() || type?.SpecialType is SpecialType.System_IntPtr;

    private static bool IsConditionalCastSafeToRemove(
        ExpressionSyntax castNode, SemanticModel originalSemanticModel,
        ExpressionSyntax rewrittenExpression, SemanticModel rewrittenSemanticModel, CancellationToken cancellationToken)
    {
        if (castNode is not CastExpressionSyntax castExpression)
            return false;

        var parent = castExpression.WalkUpParentheses();
        if (parent.Parent is not ConditionalExpressionSyntax originalConditionalExpression)
            return false;

        // if we were parented by a conditional before, we must be parented by a conditional afterwards.
        var rewrittenConditionalExpression = (ConditionalExpressionSyntax)rewrittenExpression.WalkUpParentheses().GetRequiredParent();

        if (parent != originalConditionalExpression.WhenFalse && parent != originalConditionalExpression.WhenTrue)
            return false;

        if (originalSemanticModel.GetOperation(castExpression, cancellationToken) is not IConversionOperation conversionOperation)
            return false;

        var originalConversion = conversionOperation.GetConversion();
        if (!originalConversion.IsNullable && !originalConversion.IsNumeric)
            return false;

        if (originalConversion.IsNullable)
        {
            // if we have `a ? (int?)b : default` then we can't remove the nullable cast as it changes the
            // meaning of `default`.
            if (originalConditionalExpression.WhenTrue.WalkDownParentheses().IsKind(SyntaxKind.DefaultLiteralExpression) ||
                originalConditionalExpression.WhenFalse.WalkDownParentheses().IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                return false;
            }
        }

        var originalCastExpressionTypeInfo = originalSemanticModel.GetTypeInfo(castExpression, cancellationToken);
        var originalConditionalTypeInfo = originalSemanticModel.GetTypeInfo(originalConditionalExpression, cancellationToken);
        var rewrittenConditionalTypeInfo = rewrittenSemanticModel.GetTypeInfo(rewrittenConditionalExpression, cancellationToken);

        if (IsNullOrErrorType(originalCastExpressionTypeInfo) ||
            IsNullOrErrorType(originalConditionalTypeInfo) ||
            IsNullOrErrorType(rewrittenConditionalTypeInfo))
        {
            return false;
        }

        // when we have    a ? (T)b : c
        // 
        // then we want the type of the written conditional to be T as well.  And we want the final converted
        // type of `a ? b : c` to be the same as what `a ? (T)b : c` is converted to.

        if (!originalConditionalTypeInfo.ConvertedType!.Equals(rewrittenConditionalTypeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability))
            return false;

        var castType = originalSemanticModel.GetTypeInfo(castExpression, cancellationToken).Type;
        if (IsNullOrErrorType(castType))
            return false;

        if (rewrittenSemanticModel.GetOperation(rewrittenConditionalExpression, cancellationToken) is not IConditionalOperation rewrittenConditionalOperation)
            return false;

        if (castType.Equals(rewrittenConditionalOperation.Type, SymbolEqualityComparer.IncludeNullability))
            return true;

        if (rewrittenConditionalOperation.Parent is IConversionOperation conditionalParentConversion &&
            conditionalParentConversion.GetConversion().IsImplicit &&
            castType.Equals(conditionalParentConversion.Type, SymbolEqualityComparer.IncludeNullability))
        {
            return true;
        }

        return false;
    }

    private static bool IsNullOrErrorType(TypeInfo info)
        => IsNullOrErrorType(info.Type) || IsNullOrErrorType(info.ConvertedType);

    private static bool IsNullOrErrorType([NotNullWhen(false)] ITypeSymbol? type)
        => type is null || type is IErrorTypeSymbol;

    private static bool CastRemovalWouldCauseUnintendedReferenceComparisonWarning(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Translated from DiagnosticPass.CheckRelationals
        var parentBinary = expression.WalkUpParentheses().GetRequiredParent() as BinaryExpressionSyntax;
        if (parentBinary != null && parentBinary.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression)
        {
            var operation = semanticModel.GetOperation(parentBinary, cancellationToken);
            if (operation.UnwrapImplicitConversion() is IBinaryOperation binaryOperation)
            {
                if (binaryOperation.LeftOperand.Type?.SpecialType == SpecialType.System_Object &&
                    !IsExplicitCast(parentBinary.Left) &&
                    !IsConstantNull(binaryOperation.LeftOperand) &&
                    ConvertedHasUserDefinedEquals(binaryOperation.OperatorKind, binaryOperation.RightOperand))
                {
                    return true;
                }
                else if (binaryOperation.RightOperand.Type?.SpecialType == SpecialType.System_Object &&
                    !IsExplicitCast(parentBinary.Right) &&
                    !IsConstantNull(binaryOperation.RightOperand) &&
                    ConvertedHasUserDefinedEquals(binaryOperation.OperatorKind, binaryOperation.LeftOperand))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ConvertedHasUserDefinedEquals(BinaryOperatorKind operatorKind, IOperation operation)
    {
        // translated from DiagnosticPass.ConvertedHasEqual

        if (operation is not IConversionOperation conversionOperation)
            return false;

        if (IsExplicitCast(conversionOperation.Syntax))
            return false;

        if (conversionOperation.Operand.Type is not INamedTypeSymbol original)
            return false;

        if (!original.IsReferenceType || original.TypeKind == TypeKind.Interface)
            return false;

        var opName = operatorKind == BinaryOperatorKind.Equals
            ? WellKnownMemberNames.EqualityOperatorName
            : WellKnownMemberNames.InequalityOperatorName;
        for (var type = original; type != null; type = type.BaseType)
        {
            foreach (var sym in type.GetMembers(opName))
            {
                if (sym is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } op)
                {
                    var parameters = op.GetParameters();
                    if (parameters.Length == 2 &&
                        type.Equals(parameters[0].Type) &&
                        type.Equals(parameters[1].Type))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsConstantNull(IOperation operation)
        => operation.ConstantValue.HasValue && operation.ConstantValue.Value is null;

    private static bool IsExplicitCast(SyntaxNode node)
        => node is ExpressionSyntax expression && expression.WalkDownParentheses().Kind() is SyntaxKind.CastExpression or SyntaxKind.AsExpression;

    private static bool IsExplicitCastThatMustBePreserved(
        SemanticModel semanticModel,
        ExpressionSyntax castOrAsNode,
        Conversion conversion,
        CancellationToken cancellationToken)
    {
        if (!conversion.IsExplicit)
            return false;

        // Some explicit casts are safe to remove as they still will have no runtime impact, (or the compiler would
        // insert the implicit cast for it later due to surrounding context).

        // Explicit identity casts arise with things like `(string?)""`.  In this case, there is no runtime impact,
        // just type system impact.  This is a candidate for removal, and our later checks will ensure the same 
        // types remain.
        if (conversion.IsIdentity)
            return false;

        // Explicit nullable casts arise with things like `(int?)0`.  These will succeed at runtime, but are potentially
        // removable if the language would insert such a cast anyways (for things like `x ? (int?)0 : null`).  In C# 9
        // and above this will create a legal conditional conversion that implicitly adds that cast.
        //
        // Note: this does not apply for `as byte?`.  This is an explicit as-cast that can produce null values and
        // so it should be maintained.
        if (conversion.IsNullable && castOrAsNode is CastExpressionSyntax castExpression)
        {
            var parent = castOrAsNode.WalkUpParentheses();
            if (parent.Parent is ConditionalExpressionSyntax conditionalExpression)
            {
                // If we have `(T?)expr == null` or `null == (T?)expr` then we can potentially remove this cast as
                // the lang will implicitly create such a cast with an appropriate type and null.
                var (castSide, otherSide) = conditionalExpression.WhenTrue == parent
                    ? (conditionalExpression.WhenTrue, conditionalExpression.WhenFalse)
                    : (conditionalExpression.WhenFalse, conditionalExpression.WhenTrue);

                if (otherSide.WalkDownParentheses().Kind() == SyntaxKind.NullLiteralExpression)
                    return false;

                // if we have `(T?)TExpr == nullableTExpr` then we can also remove this cast as the language will
                // insert the same nullable widening cast implicitly.
                //
                // If we have `(T?)TExpr == TExpr` then we can potentially remove this cast if the caller determines
                // that there is an outer contextual cast to `T?` higher up.
                var castSideType = semanticModel.GetTypeInfo(castSide, cancellationToken).Type;
                var castedExpressionType = semanticModel.GetTypeInfo(castExpression.Expression, cancellationToken).Type;

                if (castSideType.IsNullable(out var underlyingType) && Equals(underlyingType, castedExpressionType))
                {
                    var otherSideType = semanticModel.GetTypeInfo(otherSide, cancellationToken).Type;
                    if (Equals(castSideType, otherSideType) || Equals(underlyingType, otherSideType))
                        return false;
                }
            }
        }

        return true;
    }

    private static bool IsIdentityFloatingPointCastThatMustBePreserved(
       ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
       SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var conversion = semanticModel.GetConversion(castedExpressionNode, cancellationToken);
        if (!conversion.IsIdentity)
            return false;

        var castType = semanticModel.GetTypeInfo(castNode, cancellationToken).Type;
        var castedExpressionType = semanticModel.GetTypeInfo(castedExpressionNode, cancellationToken).Type;

        // Floating point casts can have subtle runtime behavior, even between the same fp types. For example, a
        // cast from float-to-float can still change behavior because it may take a higher precision computation and
        // truncate it to 32bits.
        //
        // Because of this we keep floating point conversions unless we can prove that it's safe.  The only safe
        // times are when we're loading or storing into a location we know has the same size as the cast size
        // (i.e. reading/writing into a field).
        if (!IsFloatingPointType(castedExpressionType) ||
            !IsFloatingPointType(castType))
        {
            // wasn't a floating point conversion.
            return false;
        }

        // Identity fp conversion is safe if this is a read from a fp field/array
        if (IsFieldOrArrayElement(semanticModel, castedExpressionNode, cancellationToken))
            return false;

        // Boxing the result will automatically truncate this as well as this must be stored into a real 32bit or
        // 64bit location.  As such, the explicit cast to truncate to 32/64 isn't necessary.  See
        // https://github.com/dotnet/roslyn/pull/56932#discussion_r725241921 for more details.
        var parentConversion = semanticModel.GetConversion(castNode, cancellationToken);
        if (parentConversion.Exists && parentConversion.IsBoxing)
            return false;

        // It wasn't a read from a fp/field/array.  But it might be a write into one.

        castNode = castNode.WalkUpParentheses();
        if (castNode.Parent is AssignmentExpressionSyntax assignmentExpression &&
            assignmentExpression.Right == castNode)
        {
            // Identity fp conversion is safe if this is a write to a fp field/array
            if (IsFieldOrArrayElement(semanticModel, assignmentExpression.Left, cancellationToken))
                return false;
        }
        else if (castNode.Parent is InitializerExpressionSyntax(SyntaxKind.ArrayInitializerExpression) arrayInitializer)
        {
            // Identity fp conversion is safe if this is in an array initializer.
            var typeInfo = semanticModel.GetTypeInfo(arrayInitializer, cancellationToken);
            return typeInfo.Type?.Kind == SymbolKind.ArrayType;
        }
        else if (castNode.Parent is EqualsValueClauseSyntax equalsValue &&
                 equalsValue.Value == castNode &&
                 equalsValue.Parent is VariableDeclaratorSyntax variableDeclarator)
        {
            // Identity fp conversion is safe if this is in a field initializer.
            var symbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
            if (symbol?.Kind == SymbolKind.Field)
                return false;
        }

        // We have to preserve this cast.
        return true;
    }

    private static bool IsFloatingPointType(ITypeSymbol? type)
        => type?.SpecialType is SpecialType.System_Double or SpecialType.System_Single;

    private static bool IsFieldOrArrayElement(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var operation = semanticModel.GetOperation(expression.WalkDownParentheses(), cancellationToken);
        return operation is IFieldReferenceOperation or IArrayElementReferenceOperation;
    }

    private static bool IntroducedConditionalExpressionConversion(
        ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        for (SyntaxNode? current = expression; current != null; current = current.Parent)
        {
            var conversion = semanticModel.GetConversion(current, cancellationToken);
            if (conversion.IsConditionalExpression)
                return true;
        }

        return false;
    }

    private static bool IntroducedAmbiguity(
        ExpressionSyntax castNode, ExpressionSyntax rewrittenExpression,
        SemanticModel originalSemanticModel, SemanticModel rewrittenSemanticModel,
        CancellationToken cancellationToken)
    {
        for (SyntaxNode? currentOld = castNode.WalkUpParentheses().Parent, currentNew = rewrittenExpression.WalkUpParentheses().Parent;
             currentOld != null && currentNew != null;
             currentOld = currentOld.Parent, currentNew = currentNew.Parent)
        {
            Debug.Assert(currentOld.Kind() == currentNew.Kind());
            var oldSymbolInfo = originalSemanticModel.GetSymbolInfo(currentOld, cancellationToken);
            if (oldSymbolInfo.Symbol != null)
            {
                // if previously we bound to a single symbol, but now we don't, then we introduced an
                // error of some sort.  Have to bail out immediately and keep the cast.
                var newSymbolInfo = rewrittenSemanticModel.GetSymbolInfo(currentNew, cancellationToken);
                if (newSymbolInfo.Symbol is null)
                    return true;
            }

            if (currentOld is InterpolatedStringExpressionSyntax && currentNew is InterpolatedStringExpressionSyntax)
            {
                // In the case of interpolations, we need to dive into the operation level to determine if the meaning
                // of the the interpolation stayed the same in the case of interpolation handlers.
                if (originalSemanticModel.GetOperation(currentOld, cancellationToken) is not IInterpolatedStringOperation oldInterpolationOperation)
                    return true;

                if (rewrittenSemanticModel.GetOperation(currentNew, cancellationToken) is not IInterpolatedStringOperation newInterpolationOperation)
                    return true;

                if (oldInterpolationOperation.Parts.Length != newInterpolationOperation.Parts.Length)
                    return true;

                for (int i = 0, n = oldInterpolationOperation.Parts.Length; i < n; i++)
                {
                    var oldInterpolationPart = oldInterpolationOperation.Parts[i];
                    var newInterpolationPart = newInterpolationOperation.Parts[i];
                    if (oldInterpolationPart.Kind != newInterpolationPart.Kind)
                        return true;

                    // If we were calling some interpolation AppendFormatted helper, and now we're not, we introduced a problem.
                    if (oldInterpolationPart is IInterpolatedStringAppendOperation { AppendCall: not IInvalidOperation } &&
                        newInterpolationPart is IInterpolatedStringAppendOperation { AppendCall: IInvalidOperation })
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool ChangedOverloadResolution(
        ExpressionSyntax castNode, ExpressionSyntax rewrittenExpression,
        SemanticModel originalSemanticModel, SemanticModel rewrittenSemanticModel,
        CancellationToken cancellationToken)
    {
        // walk upwards checking overload resolution results.  note: we skip until we hit the first argument
        // as we don't care about symbol resolution changing when removing a cast in something like `((D)b).X()`
        var haveHitArgumentNode = false;
        for (SyntaxNode? currentOld = castNode.WalkUpParentheses().Parent, currentNew = rewrittenExpression.WalkUpParentheses().Parent;
             currentOld != null && currentNew != null;
             currentOld = currentOld.Parent, currentNew = currentNew.Parent)
        {
            Debug.Assert(currentOld.Kind() == currentNew.Kind());
            if (!haveHitArgumentNode && currentOld.Kind() != SyntaxKind.Argument)
                continue;

            haveHitArgumentNode = true;

            var oldSymbolInfo = originalSemanticModel.GetSymbolInfo(currentOld, cancellationToken).Symbol;
            var newSymbolInfo = rewrittenSemanticModel.GetSymbolInfo(currentNew, cancellationToken).Symbol;

            // ignore local functions.  First, we can't test them for equality in speculative situations, but also we 
            // can't end up with an overload resolution issue for them as they don't have overloads.
            if (oldSymbolInfo is IMethodSymbol method &&
                method.MethodKind is not (MethodKind.LocalFunction or MethodKind.LambdaMethod) &&
                !Equals(oldSymbolInfo, newSymbolInfo))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ChangedForEachResolution(
        ExpressionSyntax castNode, ExpressionSyntax rewrittenExpression,
        SemanticModel originalSemanticModel, SemanticModel rewrittenSemanticModel)
    {
        for (SyntaxNode? currentOld = castNode.WalkUpParentheses().Parent, currentNew = rewrittenExpression.WalkUpParentheses().Parent;
             currentOld != null && currentNew != null;
             currentOld = currentOld.Parent, currentNew = currentNew.Parent)
        {
            Debug.Assert(currentOld.Kind() == currentNew.Kind());
            if (currentOld is CommonForEachStatementSyntax oldForEach &&
                currentNew is CommonForEachStatementSyntax newForEach)
            {
                // TODO(cyrusn): Do we need to validate anything else in the foreach infos?
                var oldForEachInfo = originalSemanticModel.GetForEachStatementInfo(oldForEach);
                var newForEachInfo = rewrittenSemanticModel.GetForEachStatementInfo(newForEach);

                var oldConversion = oldForEachInfo.ElementConversion;
                var newConversion = newForEachInfo.ElementConversion;

                if (oldConversion.IsUserDefined != newConversion.IsUserDefined)
                    return true;

                if (!Equals(oldConversion.MethodSymbol, newConversion.MethodSymbol))
                    return true;
            }
        }

        return false;
    }

    private static bool IsComplementaryMemberAccessAfterCastRemoval(
        MemberAccessExpressionSyntax memberAccessExpression,
        ExpressionSyntax rewrittenExpression,
        SemanticModel originalSemanticModel,
        SemanticModel rewrittenSemanticModel,
        CancellationToken cancellationToken)
    {
        var originalMemberSymbol = originalSemanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol;
        if (originalMemberSymbol is null)
            return false;

        var rewrittenMemberAccessExpression = (MemberAccessExpressionSyntax)rewrittenExpression.WalkUpParentheses().GetRequiredParent();
        var rewrittenMemberSymbol = rewrittenSemanticModel.GetSymbolInfo(rewrittenMemberAccessExpression, cancellationToken).Symbol;
        if (rewrittenMemberSymbol is null)
            return false;

        if (originalMemberSymbol.Kind != rewrittenMemberSymbol.Kind)
            return false;

        // check for: ((X)expr).Invoke(...);
        if (IsComplementaryDelegateInvoke(originalMemberSymbol, rewrittenMemberSymbol))
            return true;

        // Ok, we had two good member symbols before/after the cast removal.  In other words we have:
        //
        //      ((X)expr).Y
        //      (expr).Y

        // Next, see if this is a call to an interface method.
        if (originalMemberSymbol.ContainingType.TypeKind == TypeKind.Interface)
        {
            var rewrittenType = rewrittenSemanticModel.GetTypeInfo(rewrittenExpression, cancellationToken).Type;
            if (IsNullOrErrorType(rewrittenType))
                return false;

            // If we don't have a reference type, then it may not be safe to remove the cast.  The cast could
            // could have been boxing the value and removing that could cause us to operate not on the copy.
            //
            // Note: intrinsics and enums are also safe as we know they don't have state and thus
            // will have the same semantics whether or not they're boxed.
            //
            // It is also safe if we know the value is already a copy to begin with.
            //
            // TODO(cyrusn): this may not be true of floating point numbers.  Are we sure that it's
            // safe to remove an interface cast in that case?  Could that cast narrow the precision of 
            // a wider FP number to a narrower amount (like 80bit FP to 64bit)?

            if (!rewrittenType.IsReferenceType &&
                !IsIntrinsicOrEnum(rewrittenType) &&
                !IsCopy(rewrittenSemanticModel, rewrittenExpression, rewrittenType, cancellationToken))
            {
                return false;
            }

            // if we are still calling through to the same interface method, then this is safe to call.
            if (Equals(originalMemberSymbol, rewrittenMemberSymbol))
                return true;

            // Ok, we have a type casted to an interface.  It may be safe to remove this interface cast
            // if we still call into the implementation of that interface member afterwards.  Note: the
            // type has to be sealed, otherwise the interface method may have been reimplemented lower
            // in the inheritance hierarchy.
            //
            // However, if this was an object creation expression, then we know the exact type that was
            // created, and don't have to worry about subclassing.

            var isSealed =
                rewrittenType.IsSealed ||
                rewrittenType.IsValueType ||
                rewrittenType.TypeKind == TypeKind.Array ||
                IsIntrinsicOrEnum(rewrittenType) ||
                rewrittenExpression.WalkDownParentheses() is ObjectCreationExpressionSyntax;

            if (!isSealed)
                return false;

            // Then look for the current implementation of that interface member.
            var rewrittenContainingType = rewrittenMemberSymbol.ContainingType;
            var implementationMember = rewrittenContainingType.FindImplementationForInterfaceMember(originalMemberSymbol);
            if (implementationMember is null)
                return false;

            // if that's not the method we're currently calling, then this definitely isn't safe to remove.
            return
                Equals(implementationMember, rewrittenMemberSymbol) &&
                ParameterNamesAndDefaultValuesAndReturnTypesMatch(
                    memberAccessExpression, originalSemanticModel, originalMemberSymbol, rewrittenMemberSymbol, cancellationToken);
        }

        // Second, check if this is a virtual call to a different location in the inheritance hierarchy.
        // Importantly though, because of covariant return types, we have to make sure the overrides 
        // agree on the return type, or else this could change the final type of hte expression.
        for (var current = rewrittenMemberSymbol; current != null; current = current.GetOverriddenMember())
        {
            if (Equals(originalMemberSymbol, current))
            {
                // we're calling into a override of a higher up virtual in the original code.
                // This is safe as long as the names of the parameters and all default values
                // are the same.  This is because the compiler uses the names and default
                // values of the overridden member, even though it emits a virtual call to the
                // the highest in the inheritance chain.
                return ParameterNamesAndDefaultValuesAndReturnTypesMatch(
                    memberAccessExpression, originalSemanticModel, originalMemberSymbol, rewrittenMemberSymbol, cancellationToken);
            }
        }

        return false;
    }

    private static bool IsComplementaryInvocationAfterCastRemoval(
        InvocationExpressionSyntax memberAccessExpression,
        ExpressionSyntax rewrittenExpression,
        SemanticModel originalSemanticModel,
        SemanticModel rewrittenSemanticModel,
        CancellationToken cancellationToken)
    {
        var originalMemberSymbol = originalSemanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol;
        if (originalMemberSymbol is null)
            return false;

        var rewrittenMemberAccessExpression = (InvocationExpressionSyntax)rewrittenExpression.WalkUpParentheses().GetRequiredParent();
        var rewrittenMemberSymbol = rewrittenSemanticModel.GetSymbolInfo(rewrittenMemberAccessExpression, cancellationToken).Symbol;
        if (rewrittenMemberSymbol is null)
            return false;

        return IsComplementaryDelegateInvoke(originalMemberSymbol, rewrittenMemberSymbol);
    }

    private static bool IsComplementaryDelegateInvoke(ISymbol originalMemberSymbol, ISymbol rewrittenMemberSymbol)
    {
        if (originalMemberSymbol is not IMethodSymbol { MethodKind: MethodKind.DelegateInvoke } originalMethodSymbol ||
            rewrittenMemberSymbol is not IMethodSymbol { MethodKind: MethodKind.DelegateInvoke } rewrittenMethodSymbol)
        {
            return false;
        }

        // if we're invoking a delegate method, then the removal of the cast is mostly safe (as the 
        // compiler will only allow implicit reference conversions between variant delegates and 
        // variant delegates will only allow different implicit reference conversions of their
        // parameters and return type.

        // However, if the delegate return type differs, then that could change semantics higher
        // up, so we must disallow this if they're not the same.
        return Equals(originalMethodSymbol.ReturnType, rewrittenMethodSymbol.ReturnType);
    }

    private static bool IsIntrinsicOrEnum(ITypeSymbol rewrittenType)
        => rewrittenType.IsIntrinsicType() ||
           rewrittenType.IsEnumType() ||
           rewrittenType.SpecialType == SpecialType.System_Enum;

    private static bool IsCopy(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        ITypeSymbol rewrittenType,
        CancellationToken cancellationToken)
    {
        // Checked by caller first.
        Debug.Assert(!rewrittenType.IsReferenceType && !IsIntrinsicOrEnum(rewrittenType));

        // Be conservative here.  If we can't prove it's not a copy assume it's a copy.
        expression = expression.WalkDownParentheses();
        var operation = semanticModel.GetOperation(expression, cancellationToken);
        if (operation != null)
        {
            // All operators return a fresh copy.  Note: this may need to be revisited if operators
            // ever can return byref in the future.
            if (operation is IBinaryOperation { OperatorMethod: not null })
                return true;

            if (operation is IUnaryOperation { OperatorMethod: not null })
                return true;

            // if we're getting the struct through a non-ref property, then it will make a copy.
            if (operation is IPropertyReferenceOperation { Property.RefKind: not RefKind.Ref })
                return true;

            // if we're getting the struct as the return value of a non-ref method, then it will make a copy.
            if (operation is IInvocationOperation { TargetMethod.RefKind: not RefKind.Ref })
                return true;

            // If we're new'ing up this struct then we have a fresh copy that we can operate on.
            if (operation is IObjectCreationOperation)
                return true;
        }

        return false;
    }

    private static bool ParameterNamesAndDefaultValuesAndReturnTypesMatch(
        MemberAccessExpressionSyntax memberAccessExpression, SemanticModel semanticModel,
        ISymbol originalMemberSymbol, ISymbol rewrittenMemberSymbol, CancellationToken cancellationToken)
    {
        var originalMemberType = originalMemberSymbol.GetMemberType();
        var rewrittenMemberType = rewrittenMemberSymbol.GetMemberType();
        if (!Equals(originalMemberType, rewrittenMemberType))
            return false;

        // if this member actually invoked, ensure that we end up with the same values for default
        // parameters, and that the same names are used.  Note: we technically only need to check
        // default values for arguments not passed, and we only need to check names for those that
        // are passed.
        if (memberAccessExpression.GetRequiredParent() is InvocationExpressionSyntax invocationExpression &&
            semanticModel.GetOperation(invocationExpression, cancellationToken) is IInvocationOperation invocationOperation)
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

                    var argument = invocationOperation.Arguments.FirstOrDefault(a => Equals(originalParameter, a.Parameter));
                    var argumentSyntax = argument?.Syntax as ArgumentSyntax;

                    if (originalParameter.Name != rewrittenParameter.Name &&
                        argumentSyntax?.NameColon != null)
                    {
                        // names are different.  this is a problem if the original user code provided a named arg here.
                        return false;
                    }

                    if (originalParameter.HasExplicitDefaultValue &&
                        rewrittenParameter.HasExplicitDefaultValue &&
                        !Equals(originalParameter.ExplicitDefaultValue, rewrittenParameter.ExplicitDefaultValue) &&
                        argumentSyntax == null)
                    {
                        // parameter values are different, this is a problem if the original user code did *not* provide
                        // an argument here.
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static (ITypeSymbol? rewrittenConvertedType, Conversion rewrittenConversion) GetRewrittenInfo(
        ExpressionSyntax castNode, ExpressionSyntax rewrittenExpression,
        SemanticModel originalSemanticModel, SemanticModel rewrittenSemanticModel,
        Conversion originalConversion, ITypeSymbol originalConvertedType,
        CancellationToken cancellationToken)
    {
        if (castNode.WalkUpParentheses().Parent is InterpolationSyntax)
        {
            // Workaround https://github.com/dotnet/roslyn/issues/56934
            // Compiler does not give a conversion inside an interpolation. However, all values in the interpolation
            // holes are converted to object.
            //
            // Note: this may need to be revisited with improved interpolated strings (as they could take
            // strongly typed args and could avoid the object boxing).
            var convertedType = originalConversion.IsIdentity ? originalConvertedType : originalSemanticModel.Compilation.ObjectType;
            return (convertedType, default);
        }

        var rewrittenConvertedType = rewrittenSemanticModel.GetTypeInfo(rewrittenExpression, cancellationToken).ConvertedType;
        var rewrittenConversion = rewrittenSemanticModel.GetConversion(rewrittenExpression, cancellationToken);

        return (rewrittenConvertedType, rewrittenConversion);
    }

    private static (SemanticModel? rewrittenSemanticModel, ExpressionSyntax? rewrittenExpression) GetSemanticModelWithCastRemoved(
        ExpressionSyntax castNode,
        ExpressionSyntax castedExpressionNode,
        SemanticModel originalSemanticModel,
        CancellationToken cancellationToken)
    {
        var analyzer = new SpeculationAnalyzer(castNode, castedExpressionNode, originalSemanticModel, cancellationToken);

        var rewrittenExpression = analyzer.ReplacedExpression;
        var rewrittenSemanticModel = analyzer.SpeculativeSemanticModel;

        // Because of error tolerance in the compiler layer, it's possible for an overload resolution error
        // to occur, but all the checks above pass.  Specifically, with overload resolution, the binding layer
        // will still return results (in lambdas especially) for one of the overloads.  For example:
        //
        //    Goo(x => (int)x);
        //    void Goo(Func<int, object> x)
        //    Goo(Func<string, object> x)
        //
        // Here, removing the cast will cause an ambiguity issue. However, the type of 'x' will still appear to
        // be an 'int' because of error tolerance.  To address this, walk up all containing invocations and 
        // make sure they're calls to the same methods.
        if (IntroducedAmbiguity(castNode, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken))
            return default;

        if (ChangedOverloadResolution(castNode, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken))
            return default;

        // It's possible that removing a cast in a foreach collection expression will change how the foreach methods
        // and conversions resolve.  Ensure these stay the same to proceed.
        if (ChangedForEachResolution(castNode, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel))
            return default;

        // Removing a cast may cause a conditional-expression conversion to come into existence.  This is
        // fine as long as we're in C# 9 or above.
        if (originalSemanticModel.Compilation.LanguageVersion() < LanguageVersion.CSharp9 &&
            IntroducedConditionalExpressionConversion(rewrittenExpression, rewrittenSemanticModel, cancellationToken))
        {
            return default;
        }

        return (rewrittenSemanticModel, rewrittenExpression);
    }
}
