﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts deconstruction-assignment syntax (AssignmentExpressionSyntax nodes with the left
    /// being a tuple expression or declaration expression) into a BoundAssignmentOperator (or bad node).
    /// The BoundAssignmentOperator will have:
    /// - a BoundTupleLiteral as its Left,
    /// - a BoundConversion as its Right, holding:
    ///     - a tree of Conversion objects with Kind=Deconstruction, information about a Deconstruct method (optional) and
    ///         an array of nested Conversions (like a tuple conversion),
    ///     - a BoundExpression as its Operand.
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

            BoundExpression boundRight = rightPlaceholder ?? BindValue(right, diagnostics, BindValueKind.RValue);
            boundRight = FixTupleLiteral(locals.NestedVariables, boundRight, deconstruction, diagnostics);

            var assignment = BindDeconstructionAssignment(deconstruction, left, boundRight, locals.NestedVariables, diagnostics);
            DeconstructionVariable.FreeDeconstructionVariables(locals.NestedVariables);

            return assignment;
        }

        private BoundDeconstructionAssignmentOperator BindDeconstructionAssignment(
                                                        CSharpSyntaxNode node,
                                                        ExpressionSyntax left,
                                                        BoundExpression boundRHS,
                                                        ArrayBuilder<DeconstructionVariable> checkedVariables,
                                                        DiagnosticBag diagnostics)
        {
            if ((object)boundRHS.Type == null || boundRHS.Type.IsErrorType())
            {
                // we could still not infer a type for the RHS
                FailRemainingInferences(checkedVariables, diagnostics);
                var voidType = GetSpecialType(SpecialType.System_Void, diagnostics, node);

                var type = boundRHS.Type ?? voidType;
                return new BoundDeconstructionAssignmentOperator(
                            node,
                            DeconstructionVariablesAsTuple(node, checkedVariables, diagnostics, hasErrors: true),
                            new BoundConversion(boundRHS.Syntax, boundRHS, Conversion.Deconstruction, @checked: false, explicitCastInCode: false,
                                constantValueOpt: null, type: type, hasErrors: true),
                            voidType,
                            hasErrors: true);
            }

            Conversion conversion;
            bool hasErrors = !MakeDeconstructionConversion(
                                    boundRHS.Type,
                                    node,
                                    boundRHS.Syntax,
                                    diagnostics,
                                    checkedVariables,
                                    out conversion);

            FailRemainingInferences(checkedVariables, diagnostics);

            var lhsTuple = DeconstructionVariablesAsTuple(left, checkedVariables, diagnostics, hasErrors);
            TypeSymbol returnType = hasErrors ? CreateErrorType() : lhsTuple.Type;

            var boundConversion = new BoundConversion(
                boundRHS.Syntax,
                boundRHS,
                conversion,
                @checked: false,
                explicitCastInCode: false,
                constantValueOpt: null,
                type: returnType,
                hasErrors: hasErrors)
            { WasCompilerGenerated = true };

            return new BoundDeconstructionAssignmentOperator(node, lhsTuple, boundConversion, returnType);
        }

        /// <summary>When boundRHS is a tuple literal, fix it up by inferring its types.</summary>
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
        /// Recursively builds a Conversion object with Kind=Deconstruction including information about any necessary
        /// Deconstruct method and any element-wise conversion.
        ///
        /// Note that the variables may either be plain or nested variables.
        /// The variables may be updated with inferred types if they didn't have types initially.
        /// Returns false if there was an error.
        /// </summary>
        private bool MakeDeconstructionConversion(
                        TypeSymbol type,
                        SyntaxNode syntax,
                        SyntaxNode rightSyntax,
                        DiagnosticBag diagnostics,
                        ArrayBuilder<DeconstructionVariable> variables,
                        out Conversion conversion)
        {
            Debug.Assert(type != null);
            ImmutableArray<TypeSymbol> tupleOrDeconstructedTypes;
            conversion = Conversion.Deconstruction;

            // Figure out the deconstruct method (if one is required) and determine the types we get from the RHS at this level
            var deconstructInfo = default(DeconstructionInfo);
            if (type.IsTupleType)
            {
                // tuple literal such as `(1, 2)`, `(null, null)`, `(x.P, y.M())`
                tupleOrDeconstructedTypes = type.TupleElementTypes;
                SetInferredTypes(variables, tupleOrDeconstructedTypes, diagnostics);

                if (variables.Count != tupleOrDeconstructedTypes.Length)
                {
                    Error(diagnostics, ErrorCode.ERR_DeconstructWrongCardinality, syntax, tupleOrDeconstructedTypes.Length, variables.Count);
                    return false;
                }
            }
            else
            {
                ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders;
                var inputPlaceholder = new BoundDeconstructValuePlaceholder(syntax, type);
                var deconstructInvocation = MakeDeconstructInvocationExpression(variables.Count,
                    inputPlaceholder, rightSyntax, diagnostics, out outPlaceholders);

                if (deconstructInvocation.HasAnyErrors)
                {
                    return false;
                }

                deconstructInfo = new DeconstructionInfo(deconstructInvocation, inputPlaceholder, outPlaceholders);

                tupleOrDeconstructedTypes = outPlaceholders.SelectAsArray(p => p.Type);
                SetInferredTypes(variables, tupleOrDeconstructedTypes, diagnostics);
            }

            // Figure out whether those types will need conversions, including further deconstructions
            bool hasErrors = false;

            int count = variables.Count;
            var nestedConversions = ArrayBuilder<Conversion>.GetInstance(count);
            for (int i = 0; i < count; i++)
            {
                var variable = variables[i];

                Conversion nestedConversion;
                if (variable.HasNestedVariables)
                {
                    var elementSyntax = syntax.Kind() == SyntaxKind.TupleExpression ? ((TupleExpressionSyntax)syntax).Arguments[i] : syntax;

                    hasErrors |= !MakeDeconstructionConversion(tupleOrDeconstructedTypes[i], syntax, rightSyntax, diagnostics,
                        variable.NestedVariables, out nestedConversion);
                }
                else
                {
                    var single = variable.Single;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    nestedConversion = this.Conversions.ClassifyConversionFromType(tupleOrDeconstructedTypes[i], single.Type, ref useSiteDiagnostics);
                    diagnostics.Add(single.Syntax, useSiteDiagnostics);

                    if (!nestedConversion.IsImplicit)
                    {
                        hasErrors = true;
                        GenerateImplicitConversionError(diagnostics, Compilation, single.Syntax, nestedConversion, tupleOrDeconstructedTypes[i], single.Type);
                    }
                }
                nestedConversions.Add(nestedConversion);
            }

            conversion = new Conversion(ConversionKind.Deconstruction, deconstructInfo, nestedConversions.ToImmutableAndFree());

            return !hasErrors;
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
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        internal class DeconstructionVariable
        {
            internal readonly BoundExpression Single;
            internal readonly ArrayBuilder<DeconstructionVariable> NestedVariables;
            internal readonly CSharpSyntaxNode Syntax;

            internal DeconstructionVariable(BoundExpression variable, SyntaxNode syntax)
            {
                Single = variable;
                NestedVariables = null;
                Syntax = (CSharpSyntaxNode)syntax;
            }

            internal DeconstructionVariable(ArrayBuilder<DeconstructionVariable> variables, SyntaxNode syntax)
            {
                Single = null;
                NestedVariables = variables;
                Syntax = (CSharpSyntaxNode)syntax;
            }

            internal bool HasNestedVariables => NestedVariables != null;

            internal static void FreeDeconstructionVariables(ArrayBuilder<DeconstructionVariable> variables)
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
            private string GetDebuggerDisplay()
            {
                if (Single != null)
                {
                    return Single.GetDebuggerDisplay();
                }
                return $"Nested variables ({NestedVariables.Count})";
            }
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
            // DeconstructionVariablesAsTuple. It represents a smaller
            // tree of types used for figuring out natural types in tuple literal.
            // Therefore, we do not check constraints here as it would report errors
            // that are already reported later. DeconstructionVariablesAsTuple
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

        private BoundTupleLiteral DeconstructionVariablesAsTuple(CSharpSyntaxNode syntax, ArrayBuilder<DeconstructionVariable> variables, DiagnosticBag diagnostics, bool hasErrors)
        {
            int count = variables.Count;
            var valuesBuilder = ArrayBuilder<BoundExpression>.GetInstance(count);
            var typesBuilder = ArrayBuilder<TypeSymbol>.GetInstance(count);
            var locationsBuilder = ArrayBuilder<Location>.GetInstance(count);
            foreach (var variable in variables)
            {
                if (variable.HasNestedVariables)
                {
                    var nestedTuple = DeconstructionVariablesAsTuple(variable.Syntax, variable.NestedVariables, diagnostics, hasErrors);
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

            var type = TupleTypeSymbol.Create(syntax.Location,
                typesBuilder.ToImmutableAndFree(), locationsBuilder.ToImmutableAndFree(),
                elementNames: default(ImmutableArray<string>), compilation: this.Compilation,
                shouldCheckConstraints: !hasErrors, syntax: syntax, diagnostics: hasErrors ? null : diagnostics);

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
                                    int numCheckedVariables, BoundExpression receiver, SyntaxNode rightSyntax,
                                    DiagnosticBag diagnostics, out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders)
        {
            var receiverSyntax = (CSharpSyntaxNode)receiver.Syntax;
            if (numCheckedVariables < 2)
            {
                Error(diagnostics, ErrorCode.ERR_DeconstructTooFewElements, receiverSyntax);
                outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

                return BadExpression(receiverSyntax, receiver);
            }

            if (receiver.Type.IsDynamic())
            {
                Error(diagnostics, ErrorCode.ERR_CannotDeconstructDynamic, rightSyntax);
                outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

                return BadExpression(receiverSyntax, receiver);
            }

            var analyzedArguments = AnalyzedArguments.GetInstance();
            var outVars = ArrayBuilder<OutDeconstructVarPendingInference>.GetInstance(numCheckedVariables);

            try
            {
                for (int i = 0; i < numCheckedVariables; i++)
                {
                    var variable = new OutDeconstructVarPendingInference(receiverSyntax);
                    analyzedArguments.Arguments.Add(variable);
                    analyzedArguments.RefKinds.Add(RefKind.Out);
                    outVars.Add(variable);
                }

                const string methodName = "Deconstruct";
                var memberAccess = BindInstanceMemberAccess(
                                        rightSyntax, receiverSyntax, receiver, methodName, rightArity: 0,
                                        typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>), typeArguments: default(ImmutableArray<TypeSymbol>),
                                        invoked: true, diagnostics: diagnostics);

                memberAccess = CheckValue(memberAccess, BindValueKind.RValueOrMethodGroup, diagnostics);
                memberAccess.WasCompilerGenerated = true;

                if (memberAccess.Kind != BoundKind.MethodGroup)
                {
                    return MissingDeconstruct(receiver, rightSyntax, numCheckedVariables, diagnostics, out outPlaceholders, receiver);
                }

                // After the overload resolution completes, the last step is to coerce the arguments with inferred types.
                // That step returns placeholder (of correct type) instead of the outVar nodes that were passed in as arguments.
                // So the generated invocation expression will contain placeholders instead of those outVar nodes.
                // Those placeholders are also recorded in the outVar for easy access below, by the `SetInferredType` call on the outVar nodes.
                BoundExpression result = BindMethodGroupInvocation(
                                            rightSyntax, rightSyntax, methodName, (BoundMethodGroup)memberAccess, analyzedArguments, diagnostics, queryClause: null,
                                            allowUnexpandedForm: true);

                result.WasCompilerGenerated = true;

                if (result.HasErrors && !receiver.HasAnyErrors)
                {
                    return MissingDeconstruct(receiver, rightSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                // Verify all the parameters (except "this" for extension methods) are out parameters
                if (result.Kind != BoundKind.Call)
                {
                    return MissingDeconstruct(receiver, rightSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                var deconstructMethod = ((BoundCall)result).Method;
                var parameters = deconstructMethod.Parameters;
                for (int i = (deconstructMethod.IsExtensionMethod ? 1 : 0); i < parameters.Length; i++)
                {
                    if (parameters[i].RefKind != RefKind.Out)
                    {
                        return MissingDeconstruct(receiver, rightSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                    }
                }

                if (deconstructMethod.ReturnType.GetSpecialTypeSafe() != SpecialType.System_Void)
                {
                    return MissingDeconstruct(receiver, rightSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
                }

                if (outVars.Any(v => (object)v.Placeholder == null))
                {
                    return MissingDeconstruct(receiver, rightSyntax, numCheckedVariables, diagnostics, out outPlaceholders, result);
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

        private BoundBadExpression MissingDeconstruct(BoundExpression receiver, SyntaxNode rightSyntax, int numParameters, DiagnosticBag diagnostics,
                                    out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders, BoundExpression childNode)
        {
            Error(diagnostics, ErrorCode.ERR_MissingDeconstruct, rightSyntax, receiver.Type, numParameters);
            outPlaceholders = default(ImmutableArray<BoundDeconstructValuePlaceholder>);

            return BadExpression(rightSyntax, childNode);
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
                return new BoundFieldAccess(syntax,
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
