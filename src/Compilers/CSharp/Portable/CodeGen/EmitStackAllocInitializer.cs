// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private void EmitStackAllocInitializers(TypeSymbol type, BoundArrayInitialization inits)
        {
            Debug.Assert(type is PointerTypeSymbol || type is NamedTypeSymbol);

            var elementType = (type.TypeKind == TypeKind.Pointer
                ? ((PointerTypeSymbol)type).PointedAtTypeWithAnnotations
                : ((NamedTypeSymbol)type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0]).Type;

            var initExprs = inits.Initializers;

            var initializationStyle = ShouldEmitBlockInitializerForStackAlloc(elementType, initExprs);
            if (initializationStyle == ArrayInitializerStyle.Element)
            {
                EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: true);
            }
            else
            {
                ImmutableArray<byte> data = this.GetRawData(initExprs);
                if (data.All(datum => datum == data[0]))
                {
                    // All bytes are the same, no need for metadata blob, just initblk to fill it with the repeated value.
                    _builder.EmitOpCode(ILOpCode.Dup);
                    _builder.EmitIntConstant(data[0]);
                    _builder.EmitIntConstant(data.Length);
                    _builder.EmitOpCode(ILOpCode.Initblk, -3);

                    if (initializationStyle == ArrayInitializerStyle.Mixed)
                    {
                        EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: false);
                    }
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

                    if (initializationStyle == ArrayInitializerStyle.Mixed)
                    {
                        EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: false);
                    }
                }
                else
                {
                    EmitElementStackAllocInitializers(elementType, initExprs, includeConstants: true);
                }
            }
        }

        private ArrayInitializerStyle ShouldEmitBlockInitializerForStackAlloc(TypeSymbol elementType, ImmutableArray<BoundExpression> inits)
        {
            if (_module.IsEncDelta)
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
