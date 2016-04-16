// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private void EmitConversionExpression(BoundConversion conversion, bool used)
        {
            switch (conversion.ConversionKind)
            {
                case ConversionKind.MethodGroup:
                    EmitMethodGroupConversion(conversion, used);
                    return;
                case ConversionKind.NullToPointer:
                    // The null pointer is represented as 0u.
                    _builder.EmitIntConstant(0);
                    _builder.EmitOpCode(ILOpCode.Conv_u);
                    EmitPopIfUnused(used);
                    return;
            }

            if (!used && !conversion.ConversionHasSideEffects())
            {
                EmitExpression(conversion.Operand, false); // just do expr side effects
                return;
            }

            EmitExpression(conversion.Operand, true);
            EmitConversion(conversion);

            EmitPopIfUnused(used);
        }

        private void EmitConversion(BoundConversion conversion)
        {
            switch (conversion.ConversionKind)
            {
                case ConversionKind.Identity:
                    EmitIdentityConversion(conversion);
                    break;
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                    EmitNumericConversion(conversion);
                    break;
                case ConversionKind.ImplicitReference:
                case ConversionKind.Boxing:
                    // from IL prospective ImplicitReference and Boxing conversions are the same thing.
                    // both force operand to be an object (O) - which may involve boxing 
                    // and then assume that result has the target type - which may involve unboxing.
                    EmitImplicitReferenceConversion(conversion);
                    break;
                case ConversionKind.ExplicitReference:
                case ConversionKind.Unboxing:
                    // from IL prospective ExplicitReference and UnBoxing conversions are the same thing.
                    // both force operand to be an object (O) - which may involve boxing 
                    // and then reinterpret result as the target type - which may involve unboxing.
                    EmitExplicitReferenceConversion(conversion);
                    break;
                case ConversionKind.ImplicitEnumeration:
                case ConversionKind.ExplicitEnumeration:
                    EmitEnumConversion(conversion);
                    break;
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.AnonymousFunction:
                case ConversionKind.MethodGroup:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ExplicitDynamic:
                    // None of these things should reach codegen (yet? maybe?)
                    throw ExceptionUtilities.UnexpectedValue(conversion.ConversionKind);
                case ConversionKind.PointerToVoid:
                case ConversionKind.PointerToPointer:
                    return; //no-op since they all have the same runtime representation
                case ConversionKind.PointerToInteger:
                case ConversionKind.IntegerToPointer:
                    var fromType = conversion.Operand.Type;
                    var fromPredefTypeKind = fromType.PrimitiveTypeCode;

                    var toType = conversion.Type;
                    var toPredefTypeKind = toType.PrimitiveTypeCode;

#if DEBUG
                    switch (fromPredefTypeKind)
                    {
                        case Microsoft.Cci.PrimitiveTypeCode.IntPtr:
                        case Microsoft.Cci.PrimitiveTypeCode.UIntPtr:
                        case Microsoft.Cci.PrimitiveTypeCode.Pointer:
                            Debug.Assert(toPredefTypeKind.IsNumeric());
                            break;
                        default:
                            Debug.Assert(fromPredefTypeKind.IsNumeric());
                            Debug.Assert(
                                toPredefTypeKind == Microsoft.Cci.PrimitiveTypeCode.IntPtr ||
                                toPredefTypeKind == Microsoft.Cci.PrimitiveTypeCode.UIntPtr ||
                                toPredefTypeKind == Microsoft.Cci.PrimitiveTypeCode.Pointer);
                            break;
                    }
#endif

                    _builder.EmitNumericConversion(fromPredefTypeKind, toPredefTypeKind, conversion.Checked);
                    break;
                case ConversionKind.NullToPointer:
                    throw ExceptionUtilities.UnexpectedValue(conversion.ConversionKind); // Should be handled by caller.
                case ConversionKind.ImplicitNullable:
                case ConversionKind.ExplicitNullable:
                default:
                    throw ExceptionUtilities.UnexpectedValue(conversion.ConversionKind);
            }
        }

        private void EmitIdentityConversion(BoundConversion conversion)
        {
            // An _explicit_ identity conversion from double to double or float to float on
            // non-constants must stay as a conversion. An _implicit_ identity conversion can be
            // optimized away.  Why? Because (double)d1 + d2 has different semantics than d1 + d2.
            // The former rounds off to 64 bit precision; the latter is permitted to use higher
            // precision math if d1 is enregistered.

            if (conversion.ExplicitCastInCode)
            {
                switch (conversion.Type.PrimitiveTypeCode)
                {
                    case Microsoft.Cci.PrimitiveTypeCode.Float32:
                    case Microsoft.Cci.PrimitiveTypeCode.Float64:
                        // For explicitly-written "identity conversions" from float to float or
                        // double to double, we require the generation of conv.r4 or conv.r8. The
                        // runtime can use these instructions to truncate precision, and csc.exe
                        // generates them. It's not ideal, we should consider the possibility of not
                        // doing this or marking somewhere else that this is necessary.

                        // Don't need to do this for constants, however.
                        if (conversion.Operand.ConstantValue == null)
                        {
                            EmitNumericConversion(conversion);
                        }
                        break;
                }
            }
        }

        private void EmitNumericConversion(BoundConversion conversion)
        {
            var fromType = conversion.Operand.Type;
            var fromPredefTypeKind = fromType.PrimitiveTypeCode;
            Debug.Assert(fromPredefTypeKind.IsNumeric());

            var toType = conversion.Type;
            var toPredefTypeKind = toType.PrimitiveTypeCode;
            Debug.Assert(toPredefTypeKind.IsNumeric());

            _builder.EmitNumericConversion(fromPredefTypeKind, toPredefTypeKind, conversion.Checked);
        }

        private void EmitImplicitReferenceConversion(BoundConversion conversion)
        {
            // turn operand into an O(operandType)
            // if the operand is already verifiably an O, we can use it as-is
            // otherwise we need to box it, so that verifier will start tracking an O
            if (!conversion.Operand.Type.IsVerifierReference())
            {
                EmitBox(conversion.Operand.Type, conversion.Operand.Syntax);
            }

            // here we have O(operandType) that must be compatible with O(targetType)
            //
            // if target type is verifiably a reference type, we can leave the value as-is otherwise
            // we need to unbox to targetType to keep verifier happy.
            if (!conversion.Type.IsVerifierReference())
            {
                _builder.EmitOpCode(ILOpCode.Unbox_any);
                EmitSymbolToken(conversion.Type, conversion.Syntax);
            }

            return;
        }

        private void EmitExplicitReferenceConversion(BoundConversion conversion)
        {
            // turn operand into an O(operandType)
            // if the operand is already verifiably an O, we can use it as-is
            // otherwise we need to box it, so that verifier will start tracking an O
            if (!conversion.Operand.Type.IsVerifierReference())
            {
                EmitBox(conversion.Operand.Type, conversion.Operand.Syntax);
            }

            // here we have O(operandType) that could be compatible with O(targetType)
            // 
            // if target type is verifiably a reference type, we can just do a type check otherwise
            // we unbox which will both do the type check and start tracking actual target type in
            // verifier.
            if (conversion.Type.IsVerifierReference())
            {
                _builder.EmitOpCode(ILOpCode.Castclass);
                EmitSymbolToken(conversion.Type, conversion.Syntax);
            }
            else
            {
                _builder.EmitOpCode(ILOpCode.Unbox_any);
                EmitSymbolToken(conversion.Type, conversion.Syntax);
            }
        }

        private void EmitEnumConversion(BoundConversion conversion)
        {
            // Nullable enumeration conversions should have already been lowered into
            // implicit or explicit nullable conversions.
            Debug.Assert(!conversion.Type.IsNullableType());

            var fromType = conversion.Operand.Type;
            if (fromType.IsEnumType())
            {
                fromType = ((NamedTypeSymbol)fromType).EnumUnderlyingType;
            }

            var fromPredefTypeKind = fromType.PrimitiveTypeCode;
            Debug.Assert(fromPredefTypeKind.IsNumeric());

            var toType = conversion.Type;
            if (toType.IsEnumType())
            {
                toType = ((NamedTypeSymbol)toType).EnumUnderlyingType;
            }

            var toPredefTypeKind = toType.PrimitiveTypeCode;
            Debug.Assert(toPredefTypeKind.IsNumeric());

            _builder.EmitNumericConversion(fromPredefTypeKind, toPredefTypeKind, conversion.Checked);
        }

        private void EmitDelegateCreation(BoundExpression node, BoundExpression receiver, bool isExtensionMethod, MethodSymbol method, TypeSymbol delegateType, bool used)
        {
            var isStatic = receiver == null || (!isExtensionMethod && method.IsStatic);
            if (!used)
            {
                if (!isStatic)
                {
                    EmitExpression(receiver, false);
                }

                return;
            }

            // emit the receiver
            if (isStatic)
            {
                _builder.EmitNullConstant();
            }
            else
            {
                EmitExpression(receiver, true);
                if (!receiver.Type.IsVerifierReference())
                {
                    EmitBox(receiver.Type, receiver.Syntax);
                }
            }

            // emit method pointer

            // Metadata Spec (II.14.6):
            //   Delegates shall be declared sealed.
            //   The Invoke method shall be virtual.
            if (method.IsMetadataVirtual() && !method.ContainingType.IsDelegateType() && !receiver.SuppressVirtualCalls)
            {
                // NOTE: method.IsMetadataVirtual -> receiver != null
                _builder.EmitOpCode(ILOpCode.Dup);
                _builder.EmitOpCode(ILOpCode.Ldvirtftn);

                //  substitute the method with original virtual method
                method = method.GetConstructedLeastOverriddenMethod(_method.ContainingType);
            }
            else
            {
                _builder.EmitOpCode(ILOpCode.Ldftn);
            }

            EmitSymbolToken(method, node.Syntax, null);

            // call delegate constructor
            _builder.EmitOpCode(ILOpCode.Newobj, -1); // pop 2 args and push delegate object

            var ctor = DelegateConstructor(node.Syntax, delegateType);
            if ((object)ctor != null) EmitSymbolToken(ctor, node.Syntax, null);
        }

        private MethodSymbol DelegateConstructor(CSharpSyntaxNode syntax, TypeSymbol delegateType)
        {
            foreach (var possibleCtor in delegateType.GetMembers(WellKnownMemberNames.InstanceConstructorName))
            {
                var m = possibleCtor as MethodSymbol;
                if ((object)m == null) continue;
                var parameters = m.Parameters;
                if (parameters.Length != 2) continue;
                if (parameters[0].Type.SpecialType != SpecialType.System_Object) continue;
                var p1t = parameters[1].Type.SpecialType;
                if (p1t == SpecialType.System_IntPtr || p1t == SpecialType.System_UIntPtr)
                {
                    return m;
                }
            }

            // The delegate '{0}' does not have a valid constructor
            _diagnostics.Add(ErrorCode.ERR_BadDelegateConstructor, syntax.Location, delegateType);
            return null;
        }

        private void EmitMethodGroupConversion(BoundConversion conversion, bool used)
        {
            var group = (BoundMethodGroup)conversion.Operand;
            EmitDelegateCreation(conversion, group.InstanceOpt, conversion.IsExtensionMethod, conversion.SymbolOpt, conversion.Type, used);
        }
    }
}
