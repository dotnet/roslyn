// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private void EmitStackAlloc(TypeSymbol type, BoundArrayInitialization? inits, BoundExpression count)
        {
            if (inits is null)
            {
                emitLocalloc();
                return;
            }

            Debug.Assert(type is PointerTypeSymbol || type is NamedTypeSymbol);
            Debug.Assert(_diagnostics.DiagnosticBag is not null);

            var elementType = (type.TypeKind == TypeKind.Pointer
                ? ((PointerTypeSymbol)type).PointedAtTypeWithAnnotations
                : ((NamedTypeSymbol)type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0]).Type;

            var initExprs = inits.Initializers;

            var initializationStyle = ShouldEmitBlockInitializerForStackAlloc(elementType, initExprs);
            if (initializationStyle == ArrayInitializerStyle.Element)
            {
                emitLocalloc();
                EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: true);
            }
            else
            {
                bool mixedInitialized = false;

                emitLocalloc();

                var sizeInBytes = elementType.EnumUnderlyingTypeOrSelf().SpecialType.SizeInBytes();

                ImmutableArray<byte> data = GetRawData(initExprs);
                if (data.All(static (d, first) => d == first, data[0]))
                {
                    // All bytes are the same, no need for metadata blob, just initblk to fill it with the repeated value.
                    _builder.EmitOpCode(ILOpCode.Dup);
                    _builder.EmitIntConstant(data[0]);
                    _builder.EmitIntConstant(data.Length);
                    _builder.EmitOpCode(ILOpCode.Initblk, -3);
                }
                else if (sizeInBytes == 1)
                {
                    // Initialize the stackalloc by copying the data from a metadata blob
                    var field = _builder.module.GetFieldForData(data, alignment: 1, inits.Syntax, _diagnostics.DiagnosticBag);
                    _builder.EmitOpCode(ILOpCode.Dup);
                    _builder.EmitOpCode(ILOpCode.Ldsflda);
                    _builder.EmitToken(field, inits.Syntax);
                    _builder.EmitIntConstant(data.Length);
                    _builder.EmitUnaligned(alignment: 1);
                    _builder.EmitOpCode(ILOpCode.Cpblk, -3);
                }
                else
                {
                    var syntaxNode = inits.Syntax;
                    if (Binder.GetWellKnownTypeMember(_module.Compilation, WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle, _diagnostics, syntax: syntaxNode, isOptional: true) is MethodSymbol createSpanHelper &&
                        Binder.GetWellKnownTypeMember(_module.Compilation, WellKnownMember.System_ReadOnlySpan_T__get_Item, _diagnostics, syntax: syntaxNode, isOptional: true) is MethodSymbol spanGetItemDefinition)
                    {
                        // Use RuntimeHelpers.CreateSpan and cpblk.
                        var readOnlySpan = spanGetItemDefinition.ContainingType.Construct(elementType);
                        Debug.Assert(TypeSymbol.Equals(readOnlySpan.OriginalDefinition, _module.Compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.ConsiderEverything));
                        var spanGetItem = spanGetItemDefinition.AsMember(readOnlySpan);

                        _builder.EmitOpCode(ILOpCode.Dup);

                        // ldtoken <PrivateImplementationDetails>...
                        // call ReadOnlySpan<elementType> RuntimeHelpers::CreateSpan<elementType>(fldHandle)
                        var field = _builder.module.GetFieldForData(data, alignment: (ushort)sizeInBytes, syntaxNode, _diagnostics.DiagnosticBag);
                        _builder.EmitOpCode(ILOpCode.Ldtoken);
                        _builder.EmitToken(field, syntaxNode);
                        _builder.EmitOpCode(ILOpCode.Call, 0);
                        var createSpanHelperReference = createSpanHelper.Construct(elementType).GetCciAdapter();
                        _builder.EmitToken(createSpanHelperReference, syntaxNode);

                        var temp = AllocateTemp(readOnlySpan, syntaxNode);
                        _builder.EmitLocalStore(temp);
                        _builder.EmitLocalAddress(temp);

                        // span.get_Item[0]
                        _builder.EmitIntConstant(0);
                        _builder.EmitOpCode(ILOpCode.Call, -1);
                        EmitSymbolToken(spanGetItem, syntaxNode, optArgList: null);

                        _builder.EmitIntConstant(data.Length);
                        if (sizeInBytes != 8)
                        {
                            _builder.EmitUnaligned((sbyte)sizeInBytes);
                        }
                        _builder.EmitOpCode(ILOpCode.Cpblk, -3);

                        FreeTemp(temp);
                    }
                    else
                    {
                        EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: true);
                        mixedInitialized = true;
                    }
                }

                if (initializationStyle == ArrayInitializerStyle.Mixed && !mixedInitialized)
                {
                    EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: false);
                }
            }

            void emitLocalloc()
            {
                EmitExpression(count, used: true);

                _sawStackalloc = true;
                _builder.EmitOpCode(ILOpCode.Localloc);
            }
        }

        private ArrayInitializerStyle ShouldEmitBlockInitializerForStackAlloc(TypeSymbol elementType, ImmutableArray<BoundExpression> inits)
        {
            if (!_module.FieldRvaSupported)
            {
                // Avoid using FieldRva table when not supported by the runtime.
                return ArrayInitializerStyle.Element;
            }

            if (IsTypeAllowedInBlobWrapper(elementType.EnumUnderlyingTypeOrSelf().SpecialType))
            {
                int initCount = 0;
                int constCount = 0;
                StackAllocInitializerCount(inits, ref initCount, ref constCount);

                if (initCount > 2)
                {
                    if (initCount == constCount)
                    {
                        return ArrayInitializerStyle.Block;
                    }

                    int thresholdCnt = Math.Max(3, (initCount / 3));

                    if (constCount >= thresholdCnt)
                    {
                        return ArrayInitializerStyle.Mixed;
                    }
                }
            }

            return ArrayInitializerStyle.Element;
        }

        private void StackAllocInitializerCount(ImmutableArray<BoundExpression> inits, ref int initCount, ref int constInits)
        {
            if (inits.Length == 0)
            {
                return;
            }

            foreach (var init in inits)
            {
                Debug.Assert(!(init is BoundArrayInitialization), "Nested initializers are not allowed for stackalloc");

                initCount += 1;
                if (init.ConstantValueOpt != null)
                {
                    constInits += 1;
                }
            }
        }

        private void EmitElementStackAllocInitializers(TypeSymbol elementType, ImmutableArray<BoundExpression> inits, bool includeConstants)
        {
            int index = 0;
            int elementTypeSizeInBytes = elementType.EnumUnderlyingTypeOrSelf().SpecialType.SizeInBytes();
            foreach (BoundExpression init in inits)
            {
                if (includeConstants || init.ConstantValueOpt == null)
                {
                    _builder.EmitOpCode(ILOpCode.Dup);
                    EmitPointerElementAccess(init, elementType, elementTypeSizeInBytes, index);
                    EmitExpression(init, used: true);
                    EmitIndirectStore(elementType, init.Syntax);
                }

                index++;
            }
        }

        private void EmitPointerElementAccess(BoundExpression init, TypeSymbol elementType, int elementTypeSizeInBytes, int index)
        {
            if (index == 0)
            {
                return;
            }

            if (elementTypeSizeInBytes == 1)
            {
                _builder.EmitIntConstant(index);
                _builder.EmitOpCode(ILOpCode.Add);
            }
            else if (index == 1)
            {
                EmitIntConstantOrSizeOf(init, elementType, elementTypeSizeInBytes);
                _builder.EmitOpCode(ILOpCode.Add);
            }
            else
            {
                _builder.EmitIntConstant(index);
                _builder.EmitOpCode(ILOpCode.Conv_i);
                EmitIntConstantOrSizeOf(init, elementType, elementTypeSizeInBytes);
                _builder.EmitOpCode(ILOpCode.Mul);
                _builder.EmitOpCode(ILOpCode.Add);
            }
        }

        private void EmitIntConstantOrSizeOf(BoundExpression init, TypeSymbol elementType, int elementTypeSizeInBytes)
        {
            if (elementTypeSizeInBytes == 0)
            {
                _builder.EmitOpCode(ILOpCode.Sizeof);
                EmitSymbolToken(elementType, init.Syntax);
            }
            else
            {
                _builder.EmitIntConstant(elementTypeSizeInBytes);
            }
        }
    }
}
