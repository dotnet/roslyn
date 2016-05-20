// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();

            var lhsReceivers = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var variable in node.LeftVariables)
            {
                // This will be filled in with the LHS that uses temporaries to prevent
                // double-evaluation of side effects.
                lhsReceivers.Add(TransformCompoundAssignmentLHS(variable, stores, temps, isDynamicAssignment: false));
            }

            BoundExpression loweredRight = VisitExpression(node.Right);
            ImmutableArray<BoundExpression> rhsValues;

            // get or make right-hand-side values
            if (node.Right.Type.IsTupleType)
            {
                rhsValues = AccessTupleFields(node, loweredRight, temps, stores);
            }
            else
            {
                rhsValues = CallDeconstruct(node, loweredRight, temps, stores);
            }

            // assign from rhs values to lhs receivers
            int numAssignments = node.Assignments.Length;
            for (int i = 0; i < numAssignments; i++)
            {
                // lower the assignment and replace the placeholders for source and target in the process
                var assignmentInfo = node.Assignments[i];

                AddPlaceholderReplacement(assignmentInfo.LValuePlaceholder, lhsReceivers[i]);
                AddPlaceholderReplacement(assignmentInfo.RValuePlaceholder, rhsValues[i]);

                var assignment = VisitExpression(assignmentInfo.Assignment);

                RemovePlaceholderReplacement(assignmentInfo.LValuePlaceholder);
                RemovePlaceholderReplacement(assignmentInfo.RValuePlaceholder);

                stores.Add(assignment);
            }

            var result = _factory.Sequence(temps.ToImmutable(), stores.ToArray());

            temps.Free();
            stores.Free();
            lhsReceivers.Free();

            return result;
        }

        private ImmutableArray<BoundExpression> AccessTupleFields(BoundDeconstructionAssignmentOperator node, BoundExpression loweredRight, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores)
        {
            var tupleType = loweredRight.Type.IsTupleType ? loweredRight.Type : TupleTypeSymbol.Create((NamedTypeSymbol)loweredRight.Type);
            var tupleElementTypes = tupleType.TupleElementTypes;

            var numElements = tupleElementTypes.Length;
            Debug.Assert(numElements == node.LeftVariables.Length);

            CSharpSyntaxNode syntax = node.Syntax;

            // save the loweredRight as we need to access it multiple times 
            BoundAssignmentOperator assignmentToTemp;
            var savedTuple = _factory.StoreToTemp(loweredRight, out assignmentToTemp);
            stores.Add(assignmentToTemp);
            temps.Add(savedTuple.LocalSymbol);

            // list the tuple fields accessors
            var fieldAccessorsBuilder = ArrayBuilder<BoundExpression>.GetInstance(numElements);
            var fields = tupleType.TupleElementFields;

            for (int i = 0; i < numElements; i++)
            {
                var field = fields[i];

                DiagnosticInfo useSiteInfo = field.GetUseSiteDiagnostic();
                if ((object)useSiteInfo != null && useSiteInfo.Severity == DiagnosticSeverity.Error)
                {
                    Symbol.ReportUseSiteDiagnostic(useSiteInfo, _diagnostics, syntax.Location);
                }
                var fieldAccess = MakeTupleFieldAccess(syntax, field, savedTuple, null, LookupResultKind.Empty, tupleElementTypes[i]);
                fieldAccessorsBuilder.Add(fieldAccess);
            }

            return fieldAccessorsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Prepares local variables to be used in Deconstruct call
        /// Adds a invocation of Deconstruct with those as out parameters onto the 'stores' sequence
        /// Returns the expressions for those out parameters
        /// </summary>
        private ImmutableArray<BoundExpression> CallDeconstruct(BoundDeconstructionAssignmentOperator node, BoundExpression loweredRight, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores)
        {
            Debug.Assert((object)node.DeconstructMemberOpt != null);

            CSharpSyntaxNode syntax = node.Syntax;

            // prepare out parameters for Deconstruct
            var deconstructParameters = node.DeconstructMemberOpt.Parameters;
            var outParametersBuilder = ArrayBuilder<BoundExpression>.GetInstance(deconstructParameters.Length);
            Debug.Assert(deconstructParameters.Length == node.LeftVariables.Length);

            foreach (var deconstructParameter in deconstructParameters)
            {
                var localSymbol = new SynthesizedLocal(_factory.CurrentMethod, deconstructParameter.Type, SynthesizedLocalKind.LoweringTemp);

                var localBound = new BoundLocal(syntax,
                                                localSymbol,
                                                null,
                                                deconstructParameter.Type
                                                )
                { WasCompilerGenerated = true };

                temps.Add(localSymbol);
                outParametersBuilder.Add(localBound);
            }

            var outParameters = outParametersBuilder.ToImmutableAndFree();

            // invoke Deconstruct
            var invokeDeconstruct = MakeCall(syntax, loweredRight, node.DeconstructMemberOpt, outParameters, node.DeconstructMemberOpt.ReturnType);
            stores.Add(invokeDeconstruct);

            return outParameters;
        }
    }
}
