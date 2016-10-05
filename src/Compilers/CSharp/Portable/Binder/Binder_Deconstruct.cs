// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts deconstruction-assignment syntax (AssignmentExpressionSyntax nodes with the left being a tuple expression)
    /// into a BoundDeconstructionAssignmentOperator (or bad node).
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindDeconstructionAssignment(AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var left = (TupleExpressionSyntax)node.Left;
            ArrayBuilder<DeconstructionVariable> checkedVariables = BindDeconstructionAssignmentVariables(left.Arguments, left, diagnostics);

            var result = BindDeconstructionAssignment(node, node.Right, checkedVariables, diagnostics, isDeclaration: false);
            FreeDeconstructionVariables(checkedVariables);

            return result;
        }

        private static void FreeDeconstructionVariables(ArrayBuilder<DeconstructionVariable> variables)
        {
            foreach (var v in variables)
            {
                if (v.HasNestedVariables)
                {
                    FreeDeconstructionVariables(v.NestedVariables);
                }
            }

            variables.Free();
        }

        /// <summary>
        /// There are two kinds of deconstruction-assignments which this binding handles: tuple and non-tuple.
        ///
        /// Returns a BoundDeconstructionAssignmentOperator with a list of deconstruction steps and assignment steps.
        /// Deconstruct steps for tuples have no invocation to Deconstruct, but steps for non-tuples do.
        /// The caller is responsible for releasing all the ArrayBuilders in checkedVariables.
        /// </summary>
        private BoundDeconstructionAssignmentOperator BindDeconstructionAssignment(
                                                        CSharpSyntaxNode node,
                                                        ExpressionSyntax right,
                                                        ArrayBuilder<DeconstructionVariable> checkedVariables,
                                                        DiagnosticBag diagnostics,
                                                        bool isDeclaration,
                                                        BoundDeconstructValuePlaceholder rhsPlaceholder = null)
        {
            // receiver for first Deconstruct step
            var boundRHS = rhsPlaceholder ?? BindValue(right, diagnostics, BindValueKind.RValue);

            boundRHS = FixTupleLiteral(checkedVariables, boundRHS, node, diagnostics);

            if ((object)boundRHS.Type == null)
            {
                // we could still not infer a type for the RHS
                FailRemainingInferences(checkedVariables, diagnostics);

                return new BoundDeconstructionAssignmentOperator(
                            node, isDeclaration, FlattenDeconstructVariables(checkedVariables), boundRHS,
                            ImmutableArray<BoundDeconstructionDeconstructStep>.Empty,
                            ImmutableArray<BoundDeconstructionAssignmentStep>.Empty,
                            ImmutableArray<BoundDeconstructionAssignmentStep>.Empty,
                            ImmutableArray<BoundDeconstructionConstructionStep>.Empty,
                            GetSpecialType(SpecialType.System_Void, diagnostics, node),
                            hasErrors: true);
            }

            var deconstructionSteps = ArrayBuilder<BoundDeconstructionDeconstructStep>.GetInstance(1);
            var conversionSteps = ArrayBuilder<BoundDeconstructionAssignmentStep>.GetInstance(1);
            var assignmentSteps = ArrayBuilder<BoundDeconstructionAssignmentStep>.GetInstance(1);
            var constructionStepsOpt = isDeclaration ? null : ArrayBuilder<BoundDeconstructionConstructionStep>.GetInstance(1);

            bool hasErrors = !DeconstructIntoSteps(
                                    new BoundDeconstructValuePlaceholder(boundRHS.Syntax, boundRHS.Type),
                                    node,
                                    diagnostics,
                                    checkedVariables,
                                    deconstructionSteps,
                                    conversionSteps,
                                    assignmentSteps,
                                    constructionStepsOpt);

            TypeSymbol returnType = isDeclaration ?
                                            GetSpecialType(SpecialType.System_Void, diagnostics, node) :
                                            hasErrors ?
                                                CreateErrorType() :
                                                constructionStepsOpt.Last().OutputPlaceholder.Type;

            var deconstructions = deconstructionSteps.ToImmutableAndFree();
            var conversions = conversionSteps.ToImmutableAndFree();
            var assignments = assignmentSteps.ToImmutableAndFree();
            var constructions = isDeclaration ? default(ImmutableArray<BoundDeconstructionConstructionStep>) : constructionStepsOpt.ToImmutableAndFree();

            FailRemainingInferences(checkedVariables, diagnostics);

            return new BoundDeconstructionAssignmentOperator(
                            node, isDeclaration, FlattenDeconstructVariables(checkedVariables), boundRHS,
                            deconstructions, conversions, assignments, constructions, returnType, hasErrors: hasErrors);
        }

        private BoundExpression FixTupleLiteral(ArrayBuilder<DeconstructionVariable> checkedVariables, BoundExpression boundRHS, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            if (boundRHS.Kind == BoundKind.TupleLiteral)
            {
                // Let's fix the literal up by figuring out its type
                // For declarations, that means merging type information from the LHS and RHS
                // For assignments, only the LHS side matters since it is necessarily typed
                TypeSymbol mergedTupleType = MakeMergedTupleType(checkedVariables, (BoundTupleLiteral)boundRHS, syntax, Compilation, diagnostics);
                if ((object)mergedTupleType != null)
                {
                    boundRHS = GenerateConversionForAssignment(mergedTupleType, boundRHS, diagnostics);
                }
            }
            else if ((object)boundRHS.Type == null)
            {
                Error(diagnostics, ErrorCode.ERR_DeconstructRequiresExpression, boundRHS.Syntax);
            }

            return boundRHS;
        }

        /// <summary>
        /// Whether the target is a tuple or a type that requires Deconstruction, this will generate and stack appropriate deconstruction and assignment steps.
        /// Note that the variables may either be plain or nested variables.
        /// The variables may be updated with inferred types if they didn't have types initially.
        /// Returns false if there was an error.
        /// Pass in constructionStepsOpt as null if construction steps should not be computed.
        /// </summary>
        private bool DeconstructIntoSteps(
                        BoundDeconstructValuePlaceholder targetPlaceholder,
                        CSharpSyntaxNode syntax,
                        DiagnosticBag diagnostics,
                        ArrayBuilder<DeconstructionVariable> variables,
                        ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                        ArrayBuilder<BoundDeconstructionAssignmentStep> conversionSteps,
                        ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps,
                        ArrayBuilder<BoundDeconstructionConstructionStep> constructionStepsOpt)
        {
            Debug.Assert(targetPlaceholder.Type != null);

            BoundDeconstructionDeconstructStep step;

            if (targetPlaceholder.Type.IsTupleType)
            {
                // tuple literal such as `(1, 2)`, `(null, null)`, `(x.P, y.M())`
                step = MakeTupleDeconstructStep(targetPlaceholder, syntax, diagnostics, variables);
            }
            else
            {
                step = MakeNonTupleDeconstructStep(targetPlaceholder, syntax, diagnostics, variables);
            }

            if (step == null)
            {
                return false;
            }

            deconstructionSteps.Add(step);

            // outputs will either need a conversion step and assignment step, or if they are nested variables, they will need further deconstruction
            return DeconstructOrAssignOutputs(step, variables, syntax, diagnostics, deconstructionSteps, conversionSteps, assignmentSteps, constructionStepsOpt);
        }

        /// <summary>
        /// The produces a deconstruction step with no Deconstruct method since the tuple already has distinct elements.
        /// </summary>
        private BoundDeconstructionDeconstructStep MakeTupleDeconstructStep(
                                                        BoundDeconstructValuePlaceholder targetPlaceholder,
                                                        CSharpSyntaxNode syntax,
                                                        DiagnosticBag diagnostics,
                                                        ArrayBuilder<DeconstructionVariable> variables)
        {
            Debug.Assert(targetPlaceholder.Type.IsTupleType);

            var tupleTypes = targetPlaceholder.Type.TupleElementTypes;
            SetInferredTypes(variables, tupleTypes, diagnostics);

            if (variables.Count != tupleTypes.Length)
            {
                Error(diagnostics, ErrorCode.ERR_DeconstructWrongCardinality, syntax, tupleTypes.Length, variables.Count);
                return null;
            }

            return new BoundDeconstructionDeconstructStep(syntax, null, targetPlaceholder, tupleTypes.SelectAsArray((t, i, v) => new BoundDeconstructValuePlaceholder(v[i].Syntax, t), variables));
        }

        /// <summary>
        /// This will generate and stack appropriate deconstruction and assignment steps for a non-tuple type.
        /// Returns null if there was an error (if a suitable Deconstruct method was not found).
        /// </summary>
        private BoundDeconstructionDeconstructStep MakeNonTupleDeconstructStep(
                                                            BoundDeconstructValuePlaceholder targetPlaceholder,
                                                            CSharpSyntaxNode syntax,
                                                            DiagnosticBag diagnostics,
                                                            ArrayBuilder<DeconstructionVariable> variables)
        {
            // symbol and parameters for Deconstruct
            ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders;
            var deconstructInvocation = MakeDeconstructInvocationExpression(variables.Count, targetPlaceholder, syntax, diagnostics, out outPlaceholders);
            if (deconstructInvocation.HasAnyErrors)
            {
                return null;
            }

            SetInferredTypes(variables, outPlaceholders.SelectAsArray(p => p.Type), diagnostics);

            return new BoundDeconstructionDeconstructStep(syntax, deconstructInvocation, targetPlaceholder, outPlaceholders);
        }

        /// <summary>
        /// Inform the variables about found types.
        /// </summary>
        private void SetInferredTypes(ArrayBuilder<DeconstructionVariable> variables, ImmutableArray<TypeSymbol> foundTypes, DiagnosticBag diagnostics)
        {
            var matchCount = Math.Min(variables.Count, foundTypes.Length);
            for (int i = 0; i < matchCount; i++)
            {
                var variable = variables[i];
                if (!variable.HasNestedVariables && variable.Single.Kind == BoundKind.DeconstructionVariablePendingInference)
                {
                    BoundExpression local = ((DeconstructionVariablePendingInference)variable.Single).SetInferredType(foundTypes[i], this, diagnostics);
                    variables[i] = new DeconstructionVariable(local, local.Syntax);
                }
            }
        }

        /// <summary>
        /// Find any deconstruction locals that are still pending inference and fail their inference.
        /// </summary>
        private void FailRemainingInferences(ArrayBuilder<DeconstructionVariable> variables, DiagnosticBag diagnostics)
        {
            int count = variables.Count;
            for (int i = 0; i < count; i++)
            {
                var variable = variables[i];
                if (variable.HasNestedVariables)
                {
                    FailRemainingInferences(variable.NestedVariables, diagnostics);
                }
                else
                {
                    if (variable.Single.Kind == BoundKind.DeconstructionVariablePendingInference)
                    {
                        BoundExpression local = ((DeconstructionVariablePendingInference)variable.Single).FailInference(this, diagnostics);
                        variables[i] = new DeconstructionVariable(local, local.Syntax);
                    }
                }
            }
        }

        /// <summary>
        /// Holds the variables on the LHS of a deconstruction as a tree of bound expressions.
        /// </summary>
        private class DeconstructionVariable
        {
            public readonly BoundExpression Single;
            public readonly ArrayBuilder<DeconstructionVariable> NestedVariables;
            public readonly SyntaxNode Syntax;

            public DeconstructionVariable(BoundExpression variable, SyntaxNode syntax)
            {
                Single = variable;
                NestedVariables = null;
                Syntax = syntax;
            }

            public DeconstructionVariable(ArrayBuilder<DeconstructionVariable> variables, CSharpSyntaxNode syntax)
            {
                Single = null;
                NestedVariables = variables;
                Syntax = syntax;
            }

            public bool HasNestedVariables => NestedVariables != null;
        }

        /// <summary>
        /// Takes the outputs from the previous deconstructionStep and depending on the structure of variables, will:
        /// - generate further deconstructions,
        /// - or simply conversions and assignments.
        ///
        /// Returns true for success, but false if has errors.
        /// </summary>
        private bool DeconstructOrAssignOutputs(
                        BoundDeconstructionDeconstructStep deconstructionStep,
                        ArrayBuilder<DeconstructionVariable> variables,
                        CSharpSyntaxNode syntax,
                        DiagnosticBag diagnostics,
                        ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                        ArrayBuilder<BoundDeconstructionAssignmentStep> conversionSteps,
                        ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps,
                        ArrayBuilder<BoundDeconstructionConstructionStep> constructionStepsOpt)
        {
            bool hasErrors = false;
            var constructionInputs = constructionStepsOpt == null ? null : ArrayBuilder<BoundDeconstructValuePlaceholder>.GetInstance();

            int count = variables.Count;
            for (int i = 0; i < count; i++)
            {
                var variable = variables[i];
                var valuePlaceholder = deconstructionStep.OutputPlaceholders[i];

                if (variable.HasNestedVariables)
                {
                    var nested = variable.NestedVariables;
                    if (!DeconstructIntoSteps(valuePlaceholder, syntax, diagnostics, nested, deconstructionSteps, conversionSteps, assignmentSteps, constructionStepsOpt))
                    {
                        hasErrors = true;
                    }
                    else if (constructionInputs != null)
                    {
                        constructionInputs.Add(constructionStepsOpt.Last().OutputPlaceholder);
                    }
                }
                else
                {
                    var conversion = MakeDeconstructionAssignmentStep(variable.Single, valuePlaceholder, syntax, diagnostics);
                    conversionSteps.Add(conversion);

                    var assignment = MakeDeconstructionAssignmentStep(variable.Single, conversion.OutputPlaceholder, syntax, diagnostics);
                    assignmentSteps.Add(assignment);

                    if (constructionInputs != null)
                    {
                        constructionInputs.Add(conversion.OutputPlaceholder);
                    }
                }
            }

            if (constructionStepsOpt != null)
            {
                if (hasErrors)
                {
                    constructionInputs.Free();
                }
                else
                {
                    var construct = MakeDeconstructionConstructionStep(syntax, diagnostics, constructionInputs.ToImmutableAndFree());
                    constructionStepsOpt.Add(construct);
                }
            }

            return !hasErrors;
        }

        private BoundDeconstructionConstructionStep MakeDeconstructionConstructionStep(CSharpSyntaxNode node, DiagnosticBag diagnostics,
                                                        ImmutableArray<BoundDeconstructValuePlaceholder> constructionInputs)
        {
            var tuple = TupleTypeSymbol.Create(locationOpt: null,
                           elementTypes: constructionInputs.SelectAsArray(e => e.Type),
                           elementLocations: default(ImmutableArray<Location>),
                           elementNames: default(ImmutableArray<string>),
                           compilation: Compilation,
                           diagnostics: diagnostics,
                           syntax: node);

            var outputPlaceholder = new BoundDeconstructValuePlaceholder(node, tuple) { WasCompilerGenerated = true };

            BoundExpression construction = new BoundTupleLiteral(node, default(ImmutableArray<string>), constructionInputs.CastArray<BoundExpression>(), tuple);
            return new BoundDeconstructionConstructionStep(node, construction, outputPlaceholder);
        }

        /// <summary>
        /// For cases where the RHS of a deconstruction-declaration is a tuple literal, we merge type information from both the LHS and RHS.
        /// For cases where the RHS of a deconstruction-assignment is a tuple literal, the type information from the LHS determines the merged type, since all variables have a type.
        /// Returns null if a merged tuple type could not be fabricated.
        /// </summary>
        private static TypeSymbol MakeMergedTupleType(ArrayBuilder<DeconstructionVariable> lhsVariables, BoundTupleLiteral rhsLiteral, CSharpSyntaxNode syntax, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            int leftLength = lhsVariables.Count;
            int rightLength = rhsLiteral.Arguments.Length;

            var typesBuilder = ArrayBuilder<TypeSymbol>.GetInstance(leftLength);
            for (int i = 0; i < rightLength; i++)
            {
                BoundExpression element = rhsLiteral.Arguments[i];
                TypeSymbol mergedType = element.Type;

                if (i < leftLength)
                {
                    var variable = lhsVariables[i];
                    if (variable.HasNestedVariables)
                    {
                        if (element.Kind == BoundKind.TupleLiteral)
                        {
                            // (variables) on the left and (elements) on the right
                            mergedType = MakeMergedTupleType(variable.NestedVariables, (BoundTupleLiteral)element, syntax, compilation, diagnostics);
                        }
                        else if ((object)mergedType == null)
                        {
                            // (variables) on the left and null on the right
                            Error(diagnostics, ErrorCode.ERR_DeconstructRequiresExpression, element.Syntax);
                        }
                    }
                    else
                    {
                        if ((object)variable.Single.Type != null)
                        {
                            // typed-variable on the left
                            mergedType = variable.Single.Type;
                        }
                    }
                }
                else
                {
                    if ((object)mergedType == null)
                    {
                        // a typeless element on the right, matching no variable on the left
                        Error(diagnostics, ErrorCode.ERR_DeconstructRequiresExpression, element.Syntax);
                    }
                }

                typesBuilder.Add(mergedType);
            }

            if (typesBuilder.Any(t => t == null))
            {
                typesBuilder.Free();
                return null;
            }

            return TupleTypeSymbol.Create(locationOpt: null, elementTypes: typesBuilder.ToImmutableAndFree(), elementLocations: default(ImmutableArray<Location>),
                                    elementNames: default(ImmutableArray<string>), compilation: compilation, diagnostics: diagnostics);
        }

        /// <summary>
        /// Returns a list of variables, where some may be nested variables (BoundDeconstructionVariables).
        /// Checks that all the variables are assignable to.
        /// The caller is responsible for releasing the nested ArrayBuilders.
        /// </summary>
        private ArrayBuilder<DeconstructionVariable> BindDeconstructionAssignmentVariables(SeparatedSyntaxList<ArgumentSyntax> arguments, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            int numElements = arguments.Count;
            Debug.Assert(numElements >= 2); // this should not have parsed as a tuple.

            // bind the variables and check they can be assigned to
            var checkedVariablesBuilder = ArrayBuilder<DeconstructionVariable>.GetInstance(numElements);

            foreach (var argument in arguments)
            {
                if (argument.Expression.Kind() == SyntaxKind.TupleExpression) // nested tuple case
                {
                    var nested = (TupleExpressionSyntax)argument.Expression;
                    checkedVariablesBuilder.Add(new DeconstructionVariable(BindDeconstructionAssignmentVariables(nested.Arguments, nested, diagnostics), syntax));
                }
                else
                {
                    var boundVariable = BindExpression(argument.Expression, diagnostics, invoked: false, indexed: false);
                    var checkedVariable = CheckValue(boundVariable, BindValueKind.Assignment, diagnostics);

                    checkedVariablesBuilder.Add(new DeconstructionVariable(checkedVariable, argument));
                }
            }

            return checkedVariablesBuilder;
        }

        /// <summary>
        /// Figures out how to assign from inputPlaceholder into receivingVariable and bundles the information (leaving holes for the actual source and receiver) into an AssignmentInfo.
        /// </summary>
        private BoundDeconstructionAssignmentStep MakeDeconstructionAssignmentStep(
                                                    BoundExpression receivingVariable, BoundDeconstructValuePlaceholder inputPlaceholder,
                                                    CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            var outputPlaceholder = new BoundDeconstructValuePlaceholder(receivingVariable.Syntax, receivingVariable.Type) { WasCompilerGenerated = true };

            // each assignment has a placeholder for a receiver and another for the source
            BoundAssignmentOperator op = BindAssignment(receivingVariable.Syntax, outputPlaceholder, inputPlaceholder, diagnostics);

            return new BoundDeconstructionAssignmentStep(node, op, outputPlaceholder);
        }

        private static ImmutableArray<BoundExpression> FlattenDeconstructVariables(ArrayBuilder<DeconstructionVariable> variables)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance(variables.Count);
            FlattenDeconstructVariables(variables, builder);

            return builder.ToImmutableAndFree();
        }

        private static void FlattenDeconstructVariables(ArrayBuilder<DeconstructionVariable> variables, ArrayBuilder<BoundExpression> builder)
        {
            foreach (var variable in variables)
            {
                if (variable.HasNestedVariables)
                {
                    FlattenDeconstructVariables(variable.NestedVariables, builder);
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
                                    int numCheckedVariables, BoundExpression receiver, CSharpSyntaxNode syntax,
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

            try
            {
                for (int i = 0; i < numCheckedVariables; i++)
                {
                    var variable = new OutDeconstructVarPendingInference(syntax);
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
                    return MissingDeconstruct(receiver, syntax, numCheckedVariables, diagnostics, out outPlaceholders, receiver);
                }

                // After the overload resolution completes, the last step is to coerce the arguments with inferred types.
                // That step returns placeholder (of correct type) instead of the outVar nodes that were passed in as arguments.
                // So the generated invocation expression will contain placeholders instead of those outVar nodes.
                // Those placeholders are also recorded in the outVar for easy access below, by the `SetInferredType` call on the outVar nodes.
                BoundExpression result = BindMethodGroupInvocation(
                                            receiverSyntax, receiverSyntax, methodName, (BoundMethodGroup)memberAccess, analyzedArguments, diagnostics, queryClause: null,
                                            allowUnexpandedForm: true);

                result.WasCompilerGenerated = true;

                if (result.HasErrors && !receiver.HasAnyErrors)
                {
                    return MissingDeconstruct(receiver, syntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                // Verify all the parameters (except "this" for extension methods) are out parameters
                if (result.Kind != BoundKind.Call)
                {
                    return MissingDeconstruct(receiver, syntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                var deconstructMethod = ((BoundCall)result).Method;
                var parameters = deconstructMethod.Parameters;
                for (int i = (deconstructMethod.IsExtensionMethod ? 1 : 0); i < parameters.Length; i++)
                {
                    if (parameters[i].RefKind != RefKind.Out)
                    {
                        return MissingDeconstruct(receiver, syntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                    }
                }

                if (outVars.Any(v => (object)v.Placeholder == null))
                {
                    return MissingDeconstruct(receiver, syntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                outPlaceholders = outVars.SelectAsArray(v => v.Placeholder);

                return result;
            }
            finally
            {
                analyzedArguments.Free();
                outVars.Free();
            }
        }

        private BoundBadExpression MissingDeconstruct(BoundExpression receiver, CSharpSyntaxNode syntax, int numParameters, DiagnosticBag diagnostics,
                                    out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders, BoundNode childNode)
        {
            Error(diagnostics, ErrorCode.ERR_MissingDeconstruct, receiver.Syntax, receiver.Type, numParameters);
            outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

            return BadExpression(syntax, childNode);
        }

        internal BoundLocalDeconstructionDeclaration BindDeconstructionDeclarationStatement(DeconstructionDeclarationStatementSyntax node, DiagnosticBag diagnostics)
        {
            return new BoundLocalDeconstructionDeclaration(node, BindDeconstructionDeclaration(node, node.Assignment.VariableComponent, node.Assignment.Value, diagnostics));
        }

        internal BoundDeconstructionAssignmentOperator BindDeconstructionDeclaration(CSharpSyntaxNode node, VariableComponentSyntax declaration, ExpressionSyntax right,
                                                        DiagnosticBag diagnostics, BoundDeconstructValuePlaceholder rightPlaceholder = null)
        {
            DeconstructionVariable locals = BindDeconstructionDeclarationVariables(declaration, diagnostics);
            Debug.Assert(locals.HasNestedVariables);
            var result = BindDeconstructionAssignment(node, right, locals.NestedVariables, diagnostics, isDeclaration: true, rhsPlaceholder: rightPlaceholder);
            FreeDeconstructionVariables(locals.NestedVariables);
            return result;
        }

        /// <summary>
        /// Prepares locals (or fields in global statement) corresponding to the variables of the declaration.
        /// The locals/fields are kept in a tree which captures the nesting of variables.
        /// Each local or field is either a simple local or field access (when its type is known) or a deconstruction variable pending inference.
        /// The caller is responsible for releasing the nested ArrayBuilders.
        /// </summary>
        private DeconstructionVariable BindDeconstructionDeclarationVariables(
            VariableComponentSyntax node,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.TypedVariableComponent:
                    {
                        var component = (TypedVariableComponentSyntax)node;
                        return BindDeconstructionDeclarationVariables(component.Type, component.Designation, diagnostics);
                    }
                case SyntaxKind.ParenthesizedVariableComponent:
                    {
                        var component = (ParenthesizedVariableComponentSyntax)node;
                        var builder = ArrayBuilder<DeconstructionVariable>.GetInstance(component.Variables.Count);
                        foreach (var n in component.Variables)
                        {
                            builder.Add(BindDeconstructionDeclarationVariables(n, diagnostics));
                        }
                        return new DeconstructionVariable(builder, node);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private DeconstructionVariable BindDeconstructionDeclarationVariables(
            TypeSyntax type,
            VariableDesignationSyntax node,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    {
                        var single = (SingleVariableDesignationSyntax)node;
                        return new DeconstructionVariable(BindDeconstructionDeclarationVariable(type, single, diagnostics), node);
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        var tuple = (ParenthesizedVariableDesignationSyntax)node;
                        var builder = ArrayBuilder<DeconstructionVariable>.GetInstance();
                        foreach (var n in tuple.Variables)
                        {
                            builder.Add(BindDeconstructionDeclarationVariables(type, n, diagnostics));
                        }
                        return new DeconstructionVariable(builder, node);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        /// <summary>
        /// In embedded statements, returns a BoundLocal when the type was explicit.
        /// In global statements, returns a BoundFieldAccess when the type was explicit.
        /// Otherwise returns a DeconstructionVariablePendingInference when the type is implicit.
        /// </summary>
        private BoundExpression BindDeconstructionDeclarationVariable(
            TypeSyntax typeSyntax,
            SingleVariableDesignationSyntax designation,
            DiagnosticBag diagnostics)
        {
            SourceLocalSymbol localSymbol = LookupLocal(designation.Identifier);

            bool hasErrors = false;
            bool isVar;
            bool isConst = false;
            AliasSymbol alias;
            TypeSymbol declType = BindVariableType(designation, diagnostics, typeSyntax, ref isConst, out isVar, out alias);

            if (!isVar)
            {
                if (designation.Parent.Kind() == SyntaxKind.ParenthesizedVariableDesignation)
                {
                    // An explicit type can only be provided next to the variable
                    Error(diagnostics, ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, designation);
                    hasErrors = true;
                }
            }

            // is this a local?
            if (localSymbol != null)
            {
                // Check for variable declaration errors.
                // Use the binder that owns the scope for the local because this (the current) binder
                // might own nested scope.
                hasErrors |= localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

                if (!isVar)
                {
                    return new BoundLocal(designation, localSymbol, constantValueOpt: null, type: declType, hasErrors: hasErrors);
                }

                return new DeconstructionVariablePendingInference(designation, localSymbol, receiverOpt: null);
            }

            // Is this a field?
            GlobalExpressionVariable field = LookupDeclaredField(designation);

            if ((object)field == null)
            {
                // We should have the right binder in the chain, cannot continue otherwise.
                throw ExceptionUtilities.Unreachable;
            }

            BoundThisReference receiver = ThisReference(designation, this.ContainingType, hasErrors: false,
                                            wasCompilerGenerated: true);

            if (!isVar)
            {
                TypeSymbol fieldType = field.GetFieldType(this.FieldsBeingBound);
                return new BoundFieldAccess(designation,
                                            receiver,
                                            field,
                                            constantValueOpt: null,
                                            resultKind: LookupResultKind.Viable,
                                            type: fieldType,
                                            hasErrors: hasErrors);
            }

            return new DeconstructionVariablePendingInference(designation, field, receiver);
        }
    }
}
