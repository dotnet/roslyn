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
            var placeholders = ArrayBuilder<BoundValuePlaceholderBase>.GetInstance();

            // evaluate left-hand-side side-effects
            ImmutableArray<BoundExpression> lhsTargets = LeftHandSideSideEffects(node.LeftVariables, temps, stores);

            // get or make right-hand-side values
            BoundExpression loweredRight = VisitExpression(node.Right);

            ApplyDeconstructions(node, temps, stores, placeholders, loweredRight);
            ApplyConversions(node, temps, stores, placeholders);
            ApplyAssignments(node, stores, lhsTargets);

            BoundExpression returnValue = MakeReturnValue(node, placeholders);
            BoundExpression result = _factory.Sequence(temps.ToImmutable(), stores.ToImmutable(), returnValue);

            RemovePlaceholderReplacements(placeholders);
            placeholders.Free();

            temps.Free();
            stores.Free();

            return result;
        }

        /// <summary>
        /// Applies the deconstructions.
        /// Adds any new locals to the temps and any new expressions to be evaluated to the stores.
        /// </summary>
        private void ApplyDeconstructions(BoundDeconstructionAssignmentOperator node, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores, ArrayBuilder<BoundValuePlaceholderBase> placeholders, BoundExpression loweredRight)
        {
            var firstDeconstructStep = node.DeconstructSteps[0];
            AddPlaceholderReplacement(firstDeconstructStep.InputPlaceholder, loweredRight);
            placeholders.Add(firstDeconstructStep.InputPlaceholder);

            foreach (BoundDeconstructionDeconstructStep deconstruction in node.DeconstructSteps)
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
        }

        /// <summary>
        /// Applies the conversions.
        /// Adds any new locals to the temps and any new expressions to be evaluated to the stores.
        /// </summary>
        private void ApplyConversions(BoundDeconstructionAssignmentOperator node, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores, ArrayBuilder<BoundValuePlaceholderBase> placeholders)
        {
            int numConversions = node.ConversionSteps.Length;
            var conversionLocals = ArrayBuilder<BoundExpression>.GetInstance();

            foreach (var conversionInfo in node.ConversionSteps)
            {
                // lower the conversions and assignments to locals
                var localSymbol = new SynthesizedLocal(_factory.CurrentMethod, conversionInfo.OutputPlaceholder.Type, SynthesizedLocalKind.LoweringTemp);
                var localBound = new BoundLocal(node.Syntax,
                                               localSymbol,
                                               null,
                                               conversionInfo.OutputPlaceholder.Type)
                { WasCompilerGenerated = true };

                temps.Add(localSymbol);
                conversionLocals.Add(localBound);

                AddPlaceholderReplacement(conversionInfo.OutputPlaceholder, localBound);
                placeholders.Add(conversionInfo.OutputPlaceholder);

                var conversion = VisitExpression(conversionInfo.Assignment);

                stores.Add(conversion);
            }
        }

        /// <summary>
        /// Applies the assignments.
        /// Adds any new expressions to be evaluated to the stores.
        /// </summary>
        private void ApplyAssignments(BoundDeconstructionAssignmentOperator node, ArrayBuilder<BoundExpression> stores, ImmutableArray<BoundExpression> lhsTargets)
        {
            int numAssignments = node.AssignmentSteps.Length;
            for (int i = 0; i < numAssignments; i++)
            {
                var assignmentInfo = node.AssignmentSteps[i];
                AddPlaceholderReplacement(assignmentInfo.OutputPlaceholder, lhsTargets[i]);

                var assignment = assignmentInfo.Assignment;

                // All the input placeholders for the assignments should already be set with lowered nodes
                Debug.Assert(assignment.Left.Kind == BoundKind.DeconstructValuePlaceholder);
                Debug.Assert(assignment.Right.Kind == BoundKind.DeconstructValuePlaceholder);
                var rewrittenLeft = (BoundExpression)Visit(assignment.Left);
                var rewrittenRight = (BoundExpression)Visit(assignment.Right);

                var loweredAssignment = MakeAssignmentOperator(assignment.Syntax, rewrittenLeft, rewrittenRight, assignment.Type,
                                            used: true, isChecked: false, isCompoundAssignment: false);

                RemovePlaceholderReplacement(assignmentInfo.OutputPlaceholder);

                stores.Add(loweredAssignment);
            }
        }

        /// <summary>
        /// Makes an expression that constructs the return value for the deconstruction.
        /// For d-declarations, that is simply void.
        /// For d-assignments, that is a series of tuple constructions, that are chained with the help of placeholders.
        /// The placeholders that are set are added to the list for later clearing.
        /// </summary>
        private BoundExpression MakeReturnValue(BoundDeconstructionAssignmentOperator node, ArrayBuilder<BoundValuePlaceholderBase> placeholders)
        {
            if (node.IsDeclaration)
            {
                return new BoundVoid(node.Syntax, node.Type);
            }

            BoundExpression loweredConstruction = null;
            foreach (var constructionInfo in node.ConstructionStepsOpt)
            {
                // All the input placeholders for the constructions should already be set
                loweredConstruction = (BoundExpression)Visit(constructionInfo.Construct);

                AddPlaceholderReplacement(constructionInfo.OutputPlaceholder, loweredConstruction);
                placeholders.Add(constructionInfo.OutputPlaceholder);
            }

            Debug.Assert(loweredConstruction != null);
            return loweredConstruction;
        }

        /// <summary>
        /// Adds the side effects to stores and returns temporaries (as a flat list) to access them.
        /// </summary>
        private ImmutableArray<BoundExpression> LeftHandSideSideEffects(ImmutableArray<BoundExpression> variables, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores)
        {
            var lhsReceivers = ArrayBuilder<BoundExpression>.GetInstance(variables.Length);

            foreach (var variable in variables)
            {
                lhsReceivers.Add(TransformCompoundAssignmentLHS(variable, stores, temps, isDynamicAssignment: variable.Type.IsDynamic()));
            }

            return lhsReceivers.ToImmutableAndFree();
        }

        private void AccessTupleFields(BoundDeconstructionAssignmentOperator node, BoundDeconstructionDeconstructStep deconstruction, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> stores, ArrayBuilder<BoundValuePlaceholderBase> placeholders)
        {
            var target = PlaceholderReplacement(deconstruction.InputPlaceholder);
            var tupleType = target.Type.IsTupleType ? target.Type : TupleTypeSymbol.Create((NamedTypeSymbol)target.Type);
            var tupleElementTypes = tupleType.TupleElementTypes;

            var numElements = tupleElementTypes.Length;

            SyntaxNode syntax = node.Syntax;

            // save the target as we need to access it multiple times
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal savedTuple = _factory.StoreToTemp(target, out assignmentToTemp);
            stores.Add(assignmentToTemp);
            temps.Add(savedTuple.LocalSymbol);

            // list the tuple fields accessors
            var fields = tupleType.TupleDefaultElementFields;

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

            SyntaxNode syntax = node.Syntax;

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
