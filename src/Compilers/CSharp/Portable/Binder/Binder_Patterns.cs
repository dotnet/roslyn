// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Binder
    {
        private BoundExpression BindIsPatternExpression(IsPatternExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression expression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            bool hasErrors = IsOperandErrors(node, ref expression, diagnostics);
            TypeSymbol expressionType = expression.Type;
            if ((object)expressionType == null || expressionType.SpecialType == SpecialType.System_Void)
            {
                expressionType = CreateErrorType();
                if (!hasErrors)
                {
                    // value expected
                    diagnostics.Add(ErrorCode.ERR_BadPatternExpression, node.Expression.Location, expression.Display);
                    hasErrors = true;
                }
            }

            BoundPattern pattern = BindPattern(node.Pattern, expressionType, hasErrors, diagnostics);
            if (!hasErrors && pattern is BoundDeclarationPattern p && !p.IsVar && expression.ConstantValue == ConstantValue.Null)
            {
                diagnostics.Add(ErrorCode.WRN_IsAlwaysFalse, node.Location, p.DeclaredType.Type);
            }

            return new BoundIsPatternExpression(
                node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node), hasErrors);
        }

        private BoundExpression BindSwitchExpression(SwitchExpressionSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Binder switchBinder = this.GetBinder(node);
            return switchBinder.BindSwitchExpressionCore(node, switchBinder, diagnostics);
        }

        internal virtual BoundExpression BindSwitchExpressionCore(SwitchExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            return this.Next.BindSwitchExpressionCore(node, originalBinder, diagnostics);
        }

        internal BoundPattern BindPattern(
            PatternSyntax node,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DiscardPattern:
                    return BindDiscardPattern((DiscardPatternSyntax)node, operandType, hasErrors, diagnostics);

                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern((DeclarationPatternSyntax)node, operandType, hasErrors, diagnostics);

                case SyntaxKind.ConstantPattern:
                    return BindConstantPattern((ConstantPatternSyntax)node, operandType, hasErrors, diagnostics);

                case SyntaxKind.DeconstructionPattern:
                    return BindDeconstructionPattern((DeconstructionPatternSyntax)node, operandType, hasErrors, diagnostics);

                case SyntaxKind.PropertyPattern:
                    return BindPropertyPattern((PropertyPatternSyntax)node, operandType, hasErrors, diagnostics);

                case SyntaxKind.VarPattern:
                    return BindVarPattern((VarPatternSyntax)node, operandType, hasErrors, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundPattern BindDiscardPattern(DiscardPatternSyntax node, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            // PROTOTYPE(patterns2): give an error if there is a bindable `_` in scope.
            return new BoundDiscardPattern(node);
        }

        private BoundConstantPattern BindConstantPattern(
            ConstantPatternSyntax node,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            SyntaxNode innerExpression = node.Expression.SkipParens();
            if (innerExpression.Kind() == SyntaxKind.DefaultLiteralExpression)
            {
                diagnostics.Add(ErrorCode.ERR_DefaultPattern, innerExpression.Location);
                hasErrors = true;
            }

            return BindConstantPattern(node, operandType, node.Expression, hasErrors, diagnostics, out bool wasExpression);
        }

        internal BoundConstantPattern BindConstantPattern(
            CSharpSyntaxNode node,
            TypeSymbol operandType,
            ExpressionSyntax patternExpression,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression)
        {
            BoundExpression expression = BindValue(patternExpression, diagnostics, BindValueKind.RValue);
            ConstantValue constantValueOpt = null;
            BoundExpression convertedExpression = ConvertPatternExpression(operandType, patternExpression, expression, ref constantValueOpt, diagnostics);
            wasExpression = expression.Type?.IsErrorType() != true;
            if (!convertedExpression.HasErrors && constantValueOpt == null)
            {
                diagnostics.Add(ErrorCode.ERR_ConstantExpected, patternExpression.Location);
                hasErrors = true;
            }

            return new BoundConstantPattern(node, convertedExpression, constantValueOpt, hasErrors);
        }

        internal BoundExpression ConvertPatternExpression(TypeSymbol inputType, CSharpSyntaxNode node, BoundExpression expression, ref ConstantValue constantValue, DiagnosticBag diagnostics)
        {
            // NOTE: This will allow user-defined conversions, even though they're not allowed here.  This is acceptable
            // because the result of a user-defined conversion does not have a ConstantValue and we'll report a diagnostic
            // to that effect later.
            BoundExpression convertedExpression = GenerateConversionForAssignment(inputType, expression, diagnostics);

            if (convertedExpression.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)convertedExpression;
                BoundExpression operand = conversion.Operand;
                if (inputType.IsNullableType() && (convertedExpression.ConstantValue == null || !convertedExpression.ConstantValue.IsNull))
                {
                    // Null is a special case here because we want to compare null to the Nullable<T> itself, not to the underlying type.
                    var discardedDiagnostics = DiagnosticBag.GetInstance(); // We are not intested in the diagnostic that get created here
                    convertedExpression = CreateConversion(operand, inputType.GetNullableUnderlyingType(), discardedDiagnostics);
                    discardedDiagnostics.Free();
                }
                else if ((conversion.ConversionKind == ConversionKind.Boxing || conversion.ConversionKind == ConversionKind.ImplicitReference)
                    && operand.ConstantValue != null && convertedExpression.ConstantValue == null)
                {
                    // A boxed constant (or string converted to object) is a special case because we prefer
                    // to compare to the pre-converted value by casting the input value to the type of the constant
                    // (that is, unboxing or downcasting it) and then testing the resulting value using primitives.
                    // That is much more efficient than calling object.Equals(x, y), and we can share the downcasted
                    // input value among many constant tests.
                    convertedExpression = operand;
                }
                else if (conversion.ConversionKind == ConversionKind.NoConversion && convertedExpression.Type?.IsErrorType() == true)
                {
                    convertedExpression = operand;
                }
            }

            constantValue = convertedExpression.ConstantValue;
            return convertedExpression;
        }

        /// <summary>
        /// Check that the pattern type is valid for the operand. Return true if an error was reported.
        /// </summary>
        private bool CheckValidPatternType(
            CSharpSyntaxNode typeSyntax,
            TypeSymbol operandType,
            TypeSymbol patternType,
            bool patternTypeWasInSource,
            bool isVar,
            DiagnosticBag diagnostics)
        {
            Debug.Assert((object)operandType != null);
            Debug.Assert((object)patternType != null);

            if (operandType.IsErrorType() || patternType.IsErrorType())
            {
                return false;
            }
            else if (patternType.IsNullableType() && !isVar && patternTypeWasInSource)
            {
                // It is an error to use pattern-matching with a nullable type, because you'll never get null. Use the underlying type.
                Error(diagnostics, ErrorCode.ERR_PatternNullableType, typeSyntax, patternType, patternType.GetNullableUnderlyingType());
                return true;
            }
            else if (patternType.IsStatic)
            {
                Error(diagnostics, ErrorCode.ERR_VarDeclIsStaticClass, typeSyntax, patternType);
                return true;
            }
            else if (!isVar)
            {
                if (patternType.IsDynamic())
                {
                    Error(diagnostics, ErrorCode.ERR_PatternDynamicType, typeSyntax);
                    return true;
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                bool? matchPossible = ExpressionOfTypeMatchesPatternType(Conversions, operandType, patternType, ref useSiteDiagnostics, out Conversion conversion, operandConstantValue: null, operandCouldBeNull: true);
                diagnostics.Add(typeSyntax, useSiteDiagnostics);
                if (matchPossible != false)
                {
                    if (!conversion.Exists && (operandType.ContainsTypeParameter() || patternType.ContainsTypeParameter()))
                    {
                        // permit pattern-matching when one of the types is an open type in C# 7.1.
                        LanguageVersion requiredVersion = MessageID.IDS_FeatureGenericPatternMatching.RequiredVersion();
                        if (requiredVersion > Compilation.LanguageVersion)
                        {
                            Error(diagnostics, ErrorCode.ERR_PatternWrongGenericTypeInVersion, typeSyntax,
                                operandType, patternType,
                                Compilation.LanguageVersion.ToDisplayString(),
                                new CSharpRequiredLanguageVersion(requiredVersion));
                            return true;
                        }
                    }
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_PatternWrongType, typeSyntax, operandType, patternType);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Does an expression of type <paramref name="expressionType"/> "match" a pattern that looks for
        /// type <paramref name="patternType"/>?
        /// 'true' if the matched type catches all of them, 'false' if it catches none of them, and
        /// 'null' if it might catch some of them.
        /// </summary>
        internal static bool? ExpressionOfTypeMatchesPatternType(
            Conversions conversions,
            TypeSymbol expressionType,
            TypeSymbol patternType,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            out Conversion conversion,
            ConstantValue operandConstantValue = null,
            bool operandCouldBeNull = false)
        {
            Debug.Assert((object)expressionType != null);
            if (expressionType.IsDynamic())
            {
                // if operand is the dynamic type, we do the same thing as though it were object
                expressionType = conversions.CorLibrary.GetSpecialType(SpecialType.System_Object);
            }

            conversion = conversions.ClassifyConversionFromType(expressionType, patternType, ref useSiteDiagnostics);
            ConstantValue result = Binder.GetIsOperatorConstantResult(expressionType, patternType, conversion.Kind, operandConstantValue, operandCouldBeNull);
            return
                (result == null) ? (bool?)null :
                (result == ConstantValue.True) ? true :
                (result == ConstantValue.False) ? false :
                throw ExceptionUtilities.UnexpectedValue(result);
        }

        private BoundPattern BindDeclarationPattern(
            DeclarationPatternSyntax node,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            BoundTypeExpression boundDeclType = BindPatternType(typeSyntax, operandType, ref hasErrors, out bool isVar, diagnostics);
            if (typeSyntax.IsVar && !isVar)
            {
                // PROTOTYPE(patterns2): For compatibility, we temporarily parse the var pattern with a simple designator as a declaration pattern.
                // So we implement the semantics of the var pattern here, forbidding "var" to bind to a user-declared type.
                if (!hasErrors)
                {
                    diagnostics.Add(ErrorCode.ERR_VarMayNotBindToType, typeSyntax.Location, (boundDeclType.AliasOpt ?? (Symbol)boundDeclType.Type).ToDisplayString());
                }

                boundDeclType = new BoundTypeExpression(typeSyntax, null, inferredType: true, type: operandType, hasErrors: true);
            }

            TypeSymbol declType = boundDeclType.Type;
            BindPatternDesignation(node, node.Designation, declType, typeSyntax, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
            // PROTOTYPE(patterns2): We could bind the "var" declaration pattern as a var pattern in preparation for changing the parser to parse it as a var pattern.
            // PROTOTYPE(patterns2): Eventually we will want to remove "isVar" from the declaration pattern.
            return new BoundDeclarationPattern(node, variableSymbol, variableAccess, boundDeclType, isVar, hasErrors);
        }

        private BoundTypeExpression BindPatternType(
            TypeSyntax typeSyntax,
            TypeSymbol operandType,
            ref bool hasErrors,
            out bool isVar,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(operandType != (object)null);

            AliasSymbol aliasOpt;
            TypeSymbol declType = BindType(typeSyntax, diagnostics, out isVar, out aliasOpt);
            if (isVar)
            {
                declType = operandType;
            }

            if (declType == (object)null)
            {
                Debug.Assert(hasErrors);
                declType = this.CreateErrorType("var");
            }

            BoundTypeExpression boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: isVar, type: declType);
            if (IsOperatorErrors(typeSyntax, operandType, boundDeclType, diagnostics))
            {
                hasErrors = true;
            }
            else
            {
                hasErrors |= CheckValidPatternType(typeSyntax, operandType, declType,
                                                   isVar: isVar, patternTypeWasInSource: true, diagnostics: diagnostics);
            }

            return boundDeclType;
        }

        private void BindPatternDesignation(
            PatternSyntax node,
            VariableDesignationSyntax designation,
            TypeSymbol declType,
            TypeSyntax typeSyntax,
            DiagnosticBag diagnostics,
            ref bool hasErrors,
            out Symbol variableSymbol,
            out BoundExpression variableAccess)
        {
            switch (designation)
            {
                case SingleVariableDesignationSyntax singleVariableDesignation:
                    SyntaxToken identifier = singleVariableDesignation.Identifier;
                    SourceLocalSymbol localSymbol = this.LookupLocal(identifier);

                    if (localSymbol != (object)null)
                    {
                        if ((InConstructorInitializer || InFieldInitializer) && ContainingMemberOrLambda.ContainingSymbol.Kind == SymbolKind.NamedType)
                        {
                            Error(diagnostics, ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, node);
                        }

                        localSymbol.SetType(declType);

                        // Check for variable declaration errors.
                        hasErrors |= localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

                        if (!hasErrors)
                        {
                            hasErrors = CheckRestrictedTypeInAsync(this.ContainingMemberOrLambda, declType, diagnostics, typeSyntax ?? (SyntaxNode)designation);
                        }

                        variableSymbol = localSymbol;
                        variableAccess = new BoundLocal(node, localSymbol, null, declType);
                        return;
                    }
                    else
                    {
                        // We should have the right binder in the chain for a script or interactive, so we use the field for the pattern.
                        Debug.Assert(node.SyntaxTree.Options.Kind != SourceCodeKind.Regular);
                        GlobalExpressionVariable expressionVariableField = LookupDeclaredField(singleVariableDesignation);
                        DiagnosticBag tempDiagnostics = DiagnosticBag.GetInstance();
                        expressionVariableField.SetType(declType, tempDiagnostics);
                        tempDiagnostics.Free();
                        BoundExpression receiver = SynthesizeReceiver(node, expressionVariableField, diagnostics);

                        variableSymbol = expressionVariableField;
                        variableAccess = new BoundFieldAccess(node, receiver, expressionVariableField, null, hasErrors);
                        return;
                    }
                case DiscardDesignationSyntax _:
                case null:
                    variableSymbol = null;
                    variableAccess = null;
                    return;
                default:
                    throw ExceptionUtilities.UnexpectedValue(designation.Kind());
            }

        }

        TypeSymbol BindRecursivePatternType(TypeSyntax typeSyntax, TypeSymbol operandType, ref bool hasErrors, out BoundTypeExpression boundDeclType, DiagnosticBag diagnostics)
        {
            if (typeSyntax != null)
            {
                boundDeclType = BindPatternType(typeSyntax, operandType, ref hasErrors, out bool isVar, diagnostics);
                if (isVar)
                {
                    // The type `var` is not permitted in recursive patterns. If you want the type inferred, just omit it.
                    if (!hasErrors)
                    {
                        diagnostics.Add(ErrorCode.ERR_InferredRecursivePatternType, typeSyntax.Location);
                        hasErrors = true;
                    }

                    boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt: null, inferredType: true, type: operandType.StrippedType(), hasErrors: hasErrors);
                }

                return boundDeclType.Type;
            }
            else
            {
                boundDeclType = null;
                return operandType.StrippedType(); // remove the nullable part of the input's type
            }
        }

        private BoundPattern BindDeconstructionPattern(DeconstructionPatternSyntax node, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            TypeSymbol declType = BindRecursivePatternType(typeSyntax, operandType, ref hasErrors, out BoundTypeExpression boundDeclType, diagnostics);

            var patterns = ArrayBuilder<BoundPattern>.GetInstance(node.SubPatterns.Count);
            MethodSymbol deconstructMethod = null;
            if (declType.IsTupleType)
            {
                // It is a tuple type. Work according to its elements
                ImmutableArray<TypeSymbol> elementTypes = declType.TupleElementTypes;
                if (elementTypes.Length != node.SubPatterns.Count && !hasErrors)
                {
                    var location = new SourceLocation(node.SyntaxTree, new Text.TextSpan(node.OpenParenToken.SpanStart, node.CloseParenToken.Span.End - node.OpenParenToken.SpanStart));
                    diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, location, declType.TupleElementTypes, elementTypes.Length, node.SubPatterns.Count);
                    hasErrors = true;
                }
                for (int i = 0; i < node.SubPatterns.Count; i++)
                {
                    bool isError = i >= elementTypes.Length;
                    TypeSymbol elementType = isError ? CreateErrorType() : elementTypes[i];
                    // PROTOTYPE(patterns2): Check that node.SubPatterns[i].NameColon?.Name corresponds to tuple element i of declType.
                    BoundPattern boundSubpattern = BindPattern(node.SubPatterns[i].Pattern, elementType, isError, diagnostics);
                    patterns.Add(boundSubpattern);
                }
            }
            else
            {
                // It is not a tuple type. Seek an appropriate Deconstruct method.
                var inputPlaceholder = new BoundImplicitReceiver(node, declType); // A fake receiver expression to permit us to reuse binding logic
                BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                    node.SubPatterns.Count, inputPlaceholder, node, diagnostics, out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders, requireTwoOrMoreElements: false);
                deconstructMethod = deconstruct.ExpressionSymbol as MethodSymbol;
                // PROTOTYPE(patterns2): Set and check the deconstructMethod

                for (int i = 0; i < node.SubPatterns.Count; i++)
                {
                    bool isError = outPlaceholders.IsDefaultOrEmpty || i >= outPlaceholders.Length;
                    TypeSymbol elementType = isError ? CreateErrorType() : outPlaceholders[i].Type;
                    // PROTOTYPE(patterns2): Check that node.SubPatterns[i].NameColon?.Name corresponds to parameter i of the method. Or,
                    // better yet, include those names in the AnalyzedArguments used in MakeDeconstructInvocationExpression so they are
                    // used to disambiguate.
                    BoundPattern boundSubpattern = BindPattern(node.SubPatterns[i].Pattern, elementType, isError, diagnostics);
                    patterns.Add(boundSubpattern);
                }

                // PROTOTYPE(patterns2): If no Deconstruct method is found, try casting to `ITuple`.
            }

            ImmutableArray<(Symbol property, BoundPattern pattern)> propertiesOpt = default;
            if (node.PropertySubpattern != null)
            {
                propertiesOpt = BindPropertySubpattern(node.PropertySubpattern, declType, diagnostics, ref hasErrors);
            }

            BindPatternDesignation(node, node.Designation, declType, typeSyntax, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
            return new BoundRecursivePattern(
                syntax: node, declaredType: boundDeclType, inputType: declType, deconstructMethodOpt: deconstructMethod,
                deconstruction: patterns.ToImmutableAndFree(), propertiesOpt: propertiesOpt, variable: variableSymbol, variableAccess: variableAccess, hasErrors: hasErrors);
        }

        private BoundPattern BindVarPattern(VarPatternSyntax node, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSymbol declType = operandType;
            Symbol foundType = BindVarType(node.VarKeyword, diagnostics, out bool isVar, null);
            if (!isVar)
            {
                // Give an error if there is a bindable type "var" in scope
                diagnostics.Add(ErrorCode.ERR_VarMayNotBindToType, node.VarKeyword.GetLocation(), foundType.ToDisplayString());
                hasErrors = true;
            }

            return BindVarDesignation(node, node.Designation, operandType, hasErrors, diagnostics);
        }

        private BoundPattern BindVarDesignation(VarPatternSyntax node, VariableDesignationSyntax designation, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            switch (designation.Kind())
            {
                case SyntaxKind.DiscardDesignation:
                    {
                        //return new BoundDiscardPattern(designation);
                        // PROTOTYPE(patterns2): this should bind as a discard pattern, but for now we'll bind it as a declaration
                        // pattern for compatibility with the later phases of the compiler that do not yet handle the discard pattern.
                        var boundOperandType = new BoundTypeExpression(node, null, operandType); // fake a type expression for the variable's type
                        return new BoundDeclarationPattern(designation, null, null, boundOperandType, isVar: true, hasErrors: hasErrors);
                    }
                case SyntaxKind.SingleVariableDesignation:
                    {
                        BindPatternDesignation(node, designation, operandType, null, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
                        var boundOperandType = new BoundTypeExpression(node, null, operandType); // fake a type expression for the variable's type
                        return new BoundDeclarationPattern(designation, variableSymbol, variableAccess, boundOperandType, isVar: true, hasErrors: hasErrors);
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        var tupleDesignation = (ParenthesizedVariableDesignationSyntax)designation;
                        var patterns = ArrayBuilder<BoundPattern>.GetInstance(tupleDesignation.Variables.Count);
                        MethodSymbol deconstructMethod = null;
                        if (operandType.IsTupleType)
                        {
                            // It is a tuple type. Work according to its elements
                            ImmutableArray<TypeSymbol> elementTypes = operandType.TupleElementTypes;
                            if (elementTypes.Length != tupleDesignation.Variables.Count && !hasErrors)
                            {
                                var location = new SourceLocation(node.SyntaxTree, new Text.TextSpan(tupleDesignation.OpenParenToken.SpanStart, tupleDesignation.CloseParenToken.Span.End - tupleDesignation.OpenParenToken.SpanStart));
                                diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, location, operandType.TupleElementTypes, elementTypes.Length, tupleDesignation.Variables.Count);
                                hasErrors = true;
                            }
                            for (int i = 0; i < tupleDesignation.Variables.Count; i++)
                            {
                                bool isError = i >= elementTypes.Length;
                                TypeSymbol elementType = isError ? CreateErrorType() : elementTypes[i];
                                BoundPattern boundSubpattern = BindVarDesignation(node, tupleDesignation.Variables[i], elementType, isError, diagnostics);
                                patterns.Add(boundSubpattern);
                            }
                        }
                        else
                        {
                            // It is not a tuple type. Seek an appropriate Deconstruct method.
                            var inputPlaceholder = new BoundImplicitReceiver(node, operandType); // A fake receiver expression to permit us to reuse binding logic
                            BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                                tupleDesignation.Variables.Count, inputPlaceholder, node, diagnostics, out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders, requireTwoOrMoreElements: false);
                            deconstructMethod = deconstruct.ExpressionSymbol as MethodSymbol;
                            // PROTOTYPE(patterns2): Set and check the deconstructMethod

                            for (int i = 0; i < tupleDesignation.Variables.Count; i++)
                            {
                                bool isError = outPlaceholders.IsDefaultOrEmpty || i >= outPlaceholders.Length;
                                TypeSymbol elementType = isError ? CreateErrorType() : outPlaceholders[i].Type;
                                BoundPattern boundSubpattern = BindVarDesignation(node, tupleDesignation.Variables[i], elementType, isError, diagnostics);
                                patterns.Add(boundSubpattern);
                            }

                            // PROTOTYPE(patterns2): If no Deconstruct method is found, try casting to `ITuple`.
                        }

                        return new BoundRecursivePattern(
                            syntax: node, declaredType: null, inputType: operandType, deconstructMethodOpt: deconstructMethod,
                            deconstruction: patterns.ToImmutableAndFree(), propertiesOpt: default, variable: null, variableAccess: null, hasErrors: hasErrors);
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(designation.Kind());
                    }
            }
        }

        private BoundPattern BindPropertyPattern(PropertyPatternSyntax node, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            TypeSymbol declType = BindRecursivePatternType(typeSyntax, operandType, ref hasErrors, out BoundTypeExpression boundDeclType, diagnostics);
            ImmutableArray<(Symbol property, BoundPattern pattern)> propertiesOpt = BindPropertySubpattern(node.PropertySubpattern, declType, diagnostics, ref hasErrors);
            BindPatternDesignation(node, node.Designation, declType, typeSyntax, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
            return new BoundRecursivePattern(
                syntax: node, declaredType: boundDeclType, inputType: declType, deconstructMethodOpt: null,
                deconstruction: default, propertiesOpt: propertiesOpt, variable: variableSymbol, variableAccess: variableAccess, hasErrors: hasErrors);
        }

        ImmutableArray<(Symbol property, BoundPattern pattern)> BindPropertySubpattern(
            PropertySubpatternSyntax node,
            TypeSymbol inputType,
            DiagnosticBag diagnostics,
            ref bool hasErrors)
        {
            var builder = ArrayBuilder<(Symbol property, BoundPattern pattern)>.GetInstance(node.SubPatterns.Count);
            foreach (SubpatternElementSyntax p in node.SubPatterns)
            {
                IdentifierNameSyntax name = p.NameColon?.Name;
                PatternSyntax pattern = p.Pattern;
                Symbol member = null;
                TypeSymbol memberType;
                if (name == null)
                {
                    if (!hasErrors)
                    {
                        diagnostics.Add(ErrorCode.ERR_PropertyPatternNameMissing, pattern.Location, pattern);
                    }

                    memberType = CreateErrorType();
                    hasErrors = true;
                }
                else
                {
                    member = LookupMemberForPropertyPattern(inputType, name, out memberType, ref hasErrors, diagnostics);
                }

                BoundPattern boundPattern = BindPattern(pattern, memberType, hasErrors, diagnostics);
                builder.Add((member, boundPattern));
            }

            return builder.ToImmutableAndFree();
        }

        private Symbol LookupMemberForPropertyPattern(TypeSymbol inputType, IdentifierNameSyntax name, out TypeSymbol memberType, ref bool hasErrors, DiagnosticBag diagnostics)
        {
            Symbol symbol = BindPropertyPatternMember(inputType, name, ref hasErrors, diagnostics);

            if (inputType.IsErrorType() || hasErrors)
            {
                memberType = CreateErrorType();
                return null;
            }

            memberType = symbol.GetTypeOrReturnType();
            return symbol;
        }

        private Symbol BindPropertyPatternMember(
            TypeSymbol inputType,
            IdentifierNameSyntax memberName,
            ref bool hasErrors,
            DiagnosticBag diagnostics)
        {
            // TODO: consider refactoring out common code with BindObjectInitializerMember
            BoundImplicitReceiver implicitReceiver = new BoundImplicitReceiver(memberName, inputType);
            string name = memberName.Identifier.ValueText;

            BoundExpression boundMember = BindInstanceMemberAccess(
                node: memberName,
                right: memberName,
                boundLeft: implicitReceiver,
                rightName: name,
                rightArity: 0,
                typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>),
                typeArguments: default(ImmutableArray<TypeSymbol>),
                invoked: false,
                diagnostics: diagnostics);

            if (boundMember.Kind == BoundKind.PropertyGroup)
            {
                boundMember = BindIndexedPropertyAccess(
                    (BoundPropertyGroup)boundMember, mustHaveAllOptionalParameters: true, diagnostics: diagnostics);
            }

            LookupResultKind resultKind = boundMember.ResultKind;
            hasErrors |= boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;

            switch (boundMember.Kind)
            {
                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                    break;

                case BoundKind.IndexerAccess:
                case BoundKind.DynamicIndexerAccess:
                case BoundKind.EventAccess:
                    // PROTOTYPE(patterns2): we need to decide what kinds of members can be used in a property pattern.
                    // For now we support fields and readable non-indexed properties.
                default:
                    if (!hasErrors)
                    {
                        switch (boundMember.ResultKind)
                        {
                            case LookupResultKind.Empty:
                                Error(diagnostics, ErrorCode.ERR_NoSuchMember, memberName, implicitReceiver.Type, name);
                                break;

                            case LookupResultKind.Inaccessible:
                                boundMember = CheckValue(boundMember, BindValueKind.RValue, diagnostics);
                                Debug.Assert(boundMember.HasAnyErrors);
                                break;

                            default:
                                Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, memberName, name);
                                hasErrors = true;
                                break;
                        }
                    }
                    return null;
            }

            if (hasErrors || !CheckValueKind(memberName.Parent, boundMember, BindValueKind.RValue, false, diagnostics))
            {
                return null;
            }

            return boundMember.ExpressionSymbol;
        }
    }
}
