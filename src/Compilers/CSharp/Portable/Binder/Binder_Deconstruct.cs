﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts deconstruction-assignment syntax (AssignmentExpressionSyntax nodes with the left
    /// being a tuple expression or declaration expression) into a BoundDeconstructionAssignmentOperator (or bad node).
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindDeconstruction(AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var left = node.Left;
            var right = node.Right;
            DeclarationExpressionSyntax declaration = null;
            ExpressionSyntax expression = null;
            var result = BindDeconstruction(node, left, right, diagnostics, ref declaration, ref expression);
            if (declaration != null)
            {
                // only allowed at the top level, or in a for loop
                switch (node.Parent?.Kind())
                {
                    case null:
                    case SyntaxKind.ExpressionStatement:
                        if (expression != null)
                        {
                            // We only allow assignment-only or declaration-only deconstructions at this point.
                            // Issue https://github.com/dotnet/roslyn/issues/15050 tracks allowing mixed deconstructions.
                            // For now we give an error when you mix.
                            Error(diagnostics, ErrorCode.ERR_MixedDeconstructionUnsupported, left);
                        }
                        break;
                    case SyntaxKind.ForStatement:
                        if (((ForStatementSyntax)node.Parent).Initializers.Contains(node))
                        {
                            if (expression != null)
                            {
                                Error(diagnostics, ErrorCode.ERR_MixedDeconstructionUnsupported, left);
                            }
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_DeclarationExpressionNotPermitted, declaration);
                        }
                        break;
                    default:
                        Error(diagnostics, ErrorCode.ERR_DeclarationExpressionNotPermitted, declaration);
                        break;
                }
            }

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
                                                        ExpressionSyntax left,
                                                        ExpressionSyntax right,
                                                        ArrayBuilder<DeconstructionVariable> checkedVariables,
                                                        DiagnosticBag diagnostics,
                                                        BoundDeconstructValuePlaceholder rhsPlaceholder = null)
        {
            // receiver for first Deconstruct step
            var boundRHS = rhsPlaceholder ?? BindValue(right, diagnostics, BindValueKind.RValue);

            boundRHS = FixTupleLiteral(checkedVariables, boundRHS, node, diagnostics);

            if ((object)boundRHS.Type == null || boundRHS.Type.IsErrorType())
            {
                // we could still not infer a type for the RHS
                FailRemainingInferences(checkedVariables, diagnostics);

                return new BoundDeconstructionAssignmentOperator(
                            node, DeconstructVariablesAsTuple(left, checkedVariables), boundRHS,
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
            var constructionStepsOpt = ArrayBuilder<BoundDeconstructionConstructionStep>.GetInstance(1);

            bool hasErrors = !DeconstructIntoSteps(
                                    new BoundDeconstructValuePlaceholder(boundRHS.Syntax, boundRHS.Type),
                                    node,
                                    diagnostics,
                                    checkedVariables,
                                    deconstructionSteps,
                                    conversionSteps,
                                    assignmentSteps,
                                    constructionStepsOpt);

            TypeSymbol returnType = hasErrors ?
                                    CreateErrorType() :
                                    constructionStepsOpt.Last().OutputPlaceholder.Type;

            var deconstructions = deconstructionSteps.ToImmutableAndFree();
            var conversions = conversionSteps.ToImmutableAndFree();
            var assignments = assignmentSteps.ToImmutableAndFree();
            var constructions = constructionStepsOpt.ToImmutableAndFree();

            FailRemainingInferences(checkedVariables, diagnostics);

            return new BoundDeconstructionAssignmentOperator(
                            node, DeconstructVariablesAsTuple(left, checkedVariables), boundRHS,
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
                if (!variable.HasNestedVariables)
                {
                    var pending = variable.Single;
                    if ((object)pending.Type != null)
                    {
                        continue;
                    }

                    variables[i] = new DeconstructionVariable(SetInferredType(pending, foundTypes[i], diagnostics), variable.Syntax);
                }
            }
        }

        private BoundExpression SetInferredType(BoundExpression expression, TypeSymbol type, DiagnosticBag diagnostics)
        {
            switch (expression.Kind)
            {
                case BoundKind.DeconstructionVariablePendingInference:
                    {
                        var pending = (DeconstructionVariablePendingInference)expression;
                        return pending.SetInferredType(type, this, diagnostics);
                    }
                case BoundKind.DiscardExpression:
                    {
                        var pending = (BoundDiscardExpression)expression;
                        Debug.Assert((object)pending.Type == null);
                        return pending.SetInferredType(type);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(expression.Kind);
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
                    switch (variable.Single.Kind)
                    {
                        case BoundKind.DeconstructionVariablePendingInference:
                            BoundExpression local = ((DeconstructionVariablePendingInference)variable.Single).FailInference(this, diagnostics);
                            variables[i] = new DeconstructionVariable(local, local.Syntax);
                            break;
                        case BoundKind.DiscardExpression:
                            var pending = (BoundDiscardExpression)variable.Single;
                            if ((object)pending.Type == null)
                            {
                                Error(diagnostics, ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, pending.Syntax, "_");
                                variables[i] = new DeconstructionVariable(pending.FailInference(this, diagnostics), pending.Syntax);
                            }
                            break;
                    }

                    // at this point we expect to have a type for every lvalue
                    Debug.Assert((object)variables[i].Single.Type != null);
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
            var tuple = TupleTypeSymbol.Create(
                locationOpt: null,
                elementTypes: constructionInputs.SelectAsArray(e => e.Type),
                elementLocations: constructionInputs.SelectAsArray(e => e.Syntax.Location),
                elementNames: default(ImmutableArray<string>),
                compilation: Compilation,
                diagnostics: diagnostics,
                syntax: node,
                shouldCheckConstraints: true);

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

            // The tuple created here is not identical to the one created by
            // MakeDeconstructionConstructionStep. It represents a smaller
            // tree of types used for figuring out natural types in tuple literal.
            // Therefore, we do not check constraints here as it would report errors
            // that are already reported later. MakeDeconstructionConstructionStep
            // constructs the final tuple type and checks constraints.
            return TupleTypeSymbol.Create(
                locationOpt: null,
                elementTypes: typesBuilder.ToImmutableAndFree(),
                elementLocations: default(ImmutableArray<Location>),
                elementNames: default(ImmutableArray<string>),
                compilation: compilation,
                diagnostics: diagnostics,
                shouldCheckConstraints: false);
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

        private BoundTupleLiteral DeconstructVariablesAsTuple(SyntaxNode syntax, ArrayBuilder<DeconstructionVariable> variables)
        {
            int count = variables.Count;
            var valuesBuilder = ArrayBuilder<BoundExpression>.GetInstance(count);
            var typesBuilder = ArrayBuilder<TypeSymbol>.GetInstance(count);
            var locationsBuilder = ArrayBuilder<Location>.GetInstance(count);
            foreach (var variable in variables)
            {
                if (variable.HasNestedVariables)
                {
                    var nestedTuple = DeconstructVariablesAsTuple(variable.Syntax, variable.NestedVariables);
                    valuesBuilder.Add(nestedTuple);
                    typesBuilder.Add(nestedTuple.Type);
                }
                else
                {
                    var single = variable.Single;
                    valuesBuilder.Add(single);
                    typesBuilder.Add(single.Type);
                }
                locationsBuilder.Add(variable.Syntax.Location);
            }

            // MakeDeconstructionConstructionStep constructs the final tuple type and checks constraints already.
            var type = TupleTypeSymbol.Create(syntax.Location,
                typesBuilder.ToImmutableAndFree(), locationsBuilder.ToImmutableAndFree(),
                elementNames: default(ImmutableArray<string>), compilation: this.Compilation, shouldCheckConstraints: false);

            return new BoundTupleLiteral(syntax: syntax, argumentNamesOpt: default(ImmutableArray<string>),
                arguments: valuesBuilder.ToImmutableAndFree(), type: type);
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
            if (numCheckedVariables < 2)
            {
                Error(diagnostics, ErrorCode.ERR_DeconstructTooFewElements, receiverSyntax);
                outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

                return BadExpression(receiverSyntax, receiver);
            }

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

                if (deconstructMethod.ReturnType.GetSpecialTypeSafe() != SpecialType.System_Void)
                {
                    return MissingDeconstruct(receiver, syntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
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

        /// <summary>
        /// Bind a deconstruction assignment.
        /// </summary>
        /// <param name="deconstruction">The deconstruction operation</param>
        /// <param name="left">The left (tuple) operand</param>
        /// <param name="right">The right (deconstrucible) operand</param>
        /// <param name="diagnostics">Where to report diagnostics</param>
        /// <param name="declaration">A variable set to the first variable declaration found in the left</param>
        /// <param name="expression">A variable set to the first expression in the left that isn't a declaration or discard</param>
        /// <param name="rightPlaceholder"></param>
        /// <returns></returns>
        internal BoundDeconstructionAssignmentOperator BindDeconstruction(
            CSharpSyntaxNode deconstruction,
            ExpressionSyntax left,
            ExpressionSyntax right,
            DiagnosticBag diagnostics,
            ref DeclarationExpressionSyntax declaration,
            ref ExpressionSyntax expression,
            BoundDeconstructValuePlaceholder rightPlaceholder = null)
        {
            DeconstructionVariable locals = BindDeconstructionVariables(left, diagnostics, ref declaration, ref expression);
            Debug.Assert(locals.HasNestedVariables);
            var result = BindDeconstructionAssignment(deconstruction, left, right, locals.NestedVariables, diagnostics, rhsPlaceholder: rightPlaceholder);
            FreeDeconstructionVariables(locals.NestedVariables);
            return result;
        }

        /// <summary>
        /// Prepares locals (or fields in global statement) and lvalue expressions corresponding to the variables of the declaration.
        /// The locals/fields/lvalues are kept in a tree which captures the nesting of variables.
        /// Each local or field is either a simple local or field access (when its type is known) or a deconstruction variable pending inference.
        /// The caller is responsible for releasing the nested ArrayBuilders.
        /// </summary>
        private DeconstructionVariable BindDeconstructionVariables(
            ExpressionSyntax node,
            DiagnosticBag diagnostics,
            ref DeclarationExpressionSyntax declaration,
            ref ExpressionSyntax expression)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DeclarationExpression:
                    {
                        var component = (DeclarationExpressionSyntax)node;
                        if (declaration == null)
                        {
                            declaration = component;
                        }

                        bool isVar;
                        bool isConst = false;
                        AliasSymbol alias;
                        TypeSymbol declType = BindVariableType(component.Designation, diagnostics, component.Type, ref isConst, out isVar, out alias);
                        Debug.Assert(isVar == ((object)declType == null));
                        if (component.Designation.Kind() == SyntaxKind.ParenthesizedVariableDesignation && !isVar)
                        {
                            // An explicit is not allowed with a parenthesized designation
                            Error(diagnostics, ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, component.Designation);
                        }

                        return BindDeconstructionVariables(declType, component.Designation, component, diagnostics);
                    }
                case SyntaxKind.TupleExpression:
                    {
                        var component = (TupleExpressionSyntax)node;
                        var builder = ArrayBuilder<DeconstructionVariable>.GetInstance(component.Arguments.Count);
                        foreach (var arg in component.Arguments)
                        {
                            if (arg.NameColon != null)
                            {
                                Error(diagnostics, ErrorCode.ERR_TupleElementNamesInDeconstruction, arg.NameColon);
                            }

                            builder.Add(BindDeconstructionVariables(arg.Expression, diagnostics, ref declaration, ref expression));
                        }

                        return new DeconstructionVariable(builder, node);
                    }
                default:
                    var boundVariable = BindExpression(node, diagnostics, invoked: false, indexed: false);
                    var checkedVariable = CheckValue(boundVariable, BindValueKind.Assignment, diagnostics);
                    if (expression == null && checkedVariable.Kind != BoundKind.DiscardExpression)
                    {
                        expression = node;
                    }

                    return new DeconstructionVariable(checkedVariable, node);
            }
        }

        private DeconstructionVariable BindDeconstructionVariables(
            TypeSymbol declType,
            VariableDesignationSyntax node,
            CSharpSyntaxNode syntax,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    {
                        var single = (SingleVariableDesignationSyntax)node;
                        return new DeconstructionVariable(BindDeconstructionVariable(declType, single, syntax, diagnostics), syntax);
                    }
                case SyntaxKind.DiscardDesignation:
                    {
                        var discarded = (DiscardDesignationSyntax)node;
                        return new DeconstructionVariable(BindDiscardExpression(syntax, declType), syntax);
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        var tuple = (ParenthesizedVariableDesignationSyntax)node;
                        var builder = ArrayBuilder<DeconstructionVariable>.GetInstance();
                        foreach (var n in tuple.Variables)
                        {
                            builder.Add(BindDeconstructionVariables(declType, n, n, diagnostics));
                        }
                        return new DeconstructionVariable(builder, syntax);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private static BoundDiscardExpression BindDiscardExpression(
            SyntaxNode syntax,
            TypeSymbol declType)
        {
            return new BoundDiscardExpression(syntax, declType);
        }

        /// <summary>
        /// In embedded statements, returns a BoundLocal when the type was explicit.
        /// In global statements, returns a BoundFieldAccess when the type was explicit.
        /// Otherwise returns a DeconstructionVariablePendingInference when the type is implicit.
        /// </summary>
        private BoundExpression BindDeconstructionVariable(
            TypeSymbol declType,
            SingleVariableDesignationSyntax designation,
            CSharpSyntaxNode syntax,
            DiagnosticBag diagnostics)
        {
            SourceLocalSymbol localSymbol = LookupLocal(designation.Identifier);

            // is this a local?
            if ((object)localSymbol != null)
            {
                // Check for variable declaration errors.
                // Use the binder that owns the scope for the local because this (the current) binder
                // might own nested scope.
                var hasErrors = localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

                if ((object)declType != null)
                {
                    return new BoundLocal(syntax, localSymbol, isDeclaration: true, constantValueOpt: null, type: declType, hasErrors: hasErrors);
                }

                return new DeconstructionVariablePendingInference(syntax, localSymbol, receiverOpt: null);
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

            if ((object)declType != null)
            {
                TypeSymbol fieldType = field.GetFieldType(this.FieldsBeingBound);
                Debug.Assert(declType == fieldType);
                return new BoundFieldAccess(designation,
                                            receiver,
                                            field,
                                            constantValueOpt: null,
                                            resultKind: LookupResultKind.Viable,
                                            type: fieldType);
            }

            return new DeconstructionVariablePendingInference(syntax, field, receiver);
        }
    }
}
