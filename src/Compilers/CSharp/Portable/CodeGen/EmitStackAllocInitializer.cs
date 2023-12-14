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

            bool isReadOnlySpan = TypeSymbol.Equals(
                (type as NamedTypeSymbol)?.OriginalDefinition, _module.Compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.ConsiderEverything);

            var initExprs = inits.Initializers;

            bool isEncDelta = _module.IsEncDelta;
            var initializationStyle = ShouldEmitBlockInitializerForStackAlloc(elementType, initExprs, isEncDelta);

            if (isReadOnlySpan)
            {
                var createSpanHelper = getCreateSpanHelper(_module, elementType);

                // ROS<T> is only used here if it has already been decided to use CreateSpan
                Debug.Assert(createSpanHelper is not null);
                Debug.Assert(UseCreateSpanForReadOnlySpanStackAlloc(elementType, inits, isEncDelta: isEncDelta));

                EmitExpression(count, used: false);

                ImmutableArray<byte> data = GetRawData(initExprs);
                _builder.EmitCreateSpan(data, createSpanHelper, inits.Syntax, _diagnostics.DiagnosticBag);
            }
            else if (initializationStyle == ArrayInitializerStyle.Element)
            {
                emitLocalloc();
                EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: true);
            }
            else
            {
                bool mixedInitialized = false;

                emitLocalloc();

                ImmutableArray<byte> data = GetRawData(initExprs);
                if (data.All(datum => datum == data[0]))
                {
                    // All bytes are the same, no need for metadata blob, just initblk to fill it with the repeated value.
                    _builder.EmitOpCode(ILOpCode.Dup);
                    _builder.EmitIntConstant(data[0]);
                    _builder.EmitIntConstant(data.Length);
                    _builder.EmitOpCode(ILOpCode.Initblk, -3);
                }
                else if (elementType.EnumUnderlyingTypeOrSelf().SpecialType.SizeInBytes() == 1)
                {
                    // Initialize the stackalloc by copying the data from a metadata blob
                    var field = _builder.module.GetFieldForData(data, alignment: 1, inits.Syntax, _diagnostics.DiagnosticBag);
                    _builder.EmitOpCode(ILOpCode.Dup);
                    _builder.EmitOpCode(ILOpCode.Ldsflda);
                    _builder.EmitToken(field, inits.Syntax, _diagnostics.DiagnosticBag);
                    _builder.EmitIntConstant(data.Length);
                    _builder.EmitOpCode(ILOpCode.Cpblk, -3);
                }
                else
                {
                    if (getCreateSpanHelper(_module, elementType) is { } createSpanHelper &&
                        _module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__GetPinnableReference) is MethodSymbol getPinnableReference)
                    {
                        // Use RuntimeHelpers.CreateSpan and cpblk.
                        EmitStackAllocBlockMultiByteInitializer(data, createSpanHelper, getPinnableReference, elementType, inits.Syntax, _diagnostics.DiagnosticBag);
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

            static Cci.IMethodReference? getCreateSpanHelper(Emit.PEModuleBuilder module, TypeSymbol elementType)
            {
                var member = module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle);
                return ((MethodSymbol?)member)?.Construct(elementType).GetCciAdapter();
            }
        }

        private void EmitStackAllocBlockMultiByteInitializer(ImmutableArray<byte> data, Cci.IMethodReference createSpanHelper, MethodSymbol getPinnableReferenceDefinition, TypeSymbol elementType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            var readOnlySpan = getPinnableReferenceDefinition.ContainingType.Construct(elementType);
            Debug.Assert(TypeSymbol.Equals(readOnlySpan.OriginalDefinition, _module.Compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.ConsiderEverything));
            var getPinnableReference = getPinnableReferenceDefinition.AsMember(readOnlySpan);

            _builder.EmitOpCode(ILOpCode.Dup);
            _builder.EmitCreateSpan(data, createSpanHelper, syntaxNode, diagnostics);

            var temp = AllocateTemp(readOnlySpan, syntaxNode);
            _builder.EmitLocalStore(temp);
            _builder.EmitLocalAddress(temp);

            _builder.EmitOpCode(ILOpCode.Call, 0);
            EmitSymbolToken(getPinnableReference, syntaxNode, optArgList: null);
            _builder.EmitIntConstant(data.Length);
            _builder.EmitOpCode(ILOpCode.Cpblk, -3);

            FreeTemp(temp);
        }

        internal static bool UseCreateSpanForReadOnlySpanStackAlloc(TypeSymbol elementType, BoundArrayInitialization? inits, bool isEncDelta)
        {
            return inits?.Initializers is { } initExprs &&
                elementType.EnumUnderlyingTypeOrSelf().SpecialType.SizeInBytes() > 1 &&
                ShouldEmitBlockInitializerForStackAlloc(elementType, initExprs, isEncDelta) == ArrayInitializerStyle.Block;
        }

        private static ArrayInitializerStyle ShouldEmitBlockInitializerForStackAlloc(TypeSymbol elementType, ImmutableArray<BoundExpression> inits, bool isEncDelta)
        {
            if (isEncDelta)
            {
                // Avoid using FieldRva table. Can be allowed if tested on all supported runtimes.
                // Consider removing: https://github.com/dotnet/roslyn/issues/69480
                return ArrayInitializerStyle.Element;
            }

            if (elementType.EnumUnderlyingTypeOrSelf().SpecialType.IsBlittable())
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

        private static void StackAllocInitializerCount(ImmutableArray<BoundExpression> inits, ref int initCount, ref int constInits)
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
