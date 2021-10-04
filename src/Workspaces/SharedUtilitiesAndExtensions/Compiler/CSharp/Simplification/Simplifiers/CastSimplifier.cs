// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal static class CastSimplifier
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
            #region blacklist cases that disqualify this cast from being removed.

            // Can't remove casts in code that has syntax errors.
            if (castNode.WalkUpParentheses().ContainsDiagnostics)
                return false;

            // Quick syntactic checks we can do before semantic work.
            var isDefaultLiteralCast = castedExpressionNode.WalkDownParentheses().Kind() == SyntaxKind.DefaultLiteralExpression;

            // Language does not allow `if (x is default)` ever.  So if we have `if (x is (Y)default)`
            // then we can't remove the cast.
            if (isDefaultLiteralCast && castNode.WalkUpParentheses().Parent is PatternSyntax or CaseSwitchLabelSyntax)
                return false;

            // If removing the cast would cause the compiler to issue a specific warning, then we have to preserve it.
            if (CastRemovalWouldCauseSignExtensionWarning(castNode, originalSemanticModel, cancellationToken))
                return false;

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

            return false;
        }

        private static bool CastRemovalWouldCauseSignExtensionWarning(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Logic copied from DiagnosticsPass_Warnings.CheckForBitwiseOrSignExtend.  Including comments.

            if (expression is not CastExpressionSyntax castExpression)
                return false;

            var castRoot = castExpression.WalkUpParentheses();

            // Check both binary-or, and assignment-or
            //
            //   x | (...)y
            //   x |= (...)y

            ExpressionSyntax leftOperand, rightOperand;

            if (castRoot.Parent is BinaryExpressionSyntax parentBinary)
            {
                if (!parentBinary.IsKind(SyntaxKind.BitwiseOrExpression))
                    return false;

                (leftOperand, rightOperand) = (parentBinary.Left, parentBinary.Right);
            }
            else if (castRoot.Parent is AssignmentExpressionSyntax parentAssignment)
            {
                if (!parentAssignment.IsKind(SyntaxKind.OrAssignmentExpression))
                    return false;

                (leftOperand, rightOperand) = (parentAssignment.Left, parentAssignment.Right);
            }
            else
            {
                return false;
            }

            // The native compiler skips this warning if both sides of the operator are constants.
            //
            // CONSIDER: Is that sensible? It seems reasonable that if we would warn on int | short
            // when they are non-constants, or when one is a constant, that we would similarly warn 
            // when both are constants.
            var constantValue = semanticModel.GetConstantValue(castRoot.Parent, cancellationToken);

            if (constantValue.HasValue && constantValue.Value != null)
                return false;

            // Start by determining *which bits on each side are going to be unexpectedly turned on*.

            var leftOperation = semanticModel.GetOperation(leftOperand.WalkDownParentheses(), cancellationToken);
            var rightOperation = semanticModel.GetOperation(rightOperand.WalkDownParentheses(), cancellationToken);

            if (leftOperation == null || rightOperation == null)
                return false;

            // Note: we are asking the question about if there would be a problem removing the cast. So we have to act
            // as if an explicit cast becomes an implicit one. We do this by ignoring the appropriate cast and not
            // treating it as explicit when we encounter it.

            var left = FindSurprisingSignExtensionBits(leftOperation, leftOperand == castRoot);
            var right = FindSurprisingSignExtensionBits(rightOperation, rightOperand == castRoot);

            // If they are all the same then there's no warning to give.
            if (left == right)
                return false;

            // Suppress the warning if one side is a constant, and either all the unexpected
            // bits are already off, or all the unexpected bits are already on.

            var constVal = GetConstantValueForBitwiseOrCheck(leftOperation);
            if (constVal != null)
            {
                var val = constVal.Value;
                if ((val & right) == right || (~val & right) == right)
                    return false;
            }

            constVal = GetConstantValueForBitwiseOrCheck(rightOperation);
            if (constVal != null)
            {
                var val = constVal.Value;
                if ((val & left) == left || (~val & left) == left)
                    return false;
            }

            // This would produce a warning.  Don't offer to remove the cast.
            return true;
        }

        private static ulong? GetConstantValueForBitwiseOrCheck(IOperation operation)
        {
            // We might have a nullable conversion on top of an integer constant. But only dig out
            // one level.
            if (operation is IConversionOperation conversion &&
                conversion.Conversion.IsImplicit &&
                conversion.Conversion.IsNullable)
            {
                operation = conversion.Operand;
            }

            var constantValue = operation.ConstantValue;
            if (!constantValue.HasValue || constantValue.Value == null)
                return null;

            RoslynDebug.Assert(operation.Type is not null);
            if (!operation.Type.SpecialType.IsIntegralType())
                return null;

            return IntegerUtilities.ToUInt64(constantValue.Value);
        }

        // A "surprising" sign extension is:
        //
        // * a conversion with no cast in source code that goes from a smaller
        //   signed type to a larger signed or unsigned type.
        //
        // * an conversion (with or without a cast) from a smaller
        //   signed type to a larger unsigned type.

        private static ulong FindSurprisingSignExtensionBits(IOperation? operation, bool treatExplicitCastAsImplicit)
        {
            if (operation is not IConversionOperation conversion)
                return 0;

            var from = conversion.Operand.Type;
            var to = conversion.Type;

            if (from is null || to is null)
                return 0;

            if (from.IsNullable(out var fromUnderlying))
                from = fromUnderlying;

            if (to.IsNullable(out var toUnderlying))
                to = toUnderlying;

            var fromSpecialType = from.SpecialType;
            var toSpecialType = to.SpecialType;

            if (!fromSpecialType.IsIntegralType() || !toSpecialType.IsIntegralType())
                return 0;

            var fromSize = fromSpecialType.SizeInBytes();
            var toSize = toSpecialType.SizeInBytes();

            if (fromSize == 0 || toSize == 0)
                return 0;

            // The operand might itself be a conversion, and might be contributing
            // surprising bits. We might have more, fewer or the same surprising bits
            // as the operand.

            var recursive = FindSurprisingSignExtensionBits(conversion.Operand, treatExplicitCastAsImplicit: false);

            if (fromSize == toSize)
            {
                // No change.
                return recursive;
            }

            if (toSize < fromSize)
            {
                // We are casting from a larger type to a smaller type, and are therefore
                // losing surprising bits. 
                switch (toSize)
                {
                    case 1: return unchecked((ulong)(byte)recursive);
                    case 2: return unchecked((ulong)(ushort)recursive);
                    case 4: return unchecked((ulong)(uint)recursive);
                }

                Debug.Assert(false, "How did we get here?");
                return recursive;
            }

            // We are converting from a smaller type to a larger type, and therefore might
            // be adding surprising bits. First of all, the smaller type has got to be signed
            // for there to be sign extension.

            var fromSigned = fromSpecialType.IsSignedIntegralType();

            if (!fromSigned)
                return recursive;

            // OK, we know that the "from" type is a signed integer that is smaller than the
            // "to" type, so we are going to have sign extension. Is it surprising? The only
            // time that sign extension is *not* surprising is when we have a cast operator
            // to a *signed* type. That is, (int)myShort is not a surprising sign extension.

            var explicitInCode = !conversion.IsImplicit;
            if (!treatExplicitCastAsImplicit &&
                explicitInCode &&
                toSpecialType.IsSignedIntegralType())
            {
                return recursive;
            }

            // Note that we *could* be somewhat more clever here. Consider the following edge case:
            //
            // (ulong)(int)(uint)(ushort)mySbyte
            //
            // We could reason that the sbyte-to-ushort conversion is going to add one byte of
            // unexpected sign extension. The conversion from ushort to uint adds no more bytes.
            // The conversion from uint to int adds no more bytes. Does the conversion from int
            // to ulong add any more bytes of unexpected sign extension? Well, no, because we 
            // know that the previous conversion from ushort to uint will ensure that the top bit
            // of the uint is off! 
            //
            // But we are not going to try to be that clever. In the extremely unlikely event that
            // someone does this, we will record that the unexpectedly turned-on bits are 
            // 0xFFFFFFFF0000FF00, even though we could in theory deduce that only 0x000000000000FF00
            // are the unexpected bits.

            var result = recursive;
            for (var i = fromSize; i < toSize; ++i)
                result |= (0xFFUL) << (i * 8);

            return result;
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
            if (rewrittenOperation is not IAnonymousFunctionOperation &&
                rewrittenOperation is not IMethodReferenceOperation)
            {
                return false;
            }

            if (rewrittenOperation.Parent is not IDelegateCreationOperation rewrittenDelegateCreationOperation)
                return false;

            if (rewrittenDelegateCreationOperation.Type?.TypeKind != TypeKind.Delegate)
                return false;

            // having to be converting to the same delegate type.
            return SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(
                originalDelegateCreationOperation.Type, rewrittenDelegateCreationOperation.Type);
        }

        private static bool IsNullLiteralCast(ExpressionSyntax castedExpressionNode)
            => castedExpressionNode.WalkDownParentheses().Kind() == SyntaxKind.NullLiteralExpression;

        private static bool IsConversionCastSafeToRemove(
            ExpressionSyntax castNode, ExpressionSyntax castedExpressionNode,
            SemanticModel originalSemanticModel, IConversionOperation originalConversionOperation,
            CancellationToken cancellationToken)
        {
            // If the conversion doesn't exist then we can't do anything with this as the code isn't
            // semantically valid.
            var originalConversion = originalConversionOperation.GetConversion();
            if (!originalConversion.Exists)
                return false;

            // Explicit conversions are conversions that cannot be proven to always succeed, conversions
            // that are known to possibly lose information.  As such, we need to preserve this as it 
            // has necessary runtime behavior that must be kept.
            if (IsExplicitCastThatMustBePreserved(castNode, originalConversion))
                return false;

            // A conversion must either not exist, or it must be explicit or implicit. At this point we
            // have conversions that will always succeed, but which could have impact on the code by 
            // changing the types of things (which can affect other things like overload resolution),
            // or the runtime values of code.  We only want to remove the cast if it will do none of those
            // things.

            // we are starting with code like `(X)expr` and converting to just `expr`. Post rewrite we need
            // to ensure that the final converted-type of `expr` matches the final converted type of `(X)expr`.
            var originalConvertedType = originalSemanticModel.GetTypeInfo(castNode.WalkUpParentheses(), cancellationToken).ConvertedType;
            if (originalConvertedType is null || originalConvertedType.TypeKind == TypeKind.Error)
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
            var isNullLiteralCast = IsNullLiteralCast(castedExpressionNode);
            if (isNullLiteralCast && !originalConvertedType.IsReferenceType && !originalConvertedType.IsNullable())
                return false;

            // So far, this looks potentially possible to remove.  Now, actually do the removal and get the
            // semantic model for the rewritten code so we can check it to make sure semantics were preserved.
            var (rewrittenSemanticModel, rewrittenExpression) = GetSemanticModelWithCastRemoved(
                castNode, castedExpressionNode, originalSemanticModel, cancellationToken);
            if (rewrittenSemanticModel is null || rewrittenExpression is null)
                return false;

            var (rewrittenConvertedType, rewrittenConversion) = GetRewrittenInfo(
                castNode, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken);
            if (rewrittenConvertedType is null || rewrittenConvertedType.TypeKind == TypeKind.Error)
                return false;

            // The final converted type may be the same even after removing the cast.  However, the cast may 
            // have been necessary to convert the type and/or value in a way that could be observable.  For example:
            //
            // object o1 = (long)expr; // or (long)0
            //
            // We need to keep the cast so that the stored value stays a 'long'.
            if (originalConversion.IsConstantExpression || originalConversion.IsNumeric || originalConversion.IsEnumeration)
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

            // Identity fp-casts can actually change the runtime value of the fp number.  This can happen because the
            // runtime is allowed to perform the operations with wider precision than the actual specified fp-precision.
            // i.e. 64-bit doubles can actually be 80 bits at runtime.  Even though the language considers this to be an
            // identity cast, we don't want to remove these because the user may be depending on that truncation.
            if (IsIdentityFloatingPointCastThatMustBePreserved(castNode, castedExpressionNode, originalSemanticModel, cancellationToken))
                return false;

            #endregion blacklist cases

            #region whitelist cases that allow this cast to be removed.

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
                if (IsComplimentaryMemberAccessAfterCastRemoval(
                        memberAccessExpression, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken))
                {
                    return true;
                }
            }

            // In code like `((X)y)()` the cast to (X) can be removed if this was an implicit reference conversion
            // to a complimentary delegate (because of delegate variance) *and* the return type of the delegate
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
                if (IsComplimentaryInvocationAfterCastRemoval(
                        invocationExpression, rewrittenExpression, originalSemanticModel, rewrittenSemanticModel, cancellationToken))
                {
                    return true;
                }
            }

            // If the types of the expressions are different, then removing the conversion changed semantics
            // and we can't remove it.
            if (SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(originalConvertedType, rewrittenConvertedType))
                return true;

            #endregion whitelist cases.

            return false;
        }

        private static bool IsExplicitCastThatMustBePreserved(ExpressionSyntax castNode, Conversion conversion)
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
            // removable if teh language would insert such a cast anyways (for things like `x ? (int?)0 : null`).  In C# 9
            // and above this will create a legal conditional conversion that implicitly adds that cast.
            //
            // Note: this does not apply for `as byte?`.  This is an explicit as-cast that can produce null values and
            // so it should be maintained.
            if (conversion.IsNullable && castNode is CastExpressionSyntax)
                return false;

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

            // It wasn't a read from a fp/field/array.  But it might be a write into one.

            castNode = castNode.WalkUpParentheses();
            if (castNode.Parent is AssignmentExpressionSyntax assignmentExpression &&
                assignmentExpression.Right == castNode)
            {
                // Identity fp conversion is safe if this is a write to a fp field/array
                if (IsFieldOrArrayElement(semanticModel, assignmentExpression.Left, cancellationToken))
                    return false;
            }
            else if (castNode.Parent.IsKind(SyntaxKind.ArrayInitializerExpression, out InitializerExpressionSyntax? arrayInitializer))
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

                // TODO(cyrusn): Do we need to validate the old symbol maps to the new symbol?
                // We could easily add that if necessary.
            }

            return false;
        }

        private static bool IsComplimentaryMemberAccessAfterCastRemoval(
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
            if (IsComplimentaryDelegateInvoke(originalMemberSymbol, rewrittenMemberSymbol))
                return true;

            // Ok, we had two good member symbols before/after the cast removal.  In other words we have:
            //
            //      ((X)expr).Y
            //      (expr).Y

            // Map the original member that was called over to the new compilation so we can do proper symbol
            // checks against it.
            originalMemberSymbol = originalMemberSymbol.GetSymbolKey(cancellationToken).Resolve(
                rewrittenSemanticModel.Compilation, cancellationToken: cancellationToken).Symbol;
            if (originalMemberSymbol is null)
                return false;

            // Next, see if this is a call to an interface method.
            if (originalMemberSymbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                var rewrittenType = rewrittenSemanticModel.GetTypeInfo(rewrittenExpression, cancellationToken).Type;
                if (rewrittenType is null or IErrorTypeSymbol)
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
                if (originalMemberSymbol.Equals(rewrittenMemberSymbol))
                    return true;

                // Ok, we have a type casted to an interface.  It may be safe to remove this interface cast
                // if we still call into the implementation of that interface member afterwards.  Note: the
                // type has to be sealed, otherwise the interface method may have been reimplemented lower
                // in the inheritance hierarchy.

                var isSealed =
                    rewrittenType.IsSealed ||
                    rewrittenType.IsValueType ||
                    rewrittenType.TypeKind == TypeKind.Array ||
                    IsIntrinsicOrEnum(rewrittenType);

                if (!isSealed)
                    return false;

                // Then look for the current implementation of that interface member.
                var rewrittenContainingType = rewrittenMemberSymbol.ContainingType;
                var implementationMember = rewrittenContainingType.FindImplementationForInterfaceMember(originalMemberSymbol);
                if (implementationMember is null)
                    return false;

                // if that's not the method we're currently calling, then this definitely isn't safe to remove.
                return implementationMember.Equals(rewrittenMemberSymbol) &&
                    ParameterNamesAndDefaultValuesAndReturnTypesMatch(originalMemberSymbol, rewrittenMemberSymbol);
            }

            // Second, check if this is a virtual call to a different location in the inheritance hierarchy.
            // Importantly though, because of covariant return types, we have to make sure the overrides 
            // agree on the return type, or else this could change the final type of hte expression.
            for (var current = rewrittenMemberSymbol; current != null; current = current.GetOverriddenMember())
            {
                if (SymbolEquivalenceComparer.Instance.Equals(originalMemberSymbol, current))
                {
                    // we're calling into a override of a higher up virtual in the original code.
                    // This is safe as long as the names of the parameters and all default values
                    // are the same.  This is because the compiler uses the names and default
                    // values of the overridden member, even though it emits a virtual call to the
                    // the highest in the inheritance chain.
                    return ParameterNamesAndDefaultValuesAndReturnTypesMatch(originalMemberSymbol, rewrittenMemberSymbol);
                }
            }

            return false;
        }

        private static bool IsComplimentaryInvocationAfterCastRemoval(
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

            return IsComplimentaryDelegateInvoke(originalMemberSymbol, rewrittenMemberSymbol);
        }

        private static bool IsComplimentaryDelegateInvoke(ISymbol originalMemberSymbol, ISymbol rewrittenMemberSymbol)
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
            }

            return false;
        }

        private static bool ParameterNamesAndDefaultValuesAndReturnTypesMatch(ISymbol originalMemberSymbol, ISymbol rewrittenMemberSymbol)
        {
            var originalMemberType = originalMemberSymbol.GetMemberType();
            var rewrittenMemberType = rewrittenMemberSymbol.GetMemberType();
            if (!Equals(originalMemberType, rewrittenMemberType))
                return false;

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
            ExpressionSyntax castNode, ExpressionSyntax rewrittenExpression,
            SemanticModel originalSemanticModel, SemanticModel rewrittenSemanticModel, CancellationToken cancellationToken)
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

            // Removing a cast may cause a conditional-expression conversion to come into existence.  This is
            // fine as long as we're in C# 9 or above.
            var languageVersion = ((CSharpCompilation)originalSemanticModel.Compilation).LanguageVersion;
            if (languageVersion < LanguageVersion.CSharp9 &&
                IntroducedConditionalExpressionConversion(rewrittenExpression, rewrittenSemanticModel, cancellationToken))
            {
                return default;
            }

            return (rewrittenSemanticModel, rewrittenExpression);
        }
    }
}
