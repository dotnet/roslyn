// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CodeGen;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class SynthesizedStringSwitchHashMethod : SynthesizedGlobalMethodSymbol
    {
        private const uint InitialHashValue = (uint)2166136261;
        private const int Multiplier = 16777619;

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
                hashCode = unchecked(InitialHashValue);

                int i = 0;
                goto start;

            again:
                hashCode = unchecked((text[i] ^ hashCode) * Multiplier);
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
                                F.Assignment(F.Local(hashCode), F.Literal(InitialHashValue)),
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
                                        F.Literal(Multiplier))),
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
        internal static BoundBlock ConstructSingleInvocationMethodBody(
            SyntheticBoundNodeFactory F,
            MethodSymbol methodToInvoke,
            bool useBaseReference)
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

    internal class SynthesizedAsIntValueMethod : SynthesizedGlobalMethodSymbol
    {
        private readonly Emit.PEModuleBuilder _module;

        public SynthesizedAsIntValueMethod(Emit.PEModuleBuilder module, PrivateImplementationDetails pi)
            : base(module.SourceModule, pi,
                   module.Compilation.GetSpecialType(SpecialType.System_Boolean),
                   PrivateImplementationDetails.AsIntValueName)
        {
            this._module = module;
            var compilation = _module.Compilation;
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(
                new SynthesizedParameterSymbol(this, compilation.GetSpecialType(SpecialType.System_Object), 0, RefKind.None, "o"),
                new SynthesizedParameterSymbol(this, compilation.GetSpecialType(SpecialType.System_Int32), 1, RefKind.Out, "value")
                ));
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            ///// <summary>
            ///// If o is of an integral numeric type and contains a value that is in the range of an int,
            ///// return true and set the value parameter to that value.
            ///// </summary>
            //internal static bool AsIntValue(object o, out int value)
            //{
            //    value = 0;
            //    if (o == null)
            //    {
            //        return false;
            //    }
            //    Type t = o.GetType();
            //    if (t == typeof(byte)) { value = (byte)o; return true; }
            //    if (t == typeof(sbyte)) { value = (sbyte)o; return true; }
            //    if (t == typeof(short)) { value = (short)o; return true; }
            //    if (t == typeof(ushort)) { value = (ushort)o; return true; }
            //    if (t == typeof(int)) { value = (int)o; return true; }
            //    if (t == typeof(long))
            //    {
            //        long l = (long)o;
            //        value = (int)l;
            //        return (l >= int.MinValue && l <= int.MaxValue);
            //    }
            //    if (t == typeof(uint))
            //    {
            //        uint ui = (uint)o;
            //        value = (int)ui;
            //        return (ui <= int.MaxValue);
            //    }
            //    if (t == typeof(ulong))
            //    {
            //        ulong ul = (ulong)o;
            //        value = (int)ul;
            //        return (ul <= int.MaxValue);
            //    }
            //    // not an integral type
            //    return false;
            //}
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(
                this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = this;

            var _byte = F.SpecialType(SpecialType.System_Byte);
            var _sbyte = F.SpecialType(SpecialType.System_SByte);
            var _short = F.SpecialType(SpecialType.System_Int16);
            var _ushort = F.SpecialType(SpecialType.System_UInt16);
            var _int = F.SpecialType(SpecialType.System_Int32);
            var _uint = F.SpecialType(SpecialType.System_UInt32);
            var _long = F.SpecialType(SpecialType.System_Int64);
            var _ulong = F.SpecialType(SpecialType.System_UInt64);
            var _bool = F.SpecialType(SpecialType.System_Boolean);

            var t = F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Type));
            var ui = F.SynthesizedLocal(_uint);
            var l = F.SynthesizedLocal(_long);
            var ul = F.SynthesizedLocal(_ulong);

            var o = this.Parameters[0];
            var value = this.Parameters[1];
            var object_getTypeMethod = F.WellKnownMethod(WellKnownMember.System_Object__GetType);

            var body = F.Block(ImmutableArray.Create(t),
                //    value = 0;
                F.Assignment(F.Parameter(value), F.Literal(0)),
                //    if (o == null) return false;
                F.If(F.ObjectEqual(F.Parameter(o), F.Null(o.Type)), F.Return(F.Literal(false))),
                //    Type t = o.GetType();
                F.Assignment(F.Local(t), F.Call(F.Parameter(o), object_getTypeMethod)),
                //    if (t == typeof(byte)) { value = (byte)o; return true; }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_byte)), F.Block(
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Convert(_byte, F.Parameter(o)))),
                    F.Return(F.Literal(true)))),
                //    if (t == typeof(sbyte)) { value = (sbyte)o; return true; }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_sbyte)), F.Block(
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Convert(_sbyte, F.Parameter(o)))),
                    F.Return(F.Literal(true)))),
                //    if (t == typeof(short)) { value = (short)o; return true; }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_short)), F.Block(
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Convert(_short, F.Parameter(o)))),
                    F.Return(F.Literal(true)))),
                //    if (t == typeof(ushort)) { value = (ushort)o; return true; }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_ushort)), F.Block(
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Convert(_ushort, F.Parameter(o)))),
                    F.Return(F.Literal(true)))),
                //    if (t == typeof(int)) { value = (int)o; return true; }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_int)), F.Block(
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Parameter(o))),
                    F.Return(F.Literal(true)))),
                //    if (t == typeof(long))
                //    {
                //        long l = (long)o;
                //        value = (int)l;
                //        return (l >= int.MinValue && l <= int.MaxValue);
                //    }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_long)), F.Block(ImmutableArray.Create(l),
                    F.Assignment(F.Local(l), F.Convert(_long, F.Parameter(o))),
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Local(l))),
                    F.Return(F.LogicalAnd(F.Binary(BinaryOperatorKind.LongGreaterThanOrEqual, _bool, F.Local(l), F.Literal((long)int.MinValue)),
                                            F.Binary(BinaryOperatorKind.LongLessThanOrEqual, _bool, F.Local(l), F.Literal((long)int.MaxValue))))
                    )),
                //    if (t == typeof(uint))
                //    {
                //        uint ui = (uint)o;
                //        value = (int)ui;
                //        return (ui <= int.MaxValue);
                //    }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_uint)), F.Block(ImmutableArray.Create(ui),
                    F.Assignment(F.Local(ui), F.Convert(_uint, F.Parameter(o))),
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Local(ui))),
                    F.Return(F.Binary(BinaryOperatorKind.UIntLessThanOrEqual, _bool, F.Local(ui), F.Literal((uint)int.MaxValue)))
                    )),
                //    if (t == typeof(ulong))
                //    {
                //        ulong ul = (ulong)o;
                //        value = (int)ul;
                //        return (ul <= int.MaxValue);
                //    }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_ulong)), F.Block(ImmutableArray.Create(ul),
                    F.Assignment(F.Local(ul), F.Convert(_ulong, F.Parameter(o))),
                    F.Assignment(F.Parameter(value), F.Convert(_int, F.Local(ul))),
                    F.Return(F.Binary(BinaryOperatorKind.ULongLessThanOrEqual, _bool, F.Local(ul), F.Literal((ulong)int.MaxValue)))
                    )),
                //    // not an integral type
                //    return false;
                F.Return(F.Literal(false))
                );

            // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
            F.CloseMethod(body);
        }
    }

    internal class SynthesizedAsLargePositiveMethod : SynthesizedGlobalMethodSymbol
    {
        private readonly Emit.PEModuleBuilder _module;

        public SynthesizedAsLargePositiveMethod(Emit.PEModuleBuilder module, PrivateImplementationDetails pi)
            : base(module.SourceModule, pi,
                   module.Compilation.GetSpecialType(SpecialType.System_Boolean),
                   PrivateImplementationDetails.AsLargePositiveName)
        {
            this._module = module;
            var compilation = module.Compilation;
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(
                new SynthesizedParameterSymbol(this, compilation.GetSpecialType(SpecialType.System_Object), 0, RefKind.None, "o"),
                new SynthesizedParameterSymbol(this, compilation.GetSpecialType(SpecialType.System_UInt64), 1, RefKind.Out, "value")
                ));
        }
        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            ///// <summary>
            ///// If o is of an integral numeric type and contains a value that is greater than any value of int,
            ///// return true and set the value parameter to that value.
            ///// </summary>
            //internal static bool AsLargePositive(object o, out ulong value)
            //{
            //    value = 0;
            //    if (o == null) return false;
            //    Type t = o.GetType();
            //    if (t == typeof(uint))
            //    {
            //        uint ui = (uint)o;
            //        value = (ulong)ui;
            //        return (ui > int.MaxValue);
            //    }
            //    if (t == typeof(long))
            //    {
            //        long l = (long)o;
            //        value = (ulong)l;
            //        return (l > int.MaxValue);
            //    }
            //    if (t == typeof(ulong))
            //    {
            //        value = (ulong)o;
            //        return (value > int.MaxValue);
            //    }
            //    return false;
            //}
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(
                this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = this;

            var _uint = F.SpecialType(SpecialType.System_UInt32);
            var _long = F.SpecialType(SpecialType.System_Int64);
            var _ulong = F.SpecialType(SpecialType.System_UInt64);
            var _bool = F.SpecialType(SpecialType.System_Boolean);

            var t = F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Type));
            var ui = F.SynthesizedLocal(_uint);
            var l = F.SynthesizedLocal(_long);

            var o = this.Parameters[0];
            var value = this.Parameters[1];
            var object_getTypeMethod = F.WellKnownMethod(WellKnownMember.System_Object__GetType);

            var body = F.Block(ImmutableArray.Create(t),
                //    value = 0;
                F.Assignment(F.Parameter(value), F.Literal(0UL)),
                //    if (o == null) return false;
                F.If(F.ObjectEqual(F.Parameter(o), F.Null(o.Type)), F.Return(F.Literal(false))),
                //    Type t = o.GetType();
                F.Assignment(F.Local(t), F.Call(F.Parameter(o), object_getTypeMethod)),
                //    if (t == typeof(uint))
                //    {
                //        uint ui = (uint)o;
                //        value = (ulong)ui;
                //        return (ui > int.MaxValue);
                //    }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_uint)), F.Block(ImmutableArray.Create(ui),
                    F.Assignment(F.Local(ui), F.Convert(_uint, F.Parameter(o))),
                    F.Assignment(F.Parameter(value), F.Convert(_ulong, F.Local(ui))),
                    F.Return(F.Binary(BinaryOperatorKind.UIntGreaterThan, _bool, F.Local(ui), F.Literal((uint)int.MaxValue)))
                    )),
                //    if (t == typeof(long))
                //    {
                //        long l = (long)o;
                //        value = (ulong)l;
                //        return (l > int.MaxValue);
                //    }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_long)), F.Block(ImmutableArray.Create(l),
                    F.Assignment(F.Local(l), F.Convert(_long, F.Parameter(o))),
                    F.Assignment(F.Parameter(value), F.Convert(_ulong, F.Local(l))),
                    F.Return(F.Binary(BinaryOperatorKind.LongGreaterThan, _bool, F.Local(l), F.Literal((long)int.MinValue)))
                    )),
                //    if (t == typeof(ulong))
                //    {
                //        value = (ulong)o;
                //        return (value > int.MaxValue);
                //    }
                F.If(F.ObjectEqual(F.Local(t), F.Typeof(_ulong)), F.Block(
                    F.Assignment(F.Parameter(value), F.Convert(_ulong, F.Parameter(o))),
                    F.Return(F.Binary(BinaryOperatorKind.ULongGreaterThan, _bool, F.Parameter(value), F.Literal((ulong)int.MaxValue)))
                    )),
                //    return false;
                F.Return(F.Literal(false))
                );

            // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
            F.CloseMethod(body);
        }
    }

    internal class SynthesizedAsLargeNegativeMethod : SynthesizedGlobalMethodSymbol
    {
        private readonly Emit.PEModuleBuilder _module;

        public SynthesizedAsLargeNegativeMethod(Emit.PEModuleBuilder module, PrivateImplementationDetails pi)
            : base(module.SourceModule, pi, module.Compilation.GetSpecialType(SpecialType.System_Boolean), PrivateImplementationDetails.AsLargeNegativeName)
        {
            this._module = module;
            var compilation = module.Compilation;
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(
                new SynthesizedParameterSymbol(this, compilation.GetSpecialType(SpecialType.System_Object), 0, RefKind.None, "o"),
                new SynthesizedParameterSymbol(this, compilation.GetSpecialType(SpecialType.System_Int64), 1, RefKind.Out, "value")
                ));
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            ///// <summary>
            ///// If o is of an integral numeric type and contains a value that is less than any value of int,
            ///// return true and set the value parameter to that value. Note that this can only occur when
            ///// o is of type long.
            ///// </summary>
            //internal static bool AsLargeNegative(object o, out long value)
            //{
            //    if (o == null || o.GetType() != typeof(long))
            //    {
            //        value = 0;
            //        return false;
            //    }
            //    value = (long)o;
            //    return value < int.MinValue;
            //}
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(
                this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = this;

            var _long = F.SpecialType(SpecialType.System_Int64);
            var _bool = F.SpecialType(SpecialType.System_Boolean);

            var o = this.Parameters[0];
            var value = this.Parameters[1];
            var object_getTypeMethod = F.WellKnownMethod(WellKnownMember.System_Object__GetType);

            var body = F.Block(
                //    if (o == null || o.GetType() != typeof(long))
                //    {
                //        value = 0;
                //        return false;
                //    }
                F.If(F.LogicalOr(F.ObjectEqual(F.Parameter(o), F.Null(o.Type)), F.ObjectEqual(F.Call(F.Parameter(o), object_getTypeMethod), F.Typeof(_long))),
                    F.Block(
                        F.Assignment(F.Parameter(value), F.Literal(0L))),
                        F.Return(F.Literal(false))),
                //    value = (long)o;
                F.Assignment(F.Parameter(value), F.Convert(_long, F.Parameter(o))),
                //    return value < int.MinValue;
                F.Return(F.Binary(BinaryOperatorKind.LongLessThan, _bool, F.Parameter(value), F.Literal((long)int.MinValue)))
                );

            // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
            F.CloseMethod(body);
        }
    }
}
