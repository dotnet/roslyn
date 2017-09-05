// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitConvertedStackAllocExpression(BoundConvertedStackAllocExpression stackAllocNode)
        {
            var rewrittenCount = VisitExpression(stackAllocNode.Count);

            var conversionKind = stackAllocNode.ConversionKind;
            var elementType = stackAllocNode.ElementType;

            switch (conversionKind)
            {
                case ConversionKind.StackAllocToPointerType:
                    {
                        var stackSize = RewriteStackAllocCountToSize(rewrittenCount, elementType);
                        return stackAllocNode.Update(elementType, stackSize, conversionKind, stackAllocNode.Type);
                    }
                case ConversionKind.StackAllocToSpanType:
                    {
                        Debug.Assert(stackAllocNode.Type.IsSpanType());

                        var spanType = (NamedTypeSymbol)stackAllocNode.Type;
                        var countTemp = _factory.StoreToTemp(rewrittenCount, out BoundAssignmentOperator countTempAssignment);
                        var stackSize = RewriteStackAllocCountToSize(countTemp, elementType);
                        stackAllocNode = stackAllocNode.Update(elementType, stackSize, conversionKind, spanType);

                        var spanCtor = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Span_T__ctor).SymbolAsMember(spanType);
                        var ctorCall = _factory.New(spanCtor, stackAllocNode, countTemp);

                        return new BoundSequence(
                            syntax: stackAllocNode.Syntax,
                            locals: ImmutableArray.Create(countTemp.LocalSymbol),
                            sideEffects: ImmutableArray.Create<BoundExpression>(countTempAssignment),
                            value: ctorCall,
                            type: spanType);
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(conversionKind);
                    }
            }
        }

        private BoundExpression RewriteStackAllocCountToSize(BoundExpression count, TypeSymbol elementType)
        {
            // From ILGENREC::genExpr:
            // EDMAURER always perform a checked multiply regardless of the context.
            // localloc takes an unsigned native int. When a user specifies a negative
            // count of elements, per spec, the behavior is undefined. So convert element
            // count to unsigned.

            // NOTE: to match this special case logic, we're going to construct the multiplication
            // ourselves, rather than calling MakeSizeOfMultiplication (which inserts various checks 
            // and conversions).

            TypeSymbol uintType = _factory.SpecialType(SpecialType.System_UInt32);
            TypeSymbol uintPtrType = _factory.SpecialType(SpecialType.System_UIntPtr);

            // Why convert twice?  Because dev10 actually uses an explicit conv_u instruction and the normal conversion
            // from int32 to native uint is emitted as conv_i.  The behavior we want to emulate is to re-interpret
            // (i.e. unchecked) an int32 as unsigned (i.e. uint32) and then convert it to a native uint *without* sign
            // extension.
            BoundExpression convertedCount = _factory.Convert(uintType, count, Conversion.ExplicitNumeric);
            convertedCount = _factory.Convert(uintPtrType, convertedCount, Conversion.IntegerToPointer);

            BoundExpression sizeOfExpression = _factory.Sizeof(elementType);
            BinaryOperatorKind multiplicationKind = BinaryOperatorKind.Checked | BinaryOperatorKind.UIntMultiplication; //"UInt" just to make it unsigned
            BoundExpression product = _factory.Binary(multiplicationKind, uintPtrType, convertedCount, sizeOfExpression);

            return product;
        }
    }
}
