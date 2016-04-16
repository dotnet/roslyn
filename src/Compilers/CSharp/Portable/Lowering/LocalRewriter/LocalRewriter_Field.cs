// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            BoundExpression rewrittenReceiver = VisitExpression(node.ReceiverOpt);

            return MakeFieldAccess(node.Syntax, rewrittenReceiver, node.FieldSymbol, node.ConstantValue, node.ResultKind, node.Type, node);
        }

        private static BoundExpression MakeFieldAccess(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            BoundFieldAccess oldNodeOpt = null)
        {
            BoundExpression result = oldNodeOpt != null ?
                oldNodeOpt.Update(rewrittenReceiver, fieldSymbol, constantValueOpt, resultKind, type) :
                new BoundFieldAccess(syntax, rewrittenReceiver, fieldSymbol, constantValueOpt, resultKind, type);

            if (fieldSymbol.IsFixed)
            {
                // a reference to a fixed buffer is translated into its address
                result = new BoundConversion(syntax,
                    new BoundAddressOfOperator(syntax, result, syntax != null && SyntaxFacts.IsFixedStatementExpression(syntax), type, false),
                    new Conversion(ConversionKind.PointerToPointer), false, false, default(ConstantValue), type, false);
            }

            return result;
        }
    }
}
