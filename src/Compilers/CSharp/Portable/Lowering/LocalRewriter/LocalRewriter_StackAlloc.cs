// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            BoundExpression rewrittenCount = VisitExpression(node.Count);

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
            BoundExpression convertedCount = _factory.Convert(uintType, rewrittenCount, Conversion.ExplicitNumeric);
            convertedCount = _factory.Convert(uintPtrType, convertedCount, Conversion.IntegerToPointer);

            BoundExpression sizeOfExpression = _factory.Sizeof(((PointerTypeSymbol)node.Type).PointedAtType.TypeSymbol);
            BinaryOperatorKind multiplicationKind = BinaryOperatorKind.Checked | BinaryOperatorKind.UIntMultiplication; //"UInt" just to make it unsigned
            BoundExpression product = _factory.Binary(multiplicationKind, uintPtrType, convertedCount, sizeOfExpression);

            return node.Update(product, node.Type);
        }
    }
}
