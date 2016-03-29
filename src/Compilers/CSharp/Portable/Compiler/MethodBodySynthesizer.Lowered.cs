// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Microsoft.CodeAnalysis.Text;

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

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = this;

            try
            {
                LocalSymbol i = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32));
                LocalSymbol hashCode = F.SynthesizedLocal(F.SpecialType(SpecialType.System_UInt32));

                LabelSymbol again = F.GenerateLabel("again");
                LabelSymbol start = F.GenerateLabel("start");

                ParameterSymbol text = this.Parameters[0];

                //  This method should be kept consistent with ComputeStringHash

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
                                                ConversionKind.ImplicitNumeric),
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

    internal sealed partial class SynthesizedExplicitImplementationForwardingMethod : SynthesizedImplementationMethod
    {
        internal override bool SynthesizesLoweredBoundBody
        {
            get { return true; }
        }

        /// <summary>
        /// Given a SynthesizedExplicitImplementationMethod (effectively a tuple (interface method, implementing method, implementing type)),
        /// construct a BoundBlock body.  Consider the tuple (Interface.Foo, Base.Foo, Derived).  The generated method will look like:
        /// 
        /// R Interface.Foo&lt;T1, T2, ...&gt;(A1 a1, A2 a2, ...)
        /// {
        ///     //don't return the output if the return type is void
        ///     return this.Foo&lt;T1, T2, ...&gt;(a1, a2, ...);
        /// }
        /// </summary>
        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = (MethodSymbol)this.OriginalDefinition;

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

    internal sealed partial class SynthesizedSealedPropertyAccessor : SynthesizedInstanceMethodSymbol
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
        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = (MethodSymbol)this.OriginalDefinition;

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
    }

    internal abstract partial class MethodToClassRewriter
    {
        private sealed partial class BaseMethodWrapperSymbol : SynthesizedMethodBaseSymbol
        {
            internal sealed override bool GenerateDebugInfo
            {
                get { return false; }
            }

            internal override bool SynthesizesLoweredBoundBody
            {
                get { return true; }
            }

            /// <summary>
            /// Given a SynthesizedSealedPropertyAccessor (an accessor with a reference to the accessor it overrides),
            /// construct a BoundBlock body.
            /// </summary>
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
                F.CurrentMethod = this.OriginalDefinition;

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
    internal static partial class MethodBodySynthesizer
    {
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
            //var refKindBuilder = ArrayBuilder<RefKind>.GetInstance();

            foreach (var param in F.CurrentMethod.Parameters)
            {
                argBuilder.Add(F.Parameter(param));
                //refKindBuilder.Add(param.RefKind);
            }

            BoundExpression invocation = F.Call(useBaseReference ? (BoundExpression)F.Base() : F.This(),
                                                methodToInvoke,
                                                argBuilder.ToImmutableAndFree());

            return F.CurrentMethod.ReturnsVoid
                        ? F.Block(F.ExpressionStatement(invocation), F.Return())
                        : F.Block(F.Return(invocation));
        }
    }
}
