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

            var elementType = (type.TypeKind == TypeKind.Pointer
                ? ((PointerTypeSymbol)type).PointedAtTypeWithAnnotations
                : ((NamedTypeSymbol)type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0]).Type;

            bool isReadOnlySpan = TypeSymbol.Equals(
                (type as NamedTypeSymbol)?.OriginalDefinition, _module.Compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.ConsiderEverything);

            var initExprs = inits.Initializers;

            bool supportsPrivateImplClass = _module.SupportsPrivateImplClass;
            var initializationStyle = ShouldEmitBlockInitializerForStackAlloc(elementType, initExprs, supportsPrivateImplClass);

            if (isReadOnlySpan)
            {
                // ROS<T> is only used here if it has already been decided to use CreateSpan
                Debug.Assert(UseCreateSpanForReadOnlySpanInitialization(
                    _module.GetCreateSpanHelper(elementType.GetPublicSymbol()) is not null, false, elementType, inits, supportsPrivateImplClass));

                EmitExpression(count, used: false);

                ImmutableArray<byte> data = GetRawData(initExprs);
                _builder.EmitCreateSpan(data, elementType.GetPublicSymbol(), inits.Syntax, _diagnostics);
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
                    _builder.EmitStackAllocBlockSingleByteInitializer(data, inits.Syntax, emitInitBlock: true, _diagnostics);
                }
                else if (elementType.SpecialType.SizeInBytes() == 1)
                {
                    _builder.EmitStackAllocBlockSingleByteInitializer(data, inits.Syntax, emitInitBlock: false, _diagnostics);
                }
                else
                {
                    if (_module.GetCreateSpanHelper(elementType.GetPublicSymbol()) is null)
                    {
                        EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: true);
                        mixedInitialized = true;
                    }
                    else
                    {
                        _builder.EmitStackAllocBlockMultiByteInitializer(data, elementType.GetPublicSymbol(), inits.Syntax, _diagnostics);
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

        internal static bool UseCreateSpanForReadOnlySpanInitialization(
            bool hasCreateSpanHelper, bool considerInitblk, TypeSymbol elementType, BoundArrayInitialization? inits, bool supportsPrivateImplClass) =>
                hasCreateSpanHelper && inits?.Initializers is { } initExprs &&
                ShouldEmitBlockInitializerForStackAlloc(elementType, initExprs, supportsPrivateImplClass) == ArrayInitializerStyle.Block &&
                // if all bytes are the same, use initblk if able, instead of CreateSpan
                (!considerInitblk || (GetRawData(initExprs) is var data && !data.All(datum => datum == data[0]))) &&
                elementType.SpecialType.SizeInBytes() > 1;

        private static ArrayInitializerStyle ShouldEmitBlockInitializerForStackAlloc(TypeSymbol elementType, ImmutableArray<BoundExpression> inits, bool supportsPrivateImplClass)
        {
            if (!supportsPrivateImplClass)
            {
                return ArrayInitializerStyle.Element;
            }

            elementType = elementType.EnumUnderlyingTypeOrSelf();

            if (elementType.SpecialType.IsBlittable())
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
                if (init.ConstantValue != null)
                {
                    constInits += 1;
                }
            }
        }

        private void EmitElementStackAllocInitializers(TypeSymbol elementType, ImmutableArray<BoundExpression> inits, bool includeConstants)
        {
            int index = 0;
            int elementTypeSizeInBytes = elementType.SpecialType.SizeInBytes();
            foreach (BoundExpression init in inits)
            {
                if (includeConstants || init.ConstantValue == null)
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
