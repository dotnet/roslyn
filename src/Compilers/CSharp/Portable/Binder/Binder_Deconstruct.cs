// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts deconstruction-assignment syntax (AssignmentExpressionSyntax nodes with the left being a tuple expression)
    /// into a BoundDeconstructionAssignmentOperator (or bad node).
    /// </summary>
    internal partial class Binder
    {
        /// <summary>
        /// There are two kinds of deconstruction-assignments which this binding handles: tuple and non-tuple.
        ///
        /// Returns a BoundDeconstructionAssignmentOperator with a list of deconstruction steps and assignment steps.
        /// Deconstruct steps for tuples have no invocation to Deconstruct, but steps for non-tuples do.
        /// </summary>
        private BoundExpression BindDeconstructionAssignment(AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // receiver for first Deconstruct step
            var boundRHS = BindValue(node.Right, diagnostics, BindValueKind.RValue);

            SeparatedSyntaxList<ArgumentSyntax> arguments = ((TupleExpressionSyntax)node.Left).Arguments;
            ImmutableArray<DeconstructionVariable> checkedVariables = BindDeconstructionVariables(arguments, diagnostics);

            if ((object)boundRHS.Type == null)
            {
                if (boundRHS.Kind == BoundKind.TupleLiteral)
                {
                    // tuple literal without type such as `(null, null)`, let's fix it up by peeking at the LHS
                    TypeSymbol lhsAsTuple = MakeTupleTypeFromDeconstructionLHS(checkedVariables, diagnostics, Compilation);
                    boundRHS = GenerateConversionForAssignment(lhsAsTuple, boundRHS, diagnostics);
                }
                else
                {
                    // expression without type such as `null`
                    Error(diagnostics, ErrorCode.ERR_DeconstructRequiresExpression, node);
                    return BadExpression(node, FlattenDeconstructVariables(checkedVariables).Concat(boundRHS).Cast<BoundNode>().ToImmutableArray());
                }
            }

            var deconstructionSteps = ArrayBuilder<BoundDeconstructionDeconstructStep>.GetInstance(1);
            var assignmentSteps = ArrayBuilder<BoundDeconstructionAssignmentStep>.GetInstance(1);
            try
            {
                bool hasErrors = !DeconstructIntoSteps(new BoundDeconstructValuePlaceholder(node.Right, boundRHS.Type), node, diagnostics, checkedVariables, deconstructionSteps, assignmentSteps);

                var deconstructions = deconstructionSteps.ToImmutable();
                var assignments = assignmentSteps.ToImmutable();

                TypeSymbol voidType = GetSpecialType(SpecialType.System_Void, diagnostics, node);
                return new BoundDeconstructionAssignmentOperator(node, FlattenDeconstructVariables(checkedVariables), boundRHS, deconstructions, assignments, voidType, hasErrors: hasErrors);
            }
            finally
            {
                deconstructionSteps.Free();
                assignmentSteps.Free();
            }
        }

        /// <summary>
        /// Whether the target is a tuple or a type that requires Deconstruction, this will generate and stack appropriate deconstruction and assignment steps.
        /// Note that the variables may either be plain or nested variables.
        /// Returns false if there was an error.
        /// </summary>
        private bool DeconstructIntoSteps(
                        BoundDeconstructValuePlaceholder targetPlaceholder,
                        AssignmentExpressionSyntax syntax,
                        DiagnosticBag diagnostics,
                        ImmutableArray<DeconstructionVariable> variables,
                        ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                        ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps)
        {
            Debug.Assert(targetPlaceholder.Type != null);

            BoundDeconstructionDeconstructStep step;

            if (targetPlaceholder.Type.IsTupleType)
            {
                // tuple literal such as `(1, 2)`, `(null, null)`, `(x.P, y.M())`
                step = MakeTupleDeconstructStep(targetPlaceholder, syntax, diagnostics, variables, deconstructionSteps, assignmentSteps);
            }
            else
            {
                step = MakeNonTupleDeconstructStep(targetPlaceholder, syntax, diagnostics, variables, deconstructionSteps, assignmentSteps);
            }

            if (step == null)
            {
                return false;
            }

            deconstructionSteps.Add(step);

            // outputs will either need a conversion step and assignment step, or if they are nested variables, they will need further deconstruction
            return DeconstructOrAssignOutputs(step, variables, syntax, diagnostics, deconstructionSteps, assignmentSteps);
        }

        /// <summary>
        /// This will generate and stack appropriate deconstruction and assignment steps for a tuple type.
        /// The produced deconstruction step has no Deconstruct method since the tuple already has distinct elements.
        /// </summary>
        static private BoundDeconstructionDeconstructStep MakeTupleDeconstructStep(
                                                        BoundDeconstructValuePlaceholder targetPlaceholder,
                                                        AssignmentExpressionSyntax syntax,
                                                        DiagnosticBag diagnostics,
                                                        ImmutableArray<DeconstructionVariable> variables,
                                                        ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                                                        ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps)
        {
            Debug.Assert(targetPlaceholder.Type.IsTupleType);

            var tupleTypes = targetPlaceholder.Type.TupleElementTypes;
            if (variables.Length != tupleTypes.Length)
            {
                Error(diagnostics, ErrorCode.ERR_DeconstructWrongCardinality, syntax, tupleTypes.Length, variables.Length);
                return null;
            }

            return new BoundDeconstructionDeconstructStep(syntax, null, targetPlaceholder, tupleTypes.SelectAsArray((t, s) => new BoundDeconstructValuePlaceholder(s, t), syntax));
        }

        /// <summary>
        /// This will generate and stack appropriate deconstruction and assignment steps for a non-tuple type.
        /// Returns null if there was an error (if a suitable Deconstruct method was not found).
        /// </summary>
        private BoundDeconstructionDeconstructStep MakeNonTupleDeconstructStep(
                                                            BoundDeconstructValuePlaceholder targetPlaceholder,
                                                            AssignmentExpressionSyntax syntax,
                                                            DiagnosticBag diagnostics,
                                                            ImmutableArray<DeconstructionVariable> variables,
                                                            ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                                                            ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps)
        {
            // symbol and parameters for Deconstruct
            ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders;
            var deconstructInvocation = MakeDeconstructInvocationExpression(variables.Length, targetPlaceholder, syntax, diagnostics, out outPlaceholders);

            if (deconstructInvocation.HasAnyErrors)
            {
                return null;
            }
            else
            {
                return new BoundDeconstructionDeconstructStep(syntax, deconstructInvocation, targetPlaceholder, outPlaceholders);
            }
        }

        /// <summary>
        /// Holds the variables on the LHS of a deconstruction as a tree of bound expressions.
        /// </summary>
        private class DeconstructionVariable
        {
            public readonly BoundExpression Single;
            public readonly ImmutableArray<DeconstructionVariable> Nested;

            public DeconstructionVariable(BoundExpression variable)
            {
                Single = variable;
                Nested = default(ImmutableArray<DeconstructionVariable>);
            }

            public DeconstructionVariable(ImmutableArray<DeconstructionVariable> variables)
            {
                Single = null;
                Nested = variables;
            }

            public bool IsNested => !Nested.IsDefault;
        }

        /// <summary>
        /// Takes the outputs from the previous deconstructionStep and depending on the structure of variables, will generate further deconstructions, or simply assignments.
        /// Returns true for success, but false if has errors.
        /// </summary>
        private bool DeconstructOrAssignOutputs(
                        BoundDeconstructionDeconstructStep deconstructionStep,
                        ImmutableArray<DeconstructionVariable> variables,
                        AssignmentExpressionSyntax syntax,
                        DiagnosticBag diagnostics,
                        ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                        ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps)
        {
            bool hasErrors = false;

            for (int i = 0; i < variables.Length; i++)
            {
                var variable = variables[i];
                var valuePlaceholder = deconstructionStep.OutputPlaceholders[i];

                if (variable.IsNested)
                {
                    if (!DeconstructIntoSteps(valuePlaceholder, syntax, diagnostics, variable.Nested, deconstructionSteps, assignmentSteps))
                    {
                        hasErrors = true;
                    }
                }
                else
                {
                    var assignment = MakeDeconstructionAssignmentStep(variable.Single, valuePlaceholder.Type, valuePlaceholder, syntax, diagnostics);
                    assignmentSteps.Add(assignment);
                }
            }

            return !hasErrors;
        }

        /// <summary>
        /// For cases where the RHS of a deconstruction-assignment has no type (TupleLiteral), we squint and look at the LHS as a tuple type to give the RHS a type.
        /// </summary>
        static private TypeSymbol MakeTupleTypeFromDeconstructionLHS(ImmutableArray<DeconstructionVariable> topLevelCheckedVariables, DiagnosticBag diagnostics, CSharpCompilation compilation)
        {
            var typesBuilder = ArrayBuilder<TypeSymbol>.GetInstance(topLevelCheckedVariables.Length);
            foreach (var variable in topLevelCheckedVariables)
            {
                if (variable.IsNested)
                {
                    typesBuilder.Add(MakeTupleTypeFromDeconstructionLHS(variable.Nested, diagnostics, compilation));
                }
                else
                {
                    typesBuilder.Add(variable.Single.Type);
                }
            }

            return TupleTypeSymbol.Create(locationOpt: null, elementTypes: typesBuilder.ToImmutableAndFree(), elementLocations: default(ImmutableArray<Location>), elementNames: default(ImmutableArray<string>), compilation: compilation, diagnostics: diagnostics);
        }

        /// <summary>
        /// Returns an array of variables, where some may be nested variables (BoundDeconstructionVariables).
        /// Checks that all the variables are assignable to.
        /// </summary>
        private ImmutableArray<DeconstructionVariable> BindDeconstructionVariables(SeparatedSyntaxList<ArgumentSyntax> arguments, DiagnosticBag diagnostics)
        {
            int numElements = arguments.Count;
            Debug.Assert(numElements >= 2); // this should not have parsed as a tuple.

            // bind the variables and check they can be assigned to
            var checkedVariablesBuilder = ArrayBuilder<DeconstructionVariable>.GetInstance(numElements);

            foreach (var argument in arguments)
            {
                if (argument.Expression.Kind() == SyntaxKind.TupleExpression) // nested tuple case
                {
                    var nestedArguments = ((TupleExpressionSyntax)argument.Expression).Arguments;
                    checkedVariablesBuilder.Add(new DeconstructionVariable(BindDeconstructionVariables(nestedArguments, diagnostics)));
                }
                else
                {
                    var boundVariable = BindExpression(argument.Expression, diagnostics, invoked: false, indexed: false);
                    var checkedVariable = CheckValue(boundVariable, BindValueKind.Assignment, diagnostics);

                    checkedVariablesBuilder.Add(new DeconstructionVariable(checkedVariable));
                }
            }

            var checkedVariables = checkedVariablesBuilder.ToImmutableAndFree();
            return checkedVariables;
        }

        /// <summary>
        /// Figures out how to assign from sourceType into receivingVariable and bundles the information (leaving holes for the actual source and receiver) into an AssignmentInfo.
        /// </summary>
        private BoundDeconstructionAssignmentStep MakeDeconstructionAssignmentStep(
                                                    BoundExpression receivingVariable, TypeSymbol sourceType, BoundDeconstructValuePlaceholder inputPlaceholder,
                                                    AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var outputPlaceholder = new BoundDeconstructValuePlaceholder(receivingVariable.Syntax, receivingVariable.Type) { WasCompilerGenerated = true };

            // each assignment has a placeholder for a receiver and another for the source
            BoundAssignmentOperator op = BindAssignment(node, outputPlaceholder, inputPlaceholder, diagnostics);

            return new BoundDeconstructionAssignmentStep(node, op, inputPlaceholder, outputPlaceholder);
        }

        static private ImmutableArray<BoundExpression> FlattenDeconstructVariables(ImmutableArray<DeconstructionVariable> variables)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance(variables.Length);
            FlattenDeconstructVariables(variables, builder);

            return builder.ToImmutableAndFree();
        }

        static private void FlattenDeconstructVariables(ImmutableArray<DeconstructionVariable> variables, ArrayBuilder<BoundExpression> builder)
        {
            foreach (var variable in variables)
            {
                if (variable.IsNested)
                {
                    FlattenDeconstructVariables(variable.Nested, builder);
                }
                else
                {
                    builder.Add(variable.Single);
                }
            }
        }

        /// <summary>
        /// Find the Deconstruct method for the expression on the right, that will fit the number of assignable variables on the left.
        /// Returns an invocation expression if the Deconstruct method is found.
        ///     If so, it outputs placeholders that were coerced to the output types of the resolved Deconstruct method.
        /// The overload resolution is similar to writing `receiver.Deconstruct(out var x1, out var x2, ...)`.
        /// </summary>
        private BoundExpression MakeDeconstructInvocationExpression(
                                    int numCheckedVariables, BoundExpression receiver, AssignmentExpressionSyntax assignmentSyntax,
                                    DiagnosticBag diagnostics, out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders)
        {
            var receiverSyntax = receiver.Syntax;

            if (receiver.Type.IsDynamic())
            {
                Error(diagnostics, ErrorCode.ERR_CannotDeconstructDynamic, receiverSyntax);
                outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

                return BadExpression(receiverSyntax, receiver);
            }

            var analyzedArguments = AnalyzedArguments.GetInstance();
            var outVars = ArrayBuilder<OutDeconstructVarPendingInference>.GetInstance(numCheckedVariables);
            DiagnosticBag bag = null;

            try
            {
                for (int i = 0; i < numCheckedVariables; i++)
                {
                    var variable = new OutDeconstructVarPendingInference(assignmentSyntax);
                    analyzedArguments.Arguments.Add(variable);
                    analyzedArguments.RefKinds.Add(RefKind.Out);
                    outVars.Add(variable);
                }

                const string methodName = "Deconstruct";
                var memberAccess = BindInstanceMemberAccess(
                                        receiverSyntax, receiverSyntax, receiver, methodName, rightArity: 0,
                                        typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>), typeArguments: default(ImmutableArray<TypeSymbol>),
                                        invoked: true, diagnostics: diagnostics);

                memberAccess = CheckValue(memberAccess, BindValueKind.RValueOrMethodGroup, diagnostics);
                memberAccess.WasCompilerGenerated = true;

                if (memberAccess.Kind != BoundKind.MethodGroup)
                {
                    return MissingDeconstruct(receiver, assignmentSyntax, numCheckedVariables, diagnostics, out outPlaceholders, receiver);
                }

                // After the overload resolution completes, the last step is to coerce the arguments with inferred types.
                // That step returns placeholder (of correct type) instead of the outVar nodes that were passed in as arguments.
                // So the generated invocation expression will contain placeholders instead of those outVar nodes.
                // Those placeholders are also recorded in the outVar for easy access below, by the `SetInferredType` call on the outVar nodes.
                bag = DiagnosticBag.GetInstance();
                BoundExpression result = BindMethodGroupInvocation(
                                            receiverSyntax, receiverSyntax, methodName, (BoundMethodGroup)memberAccess, analyzedArguments, bag, queryClause: null,
                                            allowUnexpandedForm: true);

                result.WasCompilerGenerated = true;
                diagnostics.AddRange(bag);

                if (bag.HasAnyErrors())
                {
                    return MissingDeconstruct(receiver, assignmentSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                // Verify all the parameters (except "this" for extension methods) are out parameters
                if (result.Kind != BoundKind.Call)
                {
                    return MissingDeconstruct(receiver, assignmentSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                var deconstructMethod = ((BoundCall)result).Method;
                var parameters = deconstructMethod.Parameters;
                for (int i = (deconstructMethod.IsExtensionMethod ? 1 : 0); i < parameters.Length; i++)
                {
                    if (parameters[i].RefKind != RefKind.Out)
                    {
                        return MissingDeconstruct(receiver, assignmentSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                    }
                }

                if (outVars.Any(v => (object)v.Placeholder == null))
                {
                    return MissingDeconstruct(receiver, assignmentSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                outPlaceholders = outVars.SelectAsArray(v => v.Placeholder);

                return result;
            }
            finally
            {
                analyzedArguments.Free();
                outVars.Free();

                if (bag != null)
                {
                    bag.Free();
                }
            }

        }

        private BoundExpression MissingDeconstruct(BoundExpression receiver, AssignmentExpressionSyntax syntax, int numParameters, DiagnosticBag diagnostics, out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders, BoundNode childNode)
        {
            Error(diagnostics, ErrorCode.ERR_MissingDeconstruct, receiver.Syntax, receiver.Type, numParameters);
            outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

            return BadExpression(syntax, childNode);
        }
    }
}

