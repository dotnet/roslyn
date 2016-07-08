// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Debug.Assert(node.DeconstructSteps != null);
            Debug.Assert(node.AssignmentSteps != null);

            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();
            var placeholders = ArrayBuilder<BoundValuePlaceholderBase>.GetInstance();

            // evaluate left-hand-side side-effects
            var lhsTargets = LeftHandSideSideEffects(node.LeftVariables, temps, stores);

            // get or make right-hand-side values
            BoundExpression loweredRight = VisitExpression(node.Right);
            AddPlaceholderReplacement(node.DeconstructSteps[0].TargetPlaceholder, loweredRight);
            placeholders.Add(node.DeconstructSteps[0].TargetPlaceholder);

            foreach (var deconstruction in node.DeconstructSteps)
            {
                if (deconstruction.DeconstructInvocationOpt == null)
                {
                    // tuple case
                    AccessTupleFields(node, deconstruction, temps, stores, placeholders);
                }
                else
                {
                    CallDeconstruct(node, deconstruction, temps, stores, placeholders);
                }
            }

            int numAssignments = node.AssignmentSteps.Length;
            for (int i = 0; i < numAssignments; i++)
            {
                // lower the assignment and replace the placeholders for its outputs in the process
                var assignmentInfo = node.AssignmentSteps[i];
                AddPlaceholderReplacement(assignmentInfo.OutputPlaceholder, lhsTargets[i]);

                var assignment = VisitExpression(assignmentInfo.Assignment);

                RemovePlaceholderReplacement(assignmentInfo.OutputPlaceholder);

                stores.Add(assignment);
            }

            var result = _factory.Sequence(temps.ToImmutable(), stores.ToImmutable(), new BoundVoid(node.Syntax, node.Type));

            RemovePlaceholderReplacements(placeholders);
            placeholders.Free();

            temps.Free();
            stores.Free();

            return result;
        }

        /// <summary>
        /// Adds the side effects to stores and returns temporaries (as a flat list) to access them.
        /// </summary>
        private ImmutableArray<BoundExpression> LeftHandSideSideEffects(ImmutableArray<BoundExpression> variables, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores)
        {
            var lhsReceivers = ArrayBuilder<BoundExpression>.GetInstance(variables.Length);

            foreach (var variable in variables)
            {
                lhsReceivers.Add(TransformCompoundAssignmentLHS(variable, stores, temps, isDynamicAssignment: false));
            }

            return lhsReceivers.ToImmutableAndFree();
        }

        private void AccessTupleFields(BoundDeconstructionAssignmentOperator node, BoundDeconstructionDeconstructStep deconstruction, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores, ArrayBuilder<BoundValuePlaceholderBase> placeholders)
        {
            var target = PlaceholderReplacement(deconstruction.TargetPlaceholder);
            var tupleType = target.Type.IsTupleType ? target.Type : TupleTypeSymbol.Create((NamedTypeSymbol)target.Type);
            var tupleElementTypes = tupleType.TupleElementTypes;

            var numElements = tupleElementTypes.Length;

            CSharpSyntaxNode syntax = node.Syntax;

            // save the target as we need to access it multiple times
            BoundAssignmentOperator assignmentToTemp;
            var savedTuple = _factory.StoreToTemp(target, out assignmentToTemp);
            stores.Add(assignmentToTemp);
            temps.Add(savedTuple.LocalSymbol);

            // list the tuple fields accessors
            var fields = tupleType.TupleElementFields;

            for (int i = 0; i < numElements; i++)
            {
                var field = fields[i];

                DiagnosticInfo useSiteInfo = field.GetUseSiteDiagnostic();
                if ((object)useSiteInfo != null && useSiteInfo.Severity == DiagnosticSeverity.Error)
                {
                    Symbol.ReportUseSiteDiagnostic(useSiteInfo, _diagnostics, syntax.Location);
                }
                var fieldAccess = MakeTupleFieldAccess(syntax, field, savedTuple, null, LookupResultKind.Empty);

                AddPlaceholderReplacement(deconstruction.OutputPlaceholders[i], fieldAccess);
                placeholders.Add(deconstruction.OutputPlaceholders[i]);
            }
        }

        /// <summary>
        /// Prepares local variables to be used in Deconstruct call
        /// Adds a invocation of Deconstruct with those as out parameters onto the 'stores' sequence
        /// Returns the expressions for those out parameters
        /// </summary>
        private void CallDeconstruct(BoundDeconstructionAssignmentOperator node, BoundDeconstructionDeconstructStep deconstruction, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores, ArrayBuilder<BoundValuePlaceholderBase> placeholders)
        {
            Debug.Assert((object)deconstruction.DeconstructInvocationOpt != null);

            CSharpSyntaxNode syntax = node.Syntax;

            // prepare out parameters for Deconstruct
            var deconstructParameters = deconstruction.OutputPlaceholders;
            var outParametersBuilder = ArrayBuilder<BoundExpression>.GetInstance(deconstructParameters.Length);

            for (var i = 0; i < deconstructParameters.Length; i++)
            {
                var deconstructParameter = deconstructParameters[i];
                var localSymbol = new SynthesizedLocal(_factory.CurrentMethod, deconstructParameter.Type, SynthesizedLocalKind.LoweringTemp);

                var localBound = new BoundLocal(syntax,
                                                localSymbol,
                                                null,
                                                deconstructParameter.Type
                                                )
                { WasCompilerGenerated = true };

                temps.Add(localSymbol);
                outParametersBuilder.Add(localBound);

                AddPlaceholderReplacement(deconstruction.OutputPlaceholders[i], localBound);
                placeholders.Add(deconstruction.OutputPlaceholders[i]);
            }

            var outParameters = outParametersBuilder.ToImmutableAndFree();

            // invoke Deconstruct with placeholders replaced by locals
            stores.Add(VisitExpression(deconstruction.DeconstructInvocationOpt));
        }
    }
}
