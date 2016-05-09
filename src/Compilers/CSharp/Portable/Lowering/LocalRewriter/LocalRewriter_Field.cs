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

            if (fieldSymbol.IsTupleField)
            {
                return MakeTupleFieldAccess(syntax, fieldSymbol, rewrittenReceiver, constantValueOpt, resultKind, type);
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
            FieldSymbol tupleField, 
            BoundExpression rewrittenReceiver,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type)
        {
            var tupleType = tupleField.ContainingType;

            NamedTypeSymbol currentLinkType = tupleType.TupleUnderlyingType;
            FieldSymbol underlyingField = tupleField.TupleUnderlyingField;

            if ((object)underlyingField == null)
            {
                // Use-site error must have been reported elsewhere.
                return new BoundFieldAccess(syntax, rewrittenReceiver, tupleField, constantValueOpt, resultKind, type, hasErrors: true);
            }

            if (underlyingField.ContainingType != currentLinkType)
            {
                WellKnownMember wellKnownTupleRest = TupleTypeSymbol.GetTupleTypeMember(TupleTypeSymbol.RestPosition, TupleTypeSymbol.RestPosition);
                var tupleRestField = (FieldSymbol)TupleTypeSymbol.GetWellKnownMemberInType(currentLinkType.OriginalDefinition, wellKnownTupleRest, _diagnostics, syntax);

                if ((object)tupleRestField == null)
                {
                    // error tolerance for cases when Rest is missing
                    return new BoundFieldAccess(syntax, rewrittenReceiver, tupleField, constantValueOpt, resultKind, type, hasErrors: true);
                }

                // make nested field accesses to Rest
                do
                {
                    FieldSymbol nestedFieldSymbol = tupleRestField.AsMember(currentLinkType);

                    currentLinkType = currentLinkType.TypeArgumentsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1].TupleUnderlyingType;
                    rewrittenReceiver = new BoundFieldAccess(syntax, rewrittenReceiver, nestedFieldSymbol, ConstantValue.NotAvailable, LookupResultKind.Viable, currentLinkType);
                }
                while (underlyingField.ContainingType != currentLinkType);
            }

            // make a field access for the most local access
            return new BoundFieldAccess(syntax, rewrittenReceiver, underlyingField, constantValueOpt, resultKind, type);
        }
    }
}
