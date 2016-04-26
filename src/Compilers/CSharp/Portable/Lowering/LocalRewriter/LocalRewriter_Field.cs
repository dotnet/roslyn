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
            return MakeFieldAccess(node.Syntax, rewrittenReceiver, node.FieldSymbol, node.ConstantValue, node.ResultKind, node.Type, node);
        }

        private BoundExpression MakeFieldAccess(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            BoundFieldAccess oldNodeOpt = null)
        {

            var tupleField = fieldSymbol as TupleFieldSymbol;
            if (tupleField != null)
            {
                return MakeTupleFieldAccess(syntax, tupleField, rewrittenReceiver, constantValueOpt, resultKind, type);
            }
            
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

        /// <summary>
        /// Converts access to a tuple instance into access into the underlying ValueTuple(s).
        ///
        /// For instance, tuple.Item8
        /// produces fieldAccess(field=Item1, receiver=fieldAccess(field=Rest, receiver=ValueTuple for tuple))
        /// </summary>
        private BoundExpression MakeTupleFieldAccess(
            CSharpSyntaxNode syntax, 
            TupleFieldSymbol tupleField, 
            BoundExpression rewrittenReceiver,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type)
        {
            var tupleType = (TupleTypeSymbol)tupleField.ContainingSymbol;

            int fieldRemainder;
            int loop = TupleTypeSymbol.NumberOfValueTuples(tupleField.Position, out fieldRemainder);

            NamedTypeSymbol currentLinkType = tupleType.UnderlyingTupleType;

            if (loop > 1)
            {
                WellKnownMember wellKnownTupleRest = TupleTypeSymbol.GetTupleTypeMember(TupleTypeSymbol.RestPosition, TupleTypeSymbol.RestPosition);
                var tupleRestField = (FieldSymbol)TupleTypeSymbol.GetWellKnownMemberInType(currentLinkType.OriginalDefinition, wellKnownTupleRest, _compilation.Assembly, _diagnostics, syntax);

                if ((object)tupleRestField == null)
                {
                    // error tolerance for cases when Rest is missing
                    return new BoundFieldAccess(syntax, rewrittenReceiver, tupleField, constantValueOpt, resultKind, type);
                }

                // make nested field accesses to Rest
                do
                {
                    FieldSymbol nestedFieldSymbol = tupleRestField.AsMember(currentLinkType);

                    currentLinkType = (NamedTypeSymbol)currentLinkType.TypeArgumentsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1];
                    rewrittenReceiver = new BoundFieldAccess(syntax, rewrittenReceiver, nestedFieldSymbol, ConstantValue.NotAvailable, LookupResultKind.Viable, currentLinkType);
                    loop--;
                }
                while (loop > 1);
            }

            // make a field access for the most local access
            WellKnownMember wellKnownTypeMember = TupleTypeSymbol.GetTupleTypeMember(currentLinkType.Arity, fieldRemainder);
            var linkField = (FieldSymbol)TupleTypeSymbol.GetWellKnownMemberInType(currentLinkType.OriginalDefinition, wellKnownTypeMember, _compilation.Assembly, _diagnostics, syntax);

            if ((object)linkField == null)
            {
                // error tolerance for cases when linkField is not avaialable
                return new BoundFieldAccess(syntax, rewrittenReceiver, tupleField, constantValueOpt, resultKind, type);
            }

            FieldSymbol lastFieldSymbol = linkField.AsMember(currentLinkType);

            return new BoundFieldAccess(syntax, rewrittenReceiver, lastFieldSymbol, constantValueOpt, resultKind, type);
        }
    }
}
