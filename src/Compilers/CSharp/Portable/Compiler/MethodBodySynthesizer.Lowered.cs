// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class SynthesizedStringSwitchHashMethod : SynthesizedGlobalMethodSymbol
    {
        /// <summary>
        /// Compute the hashcode of a sub string using FNV-1a
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <remarks>
        /// This method should be kept consistent with MethodBodySynthesizer.ConstructStringSwitchHashFunctionBody
        /// The control flow in this method mimics lowered "for" loop. It is exactly what we want to emit
        /// to ensure that JIT can do range check hoisting.
        /// </remarks>
        internal static uint ComputeStringHash(string text)
        {
            uint hashCode = 0;
            if (text != null)
            {
                hashCode = unchecked((uint)2166136261);

                int i = 0;
                goto start;

again:
                hashCode = unchecked((text[i] ^ hashCode) * 16777619);
                i = i + 1;

start:
                if (i < text.Length)
                    goto again;
            }
            return hashCode;
        }

        /// <remarks>
        /// This method should be kept consistent with <see cref="ComputeStringHash"/>
        /// </remarks>
        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            try
            {
                LocalSymbol i = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32));
                LocalSymbol hashCode = F.SynthesizedLocal(F.SpecialType(SpecialType.System_UInt32));

                LabelSymbol again = F.GenerateLabel("again");
                LabelSymbol start = F.GenerateLabel("start");

                ParameterSymbol text = this.Parameters[0];

                //uint hashCode = 0;
                //if (text != null)
                //{
                //    hashCode = unchecked((uint)2166136261);

                //    int i = 0;
                //    goto start;

                //again:
                //    hashCode = (text[i] ^ hashCode) * 16777619;
                //    i = i + 1;

                //start:
                //    if (i < text.Length)
                //        goto again;

                //}
                //return hashCode;

                var body = F.Block(
                        ImmutableArray.Create<LocalSymbol>(hashCode, i),
                        F.If(
                            F.Binary(BinaryOperatorKind.ObjectNotEqual, F.SpecialType(SpecialType.System_Boolean),
                                F.Parameter(text),
                                F.Null(text.Type)),
                            F.Block(
                                F.Assignment(F.Local(hashCode), F.Literal((uint)2166136261)),
                                F.Assignment(F.Local(i), F.Literal(0)),
                                F.Goto(start),
                                F.Label(again),
                                F.Assignment(
                                    F.Local(hashCode),
                                    F.Binary(BinaryOperatorKind.Multiplication, hashCode.Type,
                                        F.Binary(BinaryOperatorKind.Xor, hashCode.Type,
                                            F.Convert(hashCode.Type,
                                                F.Call(
                                                    F.Parameter(text),
                                                    F.SpecialMethod(SpecialMember.System_String__Chars),
                                                    F.Local(i)),
                                                Conversion.ImplicitNumeric),
                                            F.Local(hashCode)),
                                        F.Literal(16777619))),
                                F.Assignment(
                                    F.Local(i),
                                    F.Binary(BinaryOperatorKind.Addition, i.Type,
                                        F.Local(i),
                                        F.Literal(1))),
                                F.Label(start),
                                F.If(
                                    F.Binary(BinaryOperatorKind.LessThan, F.SpecialType(SpecialType.System_Boolean),
                                        F.Local(i),
                                        F.Call(F.Parameter(text), F.SpecialMethod(SpecialMember.System_String__Length))),
                                    F.Goto(again)))),
                        F.Return(F.Local(hashCode))
                    );

                // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
                F.CloseMethod(body);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }

    /// <summary>
    /// The synthesized method for computing the hash from a ReadOnlySpan&lt;char&gt; or Span&lt;char&gt;.
    /// Matches the corresponding method for string <see cref="SynthesizedStringSwitchHashMethod"/>.
    /// </summary>
    internal sealed partial class SynthesizedSpanSwitchHashMethod : SynthesizedGlobalMethodSymbol
    {
        /// <remarks>
        /// This method should be kept consistent with <see cref="SynthesizedStringSwitchHashMethod.ComputeStringHash"/>
        /// </remarks>
        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            try
            {
                ParameterSymbol text = this.Parameters[0];

                NamedTypeSymbol spanChar = F.WellKnownType(_isReadOnlySpan
                    ? WellKnownType.System_ReadOnlySpan_T
                    : WellKnownType.System_Span_T)
                    .Construct(F.SpecialType(SpecialType.System_Char));

                LocalSymbol i = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32));
                LocalSymbol hashCode = F.SynthesizedLocal(F.SpecialType(SpecialType.System_UInt32));

                LabelSymbol again = F.GenerateLabel("again");
                LabelSymbol start = F.GenerateLabel("start");

                //  uint hashCode = unchecked((uint)2166136261);

                //  int i = 0;
                //  goto start;

                //again:
                //  hashCode = (text[i] ^ hashCode) * 16777619;
                //  i = i + 1;

                //start:
                //  if (i < text.Length)
                //      goto again;

                //  return hashCode;

                var body = F.Block(
                        ImmutableArray.Create<LocalSymbol>(hashCode, i),
                        F.Assignment(F.Local(hashCode), F.Literal((uint)2166136261)),
                        F.Assignment(F.Local(i), F.Literal(0)),
                        F.Goto(start),
                        F.Label(again),
                        F.Assignment(
                            F.Local(hashCode),
                            F.Binary(BinaryOperatorKind.Multiplication, hashCode.Type,
                                F.Binary(BinaryOperatorKind.Xor, hashCode.Type,
                                    F.Convert(hashCode.Type,
                                        F.Call(
                                            F.Parameter(text),
                                            F.WellKnownMethod(_isReadOnlySpan
                                                ? WellKnownMember.System_ReadOnlySpan_T__get_Item
                                                : WellKnownMember.System_Span_T__get_Item).AsMember(spanChar),
                                            F.Local(i)),
                                        Conversion.ImplicitNumeric),
                                    F.Local(hashCode)),
                                F.Literal(16777619))),
                        F.Assignment(
                            F.Local(i),
                            F.Binary(BinaryOperatorKind.Addition, i.Type,
                                F.Local(i),
                                F.Literal(1))),
                        F.Label(start),
                        F.If(
                            F.Binary(BinaryOperatorKind.LessThan, F.SpecialType(SpecialType.System_Boolean),
                                F.Local(i),
                                F.Call(
                                    F.Parameter(text),
                                    F.WellKnownMethod(_isReadOnlySpan
                                        ? WellKnownMember.System_ReadOnlySpan_T__get_Length
                                        : WellKnownMember.System_Span_T__get_Length).AsMember(spanChar))),
                            F.Goto(again)),
                        F.Return(F.Local(hashCode))
                    );

                // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
                F.CloseMethod(body);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }

    internal sealed partial class SynthesizedExplicitImplementationForwardingMethod : SynthesizedImplementationMethod
    {
        internal override bool SynthesizesLoweredBoundBody
        {
            get { return true; }
        }

        public override bool IsAsync => false;
        internal override MethodImplAttributes ImplementationAttributes => default;

        /// <summary>
        /// Given a SynthesizedExplicitImplementationMethod (effectively a tuple (interface method, implementing method, implementing type)),
        /// construct a BoundBlock body.  Consider the tuple (Interface.Goo, Base.Goo, Derived).  The generated method will look like:
        /// 
        /// R Interface.Goo&lt;T1, T2, ...&gt;(A1 a1, A2 a2, ...)
        /// {
        ///     //don't return the output if the return type is void
        ///     return this.Goo&lt;T1, T2, ...&gt;(a1, a2, ...);
        /// }
        /// </summary>
        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = (MethodSymbol)this.OriginalDefinition;

            try
            {
                MethodSymbol methodToInvoke =
                    this.IsGenericMethod ?
                        this.ImplementingMethod.Construct(this.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>()) :
                        this.ImplementingMethod;

                F.CloseMethod(MethodBodySynthesizer.ConstructSingleInvocationMethodBody(F, methodToInvoke, useBaseReference: false));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }

    internal sealed partial class SynthesizedSealedPropertyAccessor : SynthesizedMethodSymbol
    {
        internal override bool SynthesizesLoweredBoundBody
        {
            get { return true; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Given a SynthesizedSealedPropertyAccessor (an accessor with a reference to the accessor it overrides),
        /// construct a BoundBlock body.
        /// </summary>
        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = (MethodSymbol)this.OriginalDefinition;

            try
            {
                F.CloseMethod(MethodBodySynthesizer.ConstructSingleInvocationMethodBody(F, this.OverriddenAccessor, useBaseReference: true));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();
    }

    internal abstract partial class MethodToClassRewriter
    {
        internal sealed partial class BaseMethodWrapperSymbol : SynthesizedMethodBaseSymbol
        {
            internal sealed override bool GenerateDebugInfo
            {
                get { return false; }
            }

            internal override bool SynthesizesLoweredBoundBody
            {
                get { return true; }
            }

            internal override ExecutableCodeBinder? TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false) => throw ExceptionUtilities.Unreachable();

            /// <summary>
            /// Given a SynthesizedSealedPropertyAccessor (an accessor with a reference to the accessor it overrides),
            /// construct a BoundBlock body.
            /// </summary>
            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
            {
                SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
                F.CurrentFunction = this.OriginalDefinition;

                try
                {
                    MethodSymbol methodBeingWrapped = this.BaseMethod;

                    if (this.Arity > 0)
                    {
                        Debug.Assert(this.Arity == methodBeingWrapped.Arity);
                        methodBeingWrapped = methodBeingWrapped.ConstructedFrom.Construct(StaticCast<TypeSymbol>.From(this.TypeParameters));
                    }

                    BoundBlock body = MethodBodySynthesizer.ConstructSingleInvocationMethodBody(F, methodBeingWrapped, useBaseReference: true);
                    if (body.Kind != BoundKind.Block) body = F.Block(body);
                    F.CompilationState.AddMethodWrapper(methodBeingWrapped, this, body);
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    diagnostics.Add(ex.Diagnostic);
                }
            }
        }
    }

    /// <summary>
    /// Contains methods related to synthesizing bound nodes in lowered form 
    /// that does not need any processing before passing to codegen
    /// </summary>
    internal static class MethodBodySynthesizer
    {
        public const int HASH_FACTOR = -1521134295; // (int)0xa5555529

        public static BoundExpression GenerateHashCombine(
            BoundExpression currentHashValue,
            MethodSymbol system_Collections_Generic_EqualityComparer_T__GetHashCode,
            MethodSymbol system_Collections_Generic_EqualityComparer_T__get_Default,
            ref BoundLiteral? boundHashFactor,
            BoundExpression valueToHash,
            SyntheticBoundNodeFactory F)
        {
            TypeSymbol system_Int32 = currentHashValue.Type!;
            Debug.Assert(system_Int32.SpecialType == SpecialType.System_Int32);

            //  bound HASH_FACTOR
            boundHashFactor ??= F.Literal(HASH_FACTOR);

            // Generate 'currentHashValue' <= 'currentHashValue * HASH_FACTOR 
            currentHashValue = F.Binary(BinaryOperatorKind.IntMultiplication, system_Int32, currentHashValue, boundHashFactor);

            // Generate 'currentHashValue' <= 'currentHashValue + EqualityComparer<valueToHash type>.Default.GetHashCode(valueToHash)'
            currentHashValue = F.Binary(BinaryOperatorKind.IntAddition,
                                     system_Int32,
                                     currentHashValue,
                                     GenerateGetHashCode(system_Collections_Generic_EqualityComparer_T__GetHashCode, system_Collections_Generic_EqualityComparer_T__get_Default, valueToHash, F));
            return currentHashValue;
        }

        public static BoundCall GenerateGetHashCode(
            MethodSymbol system_Collections_Generic_EqualityComparer_T__GetHashCode,
            MethodSymbol system_Collections_Generic_EqualityComparer_T__get_Default,
            BoundExpression valueToHash,
            SyntheticBoundNodeFactory F)
        {
            // Prepare constructed symbols
            NamedTypeSymbol equalityComparerType = system_Collections_Generic_EqualityComparer_T__GetHashCode.ContainingType;
            NamedTypeSymbol constructedEqualityComparer = equalityComparerType.Construct(valueToHash.Type);

            return F.Call(F.StaticCall(constructedEqualityComparer,
                                       system_Collections_Generic_EqualityComparer_T__get_Default.AsMember(constructedEqualityComparer)),
                          system_Collections_Generic_EqualityComparer_T__GetHashCode.AsMember(constructedEqualityComparer),
                          valueToHash);
        }

        /// <summary>
        /// Given a set of fields, produce an expression that is true when all of the given fields on
        /// `this` are equal to the fields on <paramref name="otherReceiver" /> according to the
        /// default EqualityComparer.
        /// </summary>
        public static BoundExpression GenerateFieldEquals(
            BoundExpression? initialExpression,
            BoundExpression otherReceiver,
            ArrayBuilder<FieldSymbol> fields,
            SyntheticBoundNodeFactory F)
        {
            Debug.Assert(fields.Count > 0);

            //  Expression:
            //
            //      System.Collections.Generic.EqualityComparer<T_1>.Default.Equals(this.backingFld_1, value.backingFld_1)
            //      ...
            //      && System.Collections.Generic.EqualityComparer<T_N>.Default.Equals(this.backingFld_N, value.backingFld_N)

            //  prepare symbols
            var equalityComparer_get_Default = F.WellKnownMethod(
                WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            var equalityComparer_Equals = F.WellKnownMethod(
                WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals);

            NamedTypeSymbol equalityComparerType = equalityComparer_Equals.ContainingType;

            BoundExpression? retExpression = initialExpression;

            // Compare fields
            foreach (var field in fields)
            {
                // Prepare constructed comparer
                var constructedEqualityComparer = equalityComparerType.Construct(field.Type);

                // System.Collections.Generic.EqualityComparer<T_index>.
                //   Default.Equals(this.backingFld_index, local.backingFld_index)'
                BoundExpression nextEquals = F.Call(
                    F.StaticCall(constructedEqualityComparer,
                                 equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                    equalityComparer_Equals.AsMember(constructedEqualityComparer),
                    F.Field(F.This(), field),
                    F.Field(otherReceiver, field));

                // Generate 'retExpression' = 'retExpression && nextEquals'
                retExpression = retExpression is null
                    ? nextEquals
                    : F.LogicalAnd(retExpression, nextEquals);
            }

            RoslynDebug.AssertNotNull(retExpression);

            return retExpression;
        }

        /// <summary>
        /// Construct a body for a method containing a call to a single other method with the same signature (modulo name).
        /// </summary>
        /// <param name="F">Bound node factory.</param>
        /// <param name="methodToInvoke">Method to invoke in constructed body.</param>
        /// <param name="useBaseReference">True for "base.", false for "this.".</param>
        /// <returns>Body for implementedMethod.</returns>
        internal static BoundBlock ConstructSingleInvocationMethodBody(SyntheticBoundNodeFactory F, MethodSymbol methodToInvoke, bool useBaseReference)
        {
            var argBuilder = ArrayBuilder<BoundExpression>.GetInstance();

            RoslynDebug.AssertNotNull(F.CurrentFunction);
            foreach (var param in F.CurrentFunction.Parameters)
            {
                argBuilder.Add(F.Parameter(param));
            }

            BoundExpression invocation = F.Call(methodToInvoke.IsStatic ? null : (useBaseReference ? (BoundExpression)F.Base(baseType: methodToInvoke.ContainingType) : F.This()),
                                                methodToInvoke,
                                                argBuilder.ToImmutableAndFree());

            return F.CurrentFunction.ReturnsVoid
                        ? F.Block(F.ExpressionStatement(invocation), F.Return())
                        : F.Block(F.Return(invocation));
        }
    }
}
