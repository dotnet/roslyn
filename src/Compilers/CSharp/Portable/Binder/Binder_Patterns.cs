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
            TypeSymbol inputType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DiscardPattern:
                    return BindDiscardPattern((DiscardPatternSyntax)node, inputType, hasErrors, diagnostics);

                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern((DeclarationPatternSyntax)node, inputType, hasErrors, diagnostics);

                case SyntaxKind.ConstantPattern:
                    return BindConstantPattern((ConstantPatternSyntax)node, inputType, hasErrors, diagnostics);

                case SyntaxKind.DeconstructionPattern:
                    return BindDeconstructionPattern((DeconstructionPatternSyntax)node, inputType, hasErrors, diagnostics);

                case SyntaxKind.PropertyPattern:
                    return BindPropertyPattern((PropertyPatternSyntax)node, inputType, hasErrors, diagnostics);

                case SyntaxKind.VarPattern:
                    return BindVarPattern((VarPatternSyntax)node, inputType, hasErrors, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundPattern BindDiscardPattern(DiscardPatternSyntax node, TypeSymbol inputType, bool hasErrors, DiagnosticBag diagnostics)
        {
            // give an error if there is a bindable `_` in scope.
            var lookupResult = LookupResult.GetInstance();
            var name = node.UnderscoreToken.ValueText;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupSymbolsInternal(
                lookupResult, name, arity: 0, basesBeingResolved: null,
                options: LookupOptions.AllMethodsOnArityZero, diagnose: false, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            if (lookupResult.IsMultiViable)
            {
                diagnostics.Add(ErrorCode.ERR_UnderscoreDeclaredAndDiscardPattern, node.Location, lookupResult.Symbols[0]);
            }

            lookupResult.Free();
            return new BoundDiscardPattern(node, inputType);
        }

        private BoundConstantPattern BindConstantPattern(
            ConstantPatternSyntax node,
            TypeSymbol inputType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            SyntaxNode innerExpression = node.Expression.SkipParens();
            if (innerExpression.Kind() == SyntaxKind.DefaultLiteralExpression)
            {
                diagnostics.Add(ErrorCode.ERR_DefaultPattern, innerExpression.Location);
                hasErrors = true;
            }

            return BindConstantPattern(node, innerExpression, inputType, node.Expression, hasErrors, diagnostics, out _);
        }

        internal BoundConstantPattern BindConstantPattern(
            CSharpSyntaxNode node,
            TypeSymbol inputType,
            ExpressionSyntax patternExpression,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression)
        {
            return BindConstantPattern(node, patternExpression.SkipParens(), inputType, patternExpression, hasErrors, diagnostics, out wasExpression);
        }

        internal BoundConstantPattern BindConstantPattern(
            CSharpSyntaxNode node,
            SyntaxNode innerExpression,
            TypeSymbol inputType,
            ExpressionSyntax patternExpression,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression)
        {
            if (innerExpression.Kind() == SyntaxKind.IdentifierName &&
                ((IdentifierNameSyntax)innerExpression).Identifier.Text == "_")
            {
                diagnostics.Add(ErrorCode.ERR_ConstantPatternNamedUnderscore, innerExpression.Location);
                hasErrors = true;
            }

            BoundExpression expression = BindValue(patternExpression, diagnostics, BindValueKind.RValue);
            ConstantValue constantValueOpt = null;
            BoundExpression convertedExpression = ConvertPatternExpression(inputType, patternExpression, expression, out constantValueOpt, diagnostics);
            wasExpression = expression.Type?.IsErrorType() != true;
            if (!convertedExpression.HasErrors && constantValueOpt == null)
            {
                diagnostics.Add(ErrorCode.ERR_ConstantExpected, patternExpression.Location);
                hasErrors = true;
            }

            if (convertedExpression.Type == null && constantValueOpt != ConstantValue.Null)
            {
                Debug.Assert(hasErrors);
                convertedExpression = new BoundConversion(
                    convertedExpression.Syntax, convertedExpression, Conversion.NoConversion, @checked: false,
                    explicitCastInCode: false, constantValueOpt: constantValueOpt, CreateErrorType(), hasErrors: true)
                    { WasCompilerGenerated = true };
            }

            return new BoundConstantPattern(node, convertedExpression, constantValueOpt ?? ConstantValue.Bad, inputType, hasErrors);
        }

        internal BoundExpression ConvertPatternExpression(TypeSymbol inputType, CSharpSyntaxNode node, BoundExpression expression, out ConstantValue constantValue, DiagnosticBag diagnostics)
        {
            BoundExpression convertedExpression;

            // If we are pattern-matching against an open type, we do not convert the constant to the type of the input.
            // This permits us to match a value of type `IComparable<T>` with a pattern of type `int`.
            bool inputContainsTypeParameter = inputType.ContainsTypeParameter();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (inputContainsTypeParameter)
            {
                convertedExpression = expression;
                if (expression.ConstantValue == ConstantValue.Null)
                {
                    if (inputType.IsNonNullableValueType())
                    {
                        // We do not permit matching null against a struct type.
                        diagnostics.Add(ErrorCode.ERR_ValueCantBeNull, expression.Syntax.Location, inputType);
                    }
                }
                else if (ExpressionOfTypeMatchesPatternType(Conversions, inputType, expression.Type, ref useSiteDiagnostics, out _, operandConstantValue: null) == false)
                {
                    diagnostics.Add(ErrorCode.ERR_PatternWrongType, expression.Syntax.Location, inputType, expression.Display);
                }

                diagnostics.Add(node, useSiteDiagnostics);
            }
            else
            {
                // This will allow user-defined conversions, even though they're not permitted here.  This is acceptable
                // because the result of a user-defined conversion does not have a ConstantValue. A constant pattern
                // requires a constant value so we'll report a diagnostic to that effect later.
                convertedExpression = GenerateConversionForAssignment(inputType, expression, diagnostics);

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
            }

            constantValue = convertedExpression.ConstantValue;
            return convertedExpression;
        }

        /// <summary>
        /// Check that the pattern type is valid for the operand. Return true if an error was reported.
        /// </summary>
        private bool CheckValidPatternType(
            CSharpSyntaxNode typeSyntax,
            TypeSymbol inputType,
            TypeSymbol patternType,
            bool patternTypeWasInSource,
            bool isVar,
            DiagnosticBag diagnostics)
        {
            Debug.Assert((object)inputType != null);
            Debug.Assert((object)patternType != null);

            if (inputType.IsErrorType() || patternType.IsErrorType())
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
                bool? matchPossible = ExpressionOfTypeMatchesPatternType(Conversions, inputType, patternType, ref useSiteDiagnostics, out Conversion conversion, operandConstantValue: null, operandCouldBeNull: true);
                diagnostics.Add(typeSyntax, useSiteDiagnostics);
                if (matchPossible != false)
                {
                    if (!conversion.Exists && (inputType.ContainsTypeParameter() || patternType.ContainsTypeParameter()))
                    {
                        // permit pattern-matching when one of the types is an open type in C# 7.1.
                        LanguageVersion requiredVersion = MessageID.IDS_FeatureGenericPatternMatching.RequiredVersion();
                        if (requiredVersion > Compilation.LanguageVersion)
                        {
                            Error(diagnostics, ErrorCode.ERR_PatternWrongGenericTypeInVersion, typeSyntax,
                                inputType, patternType,
                                Compilation.LanguageVersion.ToDisplayString(),
                                new CSharpRequiredLanguageVersion(requiredVersion));
                            return true;
                        }
                    }
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_PatternWrongType, typeSyntax, inputType, patternType);
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

            conversion = conversions.ClassifyBuiltInConversion(expressionType, patternType, ref useSiteDiagnostics);
            ConstantValue result = Binder.GetIsOperatorConstantResult(expressionType, patternType, conversion.Kind, operandConstantValue, operandCouldBeNull);
            return
                (result == null) ? (bool?)null :
                (result == ConstantValue.True) ? true :
                (result == ConstantValue.False) ? false :
                throw ExceptionUtilities.UnexpectedValue(result);
        }

        private BoundPattern BindDeclarationPattern(
            DeclarationPatternSyntax node,
            TypeSymbol inputType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            BoundTypeExpression boundDeclType = BindPatternType(typeSyntax, inputType, diagnostics, ref hasErrors, out bool isVar);
            if (typeSyntax.IsVar && !isVar)
            {
                // PROTOTYPE(patterns2): For compatibility, we temporarily parse the var pattern with a simple designator as a declaration pattern.
                // So we implement the semantics of the var pattern here, forbidding "var" to bind to a user-declared type.
                if (!hasErrors)
                {
                    diagnostics.Add(ErrorCode.ERR_VarMayNotBindToType, typeSyntax.Location, (boundDeclType.AliasOpt ?? (Symbol)boundDeclType.Type).ToDisplayString());
                }

                boundDeclType = new BoundTypeExpression(
                    syntax: typeSyntax, aliasOpt: null, inferredType: true, type: inputType, hasErrors: true);
            }

            TypeSymbol declType = boundDeclType.Type;
            BindPatternDesignation(node, node.Designation, declType, typeSyntax, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
            // PROTOTYPE(patterns2): We could bind the "var" declaration pattern as a var pattern in preparation for changing the parser to parse it as a var pattern.
            // PROTOTYPE(patterns2): Eventually we will want to remove "isVar" from the declaration pattern.
            return new BoundDeclarationPattern(node, variableSymbol, variableAccess, boundDeclType, isVar, inputType, hasErrors);
        }

        private BoundTypeExpression BindPatternType(
            TypeSyntax typeSyntax,
            TypeSymbol inputType,
            DiagnosticBag diagnostics,
            ref bool hasErrors,
            out bool isVar)
        {
            Debug.Assert(inputType != (object)null);

            AliasSymbol aliasOpt;
            TypeSymbol declType = BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar, out aliasOpt);
            if (isVar)
            {
                declType = inputType;
            }

            if (declType == (object)null)
            {
                Debug.Assert(hasErrors);
                declType = this.CreateErrorType("var");
            }

            BoundTypeExpression boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: isVar, type: declType);
            if (IsOperatorErrors(typeSyntax, inputType, boundDeclType, diagnostics))
            {
                hasErrors = true;
            }
            else
            {
                hasErrors |= CheckValidPatternType(typeSyntax, inputType, declType,
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
                    CheckFeatureAvailability(node, MessageID.IDS_FeatureExpressionVariablesInQueriesAndInitializers, diagnostics);
                }

                        localSymbol.SetType(declType);

                        // Check for variable declaration errors.
                        hasErrors |= localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

                        if (!hasErrors)
                        {
                            hasErrors = CheckRestrictedTypeInAsync(this.ContainingMemberOrLambda, declType, diagnostics, typeSyntax ?? (SyntaxNode)designation);
                        }

                        variableSymbol = localSymbol;
                        variableAccess = new BoundLocal(
                            syntax: node, localSymbol: localSymbol, constantValueOpt: null, type: declType);
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
                        variableAccess = new BoundFieldAccess(
                            syntax: node, receiver: receiver, fieldSymbol: expressionVariableField, constantValueOpt: null, hasErrors: hasErrors);
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

        TypeSymbol BindRecursivePatternType(TypeSyntax typeSyntax, TypeSymbol inputType, DiagnosticBag diagnostics, ref bool hasErrors, out BoundTypeExpression boundDeclType)
        {
            if (typeSyntax != null)
            {
                boundDeclType = BindPatternType(typeSyntax, inputType, diagnostics, ref hasErrors, out bool isVar);
                if (isVar)
                {
                    // The type `var` is not permitted in recursive patterns. If you want the type inferred, just omit it.
                    if (!hasErrors)
                    {
                        diagnostics.Add(ErrorCode.ERR_InferredRecursivePatternType, typeSyntax.Location);
                        hasErrors = true;
                    }

                    boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt: null, inferredType: true, type: inputType.StrippedType(), hasErrors: hasErrors);
                }

                return boundDeclType.Type;
            }
            else
            {
                boundDeclType = null;
                return inputType.StrippedType(); // remove the nullable part of the input's type
            }
        }

        private BoundPattern BindDeconstructionPattern(DeconstructionPatternSyntax node, TypeSymbol inputType, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            TypeSymbol declType = BindRecursivePatternType(typeSyntax, inputType, diagnostics, ref hasErrors, out BoundTypeExpression boundDeclType);

            var patterns = ArrayBuilder<BoundPattern>.GetInstance(node.SubPatterns.Count);
            MethodSymbol deconstructMethod = null;
            if (declType.IsTupleType)
            {
                // It is a tuple type. Work according to its elements
                ImmutableArray<TypeSymbol> elementTypes = declType.TupleElementTypes;
                if (elementTypes.Length != node.SubPatterns.Count && !hasErrors)
                {
                    var location = new SourceLocation(node.SyntaxTree, new Text.TextSpan(node.OpenParenToken.SpanStart, node.CloseParenToken.Span.End - node.OpenParenToken.SpanStart));
                    diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, location, declType, elementTypes.Length, node.SubPatterns.Count);
                    hasErrors = true;
                }
                for (int i = 0; i < node.SubPatterns.Count; i++)
                {
                    var subPattern = node.SubPatterns[i];
                    bool isError = i >= elementTypes.Length;
                    TypeSymbol elementType = isError ? CreateErrorType() : elementTypes[i];
                    if (subPattern.NameColon != null)
                    {
                        string name = subPattern.NameColon.Name.Identifier.ValueText;
                        FieldSymbol foundField = CheckIsTupleElement(subPattern.NameColon.Name, (NamedTypeSymbol)declType, name, i, diagnostics);
                        // PROTOTYPE(patterns2): Should the tuple field binding for the name be stored somewhere in the node?

                    }
                    BoundPattern boundSubpattern = BindPattern(subPattern.Pattern, elementType, isError, diagnostics);
                    patterns.Add(boundSubpattern);
                }
            }
            else
            {
                // It is not a tuple type. Seek an appropriate Deconstruct method.
                var inputPlaceholder = new BoundImplicitReceiver(node, declType); // A fake receiver expression to permit us to reuse binding logic
                // PROTOTYPE(patterns2): Can we include element names node.SubPatterns[i].NameColon?.Name in the AnalyzedArguments
                // used in MakeDeconstructInvocationExpression so they are used to disambiguate? LDM needs to reconcile with deconstruction.
                BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                    node.SubPatterns.Count, inputPlaceholder, node, diagnostics, outPlaceholders: out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders);
                deconstructMethod = deconstruct.ExpressionSymbol as MethodSymbol;
                int skippedExtensionParameters = deconstructMethod?.IsExtensionMethod == true ? 1 : 0;
                for (int i = 0; i < node.SubPatterns.Count; i++)
                {
                    var subPattern = node.SubPatterns[i];
                    bool isError = outPlaceholders.IsDefaultOrEmpty || i >= outPlaceholders.Length;
                    TypeSymbol elementType = isError ? CreateErrorType() : outPlaceholders[i].Type;
                    if (subPattern.NameColon != null && !isError)
                    {
                        // Check that the given name is the same as the corresponding parameter of the method.
                        string name = subPattern.NameColon.Name.Identifier.ValueText;
                        int parameterIndex = i + skippedExtensionParameters;
                        string parameterName = deconstructMethod.Parameters[parameterIndex].Name;
                        if (name != parameterName)
                        {
                            diagnostics.Add(ErrorCode.ERR_DeconstructParameterNameMismatch, subPattern.NameColon.Name.Location, name, parameterName);
                        }
                        // PROTOTYPE(patterns2): Should the parameter binding for the name be stored somewhere in the node?
                    }
                    BoundPattern boundSubpattern = BindPattern(subPattern.Pattern, elementType, isError, diagnostics);
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
                syntax: node, declaredType: boundDeclType, inputType: inputType, deconstructMethodOpt: deconstructMethod,
                deconstruction: patterns.ToImmutableAndFree(), propertiesOpt: propertiesOpt, variable: variableSymbol, variableAccess: variableAccess, hasErrors: hasErrors);
        }

        /// <summary>
        /// Check that the given name designates a tuple element at the given index, and return that element.
        /// </summary>
        private FieldSymbol CheckIsTupleElement(SyntaxNode node, NamedTypeSymbol tupleType, string name, int tupleIndex, DiagnosticBag diagnostics)
        {
            FieldSymbol foundElement = null;
            foreach (var symbol in tupleType.GetMembers(name))
            {
                if (symbol is FieldSymbol field && field.IsTupleElement())
                {
                    foundElement = field;
                    break;
                }
            }

            if (foundElement is null || foundElement.TupleElementIndex != tupleIndex)
            {
                diagnostics.Add(ErrorCode.ERR_TupleElementNameMismatch, node.Location, name, $"Item{tupleIndex+1}");
            }

            return foundElement;
        }

        private BoundPattern BindVarPattern(VarPatternSyntax node, TypeSymbol inputType, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSymbol declType = inputType;
            Symbol foundSymbol = BindTypeOrAliasOrKeyword(node.VarKeyword, node, diagnostics, out bool isVar);
            if (!isVar)
            {
                // Give an error if there is a bindable type "var" in scope
                diagnostics.Add(ErrorCode.ERR_VarMayNotBindToType, node.VarKeyword.GetLocation(), foundSymbol.ToDisplayString());
                hasErrors = true;
            }

            return BindVarDesignation(node, node.Designation, inputType, hasErrors, diagnostics);
        }

        private BoundPattern BindVarDesignation(VarPatternSyntax node, VariableDesignationSyntax designation, TypeSymbol inputType, bool hasErrors, DiagnosticBag diagnostics)
        {
            switch (designation.Kind())
            {
                case SyntaxKind.DiscardDesignation:
                    {
                        //return new BoundDiscardPattern(designation);
                        // PROTOTYPE(patterns2): this should bind as a discard pattern, but for now we'll bind it as a declaration
                        // pattern for compatibility with the later phases of the compiler that do not yet handle the discard pattern.
                        var boundOperandType = new BoundTypeExpression(
                            syntax: node, aliasOpt: null, type: inputType); // fake a type expression for the variable's type
                        return new BoundDeclarationPattern(
                            syntax: designation, variable: null, variableAccess: null, declaredType: boundOperandType, isVar: true, inputType: inputType, hasErrors: hasErrors);
                    }
                case SyntaxKind.SingleVariableDesignation:
                    {
                        BindPatternDesignation(
                            node: node, designation: designation, declType: inputType, typeSyntax: null, diagnostics: diagnostics,
                            hasErrors: ref hasErrors, variableSymbol: out Symbol variableSymbol, variableAccess: out BoundExpression variableAccess);
                        var boundOperandType = new BoundTypeExpression(syntax: node, aliasOpt: null, type: inputType); // fake a type expression for the variable's type
                        return new BoundDeclarationPattern(designation, variableSymbol, variableAccess, boundOperandType, isVar: true, inputType: inputType, hasErrors: hasErrors);
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        var tupleDesignation = (ParenthesizedVariableDesignationSyntax)designation;
                        var patterns = ArrayBuilder<BoundPattern>.GetInstance(tupleDesignation.Variables.Count);
                        MethodSymbol deconstructMethod = null;
                        if (inputType.IsTupleType)
                        {
                            // It is a tuple type. Work according to its elements
                            ImmutableArray<TypeSymbol> elementTypes = inputType.TupleElementTypes;
                            if (elementTypes.Length != tupleDesignation.Variables.Count && !hasErrors)
                            {
                                var location = new SourceLocation(node.SyntaxTree, 
                                    new Text.TextSpan(tupleDesignation.OpenParenToken.SpanStart, tupleDesignation.CloseParenToken.Span.End - tupleDesignation.OpenParenToken.SpanStart));
                                diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, location, inputType.TupleElementTypes, elementTypes.Length, tupleDesignation.Variables.Count);
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
                            var inputPlaceholder = new BoundImplicitReceiver(node, inputType); // A fake receiver expression to permit us to reuse binding logic
                            BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                                tupleDesignation.Variables.Count, inputPlaceholder, node, diagnostics, outPlaceholders: out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders);
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
                            syntax: node, declaredType: null, inputType: inputType, deconstructMethodOpt: deconstructMethod,
                            deconstruction: patterns.ToImmutableAndFree(), propertiesOpt: default, variable: null, variableAccess: null, hasErrors: hasErrors);
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(designation.Kind());
                    }
            }
        }

        private BoundPattern BindPropertyPattern(PropertyPatternSyntax node, TypeSymbol inputType, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            TypeSymbol declType = BindRecursivePatternType(typeSyntax, inputType, diagnostics, ref hasErrors, out BoundTypeExpression boundDeclType);
            ImmutableArray<(Symbol property, BoundPattern pattern)> propertiesOpt = BindPropertySubpattern(node.PropertySubpattern, declType, diagnostics, ref hasErrors);
            BindPatternDesignation(node, node.Designation, declType, typeSyntax, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
            return new BoundRecursivePattern(
                syntax: node, declaredType: boundDeclType, inputType: inputType, deconstructMethodOpt: null,
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
                    member = LookupMemberForPropertyPattern(inputType, name, diagnostics, ref hasErrors, out memberType);
                }

                BoundPattern boundPattern = BindPattern(pattern, memberType, hasErrors, diagnostics);
                builder.Add((member, boundPattern));
            }

            return builder.ToImmutableAndFree();
        }

        private Symbol LookupMemberForPropertyPattern(
            TypeSymbol inputType, IdentifierNameSyntax name, DiagnosticBag diagnostics, ref bool hasErrors, out TypeSymbol memberType)
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
                indexed: false,
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

            if (hasErrors || !CheckValueKind(node: memberName.Parent, expr: boundMember, valueKind: BindValueKind.RValue,
                                             checkingReceiver: false, diagnostics: diagnostics))
            {
                return null;
            }

            return boundMember.ExpressionSymbol;
        }
    }
}
