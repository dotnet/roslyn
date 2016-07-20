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
        private BoundDeconstructionAssignmentOperator BindDeconstructionAssignment(CSharpSyntaxNode node, ExpressionSyntax right, ArrayBuilder<DeconstructionVariable> checkedVariables, DiagnosticBag diagnostics, bool isDeclaration, BoundDeconstructValuePlaceholder rhsPlaceholder = null)
        {
            TypeSymbol voidType = GetSpecialType(SpecialType.System_Void, diagnostics, node);

            // receiver for first Deconstruct step
            var boundRHS = rhsPlaceholder ?? BindValue(right, diagnostics, BindValueKind.RValue);

            if ((object)boundRHS.Type == null)
            {
                if (boundRHS.Kind == BoundKind.TupleLiteral)
                {
                    // Let's fix the literal up by figuring out its type
                    // For declarations, that means merging type information from the LHS and RHS
                    // For assignments, only the LHS side matters since it is necessarily typed
                    TypeSymbol lhsAsTuple = MakeMergedTupleType(checkedVariables, (BoundTupleLiteral)boundRHS, node, Compilation, diagnostics);
                    if ((object)lhsAsTuple != null)
                    {
                        boundRHS = GenerateConversionForAssignment(lhsAsTuple, boundRHS, diagnostics);
                    }
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_DeconstructRequiresExpression, right);
                }

                if ((object)boundRHS.Type == null)
                {
                    // we could still not infer a type for the RHS
                    FailRemainingInferences(checkedVariables, diagnostics);

                    return new BoundDeconstructionAssignmentOperator(
                                node, isDeclaration, FlattenDeconstructVariables(checkedVariables), boundRHS,
                                ImmutableArray<BoundDeconstructionDeconstructStep>.Empty, ImmutableArray<BoundDeconstructionAssignmentStep>.Empty,
                                voidType, hasErrors: true);
                }
            }
            var deconstructionSteps = ArrayBuilder<BoundDeconstructionDeconstructStep>.GetInstance(1);
            var assignmentSteps = ArrayBuilder<BoundDeconstructionAssignmentStep>.GetInstance(1);
            bool hasErrors = !DeconstructIntoSteps(new BoundDeconstructValuePlaceholder(boundRHS.Syntax, boundRHS.Type), node, diagnostics, checkedVariables, deconstructionSteps, assignmentSteps);

            var deconstructions = deconstructionSteps.ToImmutableAndFree();
            var assignments = assignmentSteps.ToImmutableAndFree();

            FailRemainingInferences(checkedVariables, diagnostics);
            return new BoundDeconstructionAssignmentOperator(node, isDeclaration, FlattenDeconstructVariables(checkedVariables), boundRHS, deconstructions, assignments, voidType, hasErrors: hasErrors);
        }

        /// <summary>
        /// Whether the target is a tuple or a type that requires Deconstruction, this will generate and stack appropriate deconstruction and assignment steps.
        /// Note that the variables may either be plain or nested variables.
        /// The variables may be updated with inferred types if they didn't have types initially.
        /// Returns false if there was an error.
        /// </summary>
        private bool DeconstructIntoSteps(
                        BoundDeconstructValuePlaceholder targetPlaceholder,
                        CSharpSyntaxNode syntax,
                        DiagnosticBag diagnostics,
                        ArrayBuilder<DeconstructionVariable> variables,
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
        private static BoundDeconstructionDeconstructStep MakeTupleDeconstructStep(
                                                        BoundDeconstructValuePlaceholder targetPlaceholder,
                                                        CSharpSyntaxNode syntax,
                                                        DiagnosticBag diagnostics,
                                                        ArrayBuilder<DeconstructionVariable> variables,
                                                        ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                                                        ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps)
        {
            Debug.Assert(targetPlaceholder.Type.IsTupleType);

            var tupleTypes = targetPlaceholder.Type.TupleElementTypes;
            SetInferredTypes(variables, tupleTypes);

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
                                                            ArrayBuilder<DeconstructionVariable> variables,
                                                            ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                                                            ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps)
        {
            // symbol and parameters for Deconstruct
            ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders;
            var deconstructInvocation = MakeDeconstructInvocationExpression(variables.Count, targetPlaceholder, syntax, diagnostics, out outPlaceholders);
            if (deconstructInvocation.HasAnyErrors)
            {
                return null;
            }

            SetInferredTypes(variables, outPlaceholders.SelectAsArray(p => p.Type));

            return new BoundDeconstructionDeconstructStep(syntax, deconstructInvocation, targetPlaceholder, outPlaceholders);
        }

        /// <summary>
        /// Inform the variables about found types.
        /// </summary>
        private static void SetInferredTypes(ArrayBuilder<DeconstructionVariable> variables, ImmutableArray<TypeSymbol> foundTypes)
        {
            var matchCount = Math.Min(variables.Count, foundTypes.Length);
            for (int i = 0; i < matchCount; i++)
            {
                var variable = variables[i];
                if (!variable.HasNestedVariables && variable.Single.Kind == BoundKind.DeconstructionLocalPendingInference)
                {
                    BoundLocal local = ((DeconstructionLocalPendingInference)variable.Single).SetInferredType(foundTypes[i], success: true);
                    variables[i] = new DeconstructionVariable(local, local.Syntax);
                }
            }
        }

        /// <summary>
        /// Find any deconstruction locals that are still pending inference and fail their inference.
        /// </summary>
        private void FailRemainingInferences(ArrayBuilder<DeconstructionVariable> variables, DiagnosticBag diagnostics)
        {
            var count = variables.Count;
            for (int i = 0; i < count; i++)
            {
                var variable = variables[i];
                if (variable.HasNestedVariables)
                {
                    FailRemainingInferences(variable.NestedVariables, diagnostics);
                }
                else
                {
                    if (variable.Single.Kind == BoundKind.DeconstructionLocalPendingInference)
                    {
                        var local = ((DeconstructionLocalPendingInference)variable.Single).FailInference(this);
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
            public readonly CSharpSyntaxNode Syntax;

            public DeconstructionVariable(BoundExpression variable, CSharpSyntaxNode syntax)
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
        /// Takes the outputs from the previous deconstructionStep and depending on the structure of variables, will generate further deconstructions, or simply assignments.
        /// Returns true for success, but false if has errors.
        /// </summary>
        private bool DeconstructOrAssignOutputs(
                        BoundDeconstructionDeconstructStep deconstructionStep,
                        ArrayBuilder<DeconstructionVariable> variables,
                        CSharpSyntaxNode syntax,
                        DiagnosticBag diagnostics,
                        ArrayBuilder<BoundDeconstructionDeconstructStep> deconstructionSteps,
                        ArrayBuilder<BoundDeconstructionAssignmentStep> assignmentSteps)
        {
            bool hasErrors = false;

            int count = variables.Count;
            for (int i = 0; i < count; i++)
            {
                var variable = variables[i];
                var valuePlaceholder = deconstructionStep.OutputPlaceholders[i];

                if (variable.HasNestedVariables)
                {
                    var nested = variable.NestedVariables;
                    if (!DeconstructIntoSteps(valuePlaceholder, syntax, diagnostics, nested, deconstructionSteps, assignmentSteps))
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
                        else if ((object)mergedType == null)
                        {
                            // typeless-variable on the left and typeless-element on the right
                            Error(diagnostics, ErrorCode.ERR_DeconstructCouldNotInferMergedType, syntax, variable.Syntax, element.Syntax);
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

            return TupleTypeSymbol.Create(locationOpt: null, elementTypes: typesBuilder.ToImmutableAndFree(), elementLocations: default(ImmutableArray<Location>), elementNames: default(ImmutableArray<string>), compilation: compilation, diagnostics: diagnostics);
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
        /// Figures out how to assign from sourceType into receivingVariable and bundles the information (leaving holes for the actual source and receiver) into an AssignmentInfo.
        /// </summary>
        private BoundDeconstructionAssignmentStep MakeDeconstructionAssignmentStep(
                                                    BoundExpression receivingVariable, TypeSymbol sourceType, BoundDeconstructValuePlaceholder inputPlaceholder,
                                                    CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            var outputPlaceholder = new BoundDeconstructValuePlaceholder(receivingVariable.Syntax, receivingVariable.Type) { WasCompilerGenerated = true };

            // each assignment has a placeholder for a receiver and another for the source
            BoundAssignmentOperator op = BindAssignment(receivingVariable.Syntax, outputPlaceholder, inputPlaceholder, diagnostics);

            return new BoundDeconstructionAssignmentStep(node, op, inputPlaceholder, outputPlaceholder);
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
            DiagnosticBag bag = null;

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
                bag = DiagnosticBag.GetInstance();
                BoundExpression result = BindMethodGroupInvocation(
                                            receiverSyntax, receiverSyntax, methodName, (BoundMethodGroup)memberAccess, analyzedArguments, bag, queryClause: null,
                                            allowUnexpandedForm: true);

                result.WasCompilerGenerated = true;
                diagnostics.AddRange(bag);

                if (bag.HasAnyErrors())
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

                if (bag != null)
                {
                    bag.Free();
                }
            }

        }

        private BoundBadExpression MissingDeconstruct(BoundExpression receiver, CSharpSyntaxNode syntax, int numParameters, DiagnosticBag diagnostics, out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders, BoundNode childNode)
        {
            Error(diagnostics, ErrorCode.ERR_MissingDeconstruct, receiver.Syntax, receiver.Type, numParameters);
            outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

            return BadExpression(syntax, childNode);
        }

        internal BoundLocalDeconstructionDeclaration BindDeconstructionDeclaration(CSharpSyntaxNode node, VariableDeclarationSyntax declaration, DiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind() == SyntaxKind.LocalDeclarationStatement || node.Kind() == SyntaxKind.VariableDeclaration);
            Debug.Assert(declaration.IsDeconstructionDeclaration);

            var value = declaration.Deconstruction.Value;
            return new BoundLocalDeconstructionDeclaration(node, BindDeconstructionDeclaration(node, declaration, value, diagnostics));
        }

        internal BoundDeconstructionAssignmentOperator BindDeconstructionDeclaration(CSharpSyntaxNode node, VariableDeclarationSyntax declaration, ExpressionSyntax right, DiagnosticBag diagnostics, BoundDeconstructValuePlaceholder rightPlaceholder = null)
        {
            ArrayBuilder<DeconstructionVariable> locals = BindDeconstructionDeclarationLocals(declaration, declaration.Type, diagnostics);

            var result = BindDeconstructionAssignment(node, right, locals, diagnostics, isDeclaration: true, rhsPlaceholder: rightPlaceholder);
            FreeDeconstructionVariables(locals);

            return result;
        }

        /// <summary>
        /// Prepares locals corresponding to the variables of the declaration.
        /// The locals are kept in a tree which captures the nesting of variables.
        /// Each local is either a simple local (when its type is known) or a deconstruction local pending inference.
        /// The caller is responsible for releasing the nested ArrayBuilders.
        /// </summary>
        private ArrayBuilder<DeconstructionVariable> BindDeconstructionDeclarationLocals(VariableDeclarationSyntax node, TypeSyntax closestTypeSyntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(node.IsDeconstructionDeclaration);
            SeparatedSyntaxList<VariableDeclarationSyntax> variables = node.Deconstruction.Variables;

            // There are four cases for VariableDeclaration:
            // - type and declarators are set, but deconstruction is null. This could represent `int x`, which is a single variable.
            // - type is null, declarators are set, but deconstruction is null. This could represent `x`, which is a single variable.
            // - type is set to 'var', declarators are null, and deconstruction is set. This could represent `var (...)`
            // - type and declarators are null, but deconstruction is set. This could represent `(int x, ...)`

            var localsBuilder = ArrayBuilder<DeconstructionVariable>.GetInstance(variables.Count);
            foreach (var variable in variables)
            {
                TypeSyntax typeSyntax = variable.Type ?? closestTypeSyntax;

                DeconstructionVariable local;
                if (variable.IsDeconstructionDeclaration)
                {
                    local = new DeconstructionVariable(BindDeconstructionDeclarationLocals(variable, typeSyntax, diagnostics), variable.Deconstruction);
                }
                else
                {
                    local = new DeconstructionVariable(BindDeconstructionDeclarationLocal(variable, typeSyntax, diagnostics), variable);
                }

                localsBuilder.Add(local);
            }

            return localsBuilder;
        }

        /// <summary>
        /// Returns a BoundLocal when the type was explicit, otherwise returns a DeconstructionLocalPendingInference.
        /// </summary>
        private BoundExpression BindDeconstructionDeclarationLocal(VariableDeclarationSyntax node, TypeSyntax closestTypeSyntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(!node.IsDeconstructionDeclaration);
            Debug.Assert(node.Variables.Count == 1);

            var declarator = node.Variables[0];

            var localSymbol = LocateDeclaredVariableSymbol(declarator, closestTypeSyntax);

            // Check for variable declaration errors.
            // Use the binder that owns the scope for the local because this (the current) binder
            // might own nested scope.
            bool hasErrors = localSymbol.Binder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

            bool isVar;
            bool isConst = false;
            AliasSymbol alias;
            TypeSymbol declType = BindVariableType(node, diagnostics, closestTypeSyntax, ref isConst, out isVar, out alias);

            if (!isVar)
            {
                if (node.Type == null)
                {
                    // An explicit type can only be provided next to the variable
                    Error(diagnostics, ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, node);
                }

                return new BoundLocal(declarator, localSymbol, constantValueOpt: null, type: declType, hasErrors: node.Type == null);
            }

            return new DeconstructionLocalPendingInference(declarator, localSymbol);
        }
    }
}
