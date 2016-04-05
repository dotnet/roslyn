// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            BoundExpression rewrittenReceiver = VisitExpression(node.ReceiverOpt);

            var tupleField = node.FieldSymbol as TupleFieldSymbol;
            if (tupleField != null)
            {
                return RewriteTupleFieldAccess(node, rewrittenReceiver);
            }
            else
            {
                return MakeFieldAccess(node.Syntax, rewrittenReceiver, node.FieldSymbol, node.ConstantValue, node.ResultKind, node.Type, node);
            }
        }

        /// <summary>
        /// Converts access to a tuple instance into access into the underlying ValueTuple(s).
        ///
        /// For instance, tuple.Item8
        /// produces fieldAccess(field=Item1, receiver=fieldAccess(field=Rest, receiver=ValueTuple for tuple))
        /// </summary>
        private BoundExpression RewriteTupleFieldAccess(BoundFieldAccess node, BoundExpression rewrittenReceiver)
        {
            var tupleField = (TupleFieldSymbol)node.FieldSymbol;
            var tupleType = (TupleTypeSymbol)tupleField.ContainingSymbol;

            int fieldRemainder;
            int loop = TupleTypeSymbol.NumberOfValueTuples(tupleField.Position, out fieldRemainder);

            NamedTypeSymbol currentLinkType = tupleType.UnderlyingTupleType;

            if (loop > 1)
            {
                WellKnownMember wellKnownTupleRest = TupleTypeSymbol.GetTupleTypeMember(TupleTypeSymbol.RestPosition, TupleTypeSymbol.RestPosition);
                var tupleRestField = (FieldSymbol)Binder.GetWellKnownTypeMember(_compilation, wellKnownTupleRest, _diagnostics, syntax: node.Syntax);
                if ((object)tupleRestField == null)
                {
                    return node;
                }

                // make nested field accesses to Rest
                do
                {
                    FieldSymbol nestedFieldSymbol = tupleRestField.AsMember(currentLinkType);

                    currentLinkType = (NamedTypeSymbol)currentLinkType.TypeArgumentsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1];
                    rewrittenReceiver = new BoundFieldAccess(node.Syntax, rewrittenReceiver, nestedFieldSymbol, ConstantValue.NotAvailable, LookupResultKind.Viable, currentLinkType);
                    loop--;
                }
                while (loop > 1);
            }

            // make a field access for the most local access
            WellKnownMember wellKnownTypeMember = TupleTypeSymbol.GetTupleTypeMember(currentLinkType.Arity, fieldRemainder);
            var linkField = (FieldSymbol)Binder.GetWellKnownTypeMember(_compilation, wellKnownTypeMember, _diagnostics, syntax: node.Syntax);
            if ((object)linkField == null)
            {
                return node;
            }

            FieldSymbol lastFieldSymbol = linkField.AsMember(currentLinkType);

            return new BoundFieldAccess(node.Syntax, rewrittenReceiver, lastFieldSymbol, node.ConstantValue, node.ResultKind, node.Type);
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
