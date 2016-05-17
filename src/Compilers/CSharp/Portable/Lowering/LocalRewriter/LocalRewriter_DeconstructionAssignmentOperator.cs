// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            CSharpSyntaxNode syntax = node.Syntax;
            int numVariables = node.LeftVariables.Length;

            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();

            var lhsReceivers = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var variable in node.LeftVariables)
            {
                // This will be filled in with the LHS that uses temporaries to prevent
                // double-evaluation of side effects.
                lhsReceivers.Add(TransformCompoundAssignmentLHS(variable, stores, temps, isDynamicAssignment: false));
            }

            BoundExpression loweredRight = VisitExpression(node.DeconstructReceiver);

            // prepare out parameters for Deconstruct
            var outParametersBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            var deconstructParameters = node.DeconstructMember.Parameters;
            Debug.Assert(deconstructParameters.Length == node.LeftVariables.Length);

            for (int i = 0; i < numVariables; i++)
            {
                var localSymbol = new SynthesizedLocal(_factory.CurrentMethod, deconstructParameters[i].Type, SynthesizedLocalKind.LoweringTemp);

                var localBound = new BoundLocal(syntax,
                                                localSymbol,
                                                null,
                                                deconstructParameters[i].Type
                                                ) { WasCompilerGenerated = true };

                temps.Add(localSymbol);
                outParametersBuilder.Add(localBound);
            }

            var outParameters = outParametersBuilder.ToImmutableAndFree();

            // invoke Deconstruct
            var invokeDeconstruct = MakeCall(syntax, loweredRight, node.DeconstructMember, outParameters, node.DeconstructMember.ReturnType);
            stores.Add(invokeDeconstruct);

            // assign from out temps to lhs receivers
            for (int i = 0; i < numVariables; i++)
            {
                // lower the assignment and replace the placeholders for source and target in the process

                AddPlaceholderReplacement(node.Assignments[i].LValuePlaceholder, lhsReceivers[i]);
                AddPlaceholderReplacement(node.Assignments[i].RValuePlaceholder, outParameters[i]);

                var assignment = VisitExpression(node.Assignments[i].Assignment);

                RemovePlaceholderReplacement(node.Assignments[i].LValuePlaceholder);
                RemovePlaceholderReplacement(node.Assignments[i].RValuePlaceholder);

                stores.Add(assignment);
            }

            var result = _factory.Sequence(temps.ToImmutable(), stores.ToArray());

            temps.Free();
            stores.Free();
            lhsReceivers.Free();

            return result;
        }
    }
}
