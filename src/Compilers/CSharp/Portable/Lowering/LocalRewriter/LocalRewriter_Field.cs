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
            SyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            FieldSymbol fieldSymbol,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            BoundFieldAccess oldNodeOpt = null)
        {

            if (fieldSymbol.IsTupleField)
            {
                return MakeTupleFieldAccess(syntax, fieldSymbol, rewrittenReceiver, constantValueOpt, resultKind);
            }
            
            BoundExpression result = oldNodeOpt != null ?
                oldNodeOpt.Update(rewrittenReceiver, fieldSymbol, constantValueOpt, resultKind, type) :
                new BoundFieldAccess(syntax, rewrittenReceiver, fieldSymbol, constantValueOpt, resultKind, type);

            if (fieldSymbol.IsFixed)
            {
                // a reference to a fixed buffer is translated into its address
                result = new BoundConversion(syntax,
                    new BoundAddressOfOperator(syntax, result, syntax != null && SyntaxFacts.IsFixedStatementExpression(syntax), type, false),
                    Conversion.PointerToPointer, false, false, default(ConstantValue), type, false);
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
            SyntaxNode syntax,
            FieldSymbol tupleField, 
            BoundExpression rewrittenReceiver,
            ConstantValue constantValueOpt,
            LookupResultKind resultKind)
        {
            var tupleType = tupleField.ContainingType;

            NamedTypeSymbol currentLinkType = tupleType.TupleUnderlyingType;
            FieldSymbol underlyingField = tupleField.TupleUnderlyingField;

            if ((object)underlyingField == null)
            {
                // Use-site error must have been reported elsewhere.
                return _factory.BadExpression(tupleField.Type);
            }

            if (underlyingField.ContainingType != currentLinkType)
            {
                WellKnownMember wellKnownTupleRest = TupleTypeSymbol.GetTupleTypeMember(TupleTypeSymbol.RestPosition, TupleTypeSymbol.RestPosition);
                var tupleRestField = (FieldSymbol)TupleTypeSymbol.GetWellKnownMemberInType(currentLinkType.OriginalDefinition, wellKnownTupleRest, _diagnostics, syntax);

                if ((object)tupleRestField == null)
                {
                    // error tolerance for cases when Rest is missing
                    return _factory.BadExpression(tupleField.Type);
                }

                // make nested field accesses to Rest
                do
                {
                    FieldSymbol nestedFieldSymbol = tupleRestField.AsMember(currentLinkType);
                    rewrittenReceiver = _factory.Field(rewrittenReceiver, nestedFieldSymbol);

                    currentLinkType = currentLinkType.TypeArgumentsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1].TupleUnderlyingType;
                }
                while (underlyingField.ContainingType != currentLinkType);
            }

            // make a field access for the most local access
            return _factory.Field(rewrittenReceiver, underlyingField);
        }
    }
}
