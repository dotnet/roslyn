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
                if (!hasErrors)
                {
                    // value expected
                    diagnostics.Add(ErrorCode.ERR_BadPatternExpression, node.Expression.Location, expression.Display);
                    hasErrors = true;
                }

                expression = BadExpression(expression.Syntax, expression);
            }

            uint inputValEscape = GetValEscape(expression, LocalScopeDepth);
            BoundPattern pattern = BindPattern(node.Pattern, expression.Type, inputValEscape, hasErrors, diagnostics);
            hasErrors |= pattern.HasErrors;
            return MakeIsPatternExpression(
                node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node),
                hasErrors, diagnostics);
        }

        private BoundExpression MakeIsPatternExpression(SyntaxNode node, BoundExpression expression, BoundPattern pattern, TypeSymbol boolType, bool hasErrors, DiagnosticBag diagnostics)
        {
            // Note that these labels are for the convenience of the compilation of patterns, and are not actually emitted into the lowered code.
            LabelSymbol whenTrueLabel = new GeneratedLabelSymbol("isPatternSuccess");
            LabelSymbol whenFalseLabel = new GeneratedLabelSymbol("isPatternFailure");
            BoundDecisionDag decisionDag = DecisionDagBuilder.CreateDecisionDagForIsPattern(
                this.Compilation, pattern.Syntax, expression, pattern, whenTrueLabel: whenTrueLabel, whenFalseLabel: whenFalseLabel, diagnostics);
            if (!hasErrors && !decisionDag.ReachableLabels.Contains(whenTrueLabel))
            {
                diagnostics.Add(ErrorCode.ERR_IsPatternImpossible, node.Location, expression.Type);
                hasErrors = true;
            }

            if (expression.ConstantValue != null)
            {
                decisionDag = decisionDag.SimplifyDecisionDagIfConstantInput(expression);
                if (!hasErrors)
                {
                    if (!decisionDag.ReachableLabels.Contains(whenTrueLabel))
                    {
                        diagnostics.Add(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, node.Location);
                    }
                    else if (!decisionDag.ReachableLabels.Contains(whenFalseLabel) && pattern.Kind == BoundKind.ConstantPattern)
                    {
                        diagnostics.Add(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, node.Location);
                    }
                }
            }

            return new BoundIsPatternExpression(node, expression, pattern, decisionDag, whenTrueLabel: whenTrueLabel, whenFalseLabel: whenFalseLabel, boolType, hasErrors);
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
            uint inputValEscape,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DiscardPattern:
                    return BindDiscardPattern((DiscardPatternSyntax)node, inputType);

                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern((DeclarationPatternSyntax)node, inputType, inputValEscape, hasErrors, diagnostics);

                case SyntaxKind.ConstantPattern:
                    return BindConstantPattern((ConstantPatternSyntax)node, inputType, hasErrors, diagnostics);

                case SyntaxKind.RecursivePattern:
                    return BindRecursivePattern((RecursivePatternSyntax)node, inputType, inputValEscape, hasErrors, diagnostics);

                case SyntaxKind.VarPattern:
                    return BindVarPattern((VarPatternSyntax)node, inputType, inputValEscape, hasErrors, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundPattern BindDiscardPattern(DiscardPatternSyntax node, TypeSymbol inputType)
        {
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

            return BindConstantPattern(node, inputType, node.Expression, hasErrors, diagnostics, out _);
        }

        internal BoundConstantPattern BindConstantPattern(
            CSharpSyntaxNode node,
            TypeSymbol inputType,
            ExpressionSyntax patternExpression,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression)
        {
            BoundExpression expression = BindValue(patternExpression, diagnostics, BindValueKind.RValue);
            ConstantValue constantValueOpt = null;
            BoundExpression convertedExpression = ConvertPatternExpression(inputType, patternExpression, expression, out constantValueOpt, hasErrors, diagnostics);
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
                    convertedExpression.Syntax, convertedExpression, Conversion.NoConversion, isBaseConversion: false, @checked: false,
                    explicitCastInCode: false, constantValueOpt: constantValueOpt, conversionGroupOpt: default, type: CreateErrorType(), hasErrors: true)
                { WasCompilerGenerated = true };
            }

            return new BoundConstantPattern(node, convertedExpression, constantValueOpt ?? ConstantValue.Bad, inputType, hasErrors);
        }

        internal BoundExpression ConvertPatternExpression(
            TypeSymbol inputType,
            CSharpSyntaxNode node,
            BoundExpression expression,
            out ConstantValue constantValue,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            BoundExpression convertedExpression;

            // If we are pattern-matching against an open type, we do not convert the constant to the type of the input.
            // This permits us to match a value of type `IComparable<T>` with a pattern of type `int`.
            bool inputContainsTypeParameter = inputType.ContainsTypeParameter();
            if (inputContainsTypeParameter)
            {
                convertedExpression = expression;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                if (!hasErrors)
                {
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
            else if (inputType.TypeKind == TypeKind.Pointer || patternType.TypeKind == TypeKind.Pointer)
            {
                // pattern-matching is not permitted for pointer types
                diagnostics.Add(ErrorCode.ERR_PointerTypeInPatternMatching, typeSyntax.Location);
                return true;
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
            uint inputValEscape,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            BoundTypeExpression boundDeclType = BindPatternType(typeSyntax, inputType, diagnostics, ref hasErrors, out bool isVar);
            Debug.Assert(!typeSyntax.IsVar); // if the syntax had `var`, it would have been parsed as a var pattern.
            Debug.Assert(!isVar);
            TypeSymbol declType = boundDeclType.Type;
            inputValEscape = GetValEscape(declType, inputValEscape);
            BindPatternDesignation(node, node.Designation, boundDeclType.Type, inputValEscape, typeSyntax, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
            return new BoundDeclarationPattern(node, variableSymbol, variableAccess, boundDeclType, isVar: false, inputType, hasErrors);
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
            TypeSymbolWithAnnotations declType = BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar, out aliasOpt);
            if (isVar)
            {
                declType = TypeSymbolWithAnnotations.Create(NonNullTypesContext, inputType);
            }

            if (declType.IsNull)
            {
                Debug.Assert(hasErrors);
                declType = TypeSymbolWithAnnotations.Create(NonNullTypesContext, this.CreateErrorType("var"));
            }

            BoundTypeExpression boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: isVar, type: declType.TypeSymbol);
            hasErrors |= CheckValidPatternType(typeSyntax, inputType, declType.TypeSymbol,
                                               isVar: isVar, patternTypeWasInSource: true, diagnostics: diagnostics);
            return boundDeclType;
        }

        private void BindPatternDesignation(
            PatternSyntax node,
            VariableDesignationSyntax designation,
            TypeSymbol declType,
            uint inputValEscape,
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

                        localSymbol.SetType(TypeSymbolWithAnnotations.Create(declType));
                        localSymbol.SetValEscape(GetValEscape(declType, inputValEscape));

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
                        expressionVariableField.SetType(TypeSymbolWithAnnotations.Create(declType), tempDiagnostics);
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

        /// <summary>
        /// Compute the val escape of an expression of the given <paramref name="type"/>, which is known to be derived
        /// from an expression whose escape scope is <paramref name="possibleValEscape"/>. By the language rules, the
        /// result is either that same scope (if the type is a ref struct type) or <see cref="Binder.ExternalScope"/>.
        /// </summary>
        private static uint GetValEscape(TypeSymbol type, uint possibleValEscape)
        {
            return type.IsByRefLikeType ? possibleValEscape : Binder.ExternalScope;
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

        private BoundPattern BindRecursivePattern(RecursivePatternSyntax node, TypeSymbol inputType, uint inputValEscape, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            TypeSymbol declType = BindRecursivePatternType(typeSyntax, inputType, diagnostics, ref hasErrors, out BoundTypeExpression boundDeclType);
            inputValEscape = GetValEscape(declType, inputValEscape);

            if (ShouldUseITuple(node, declType, diagnostics, out NamedTypeSymbol iTupleType, out MethodSymbol iTupleGetLength, out MethodSymbol iTupleGetItem))
            {
                return BindITuplePattern(node, inputType, iTupleType, iTupleGetLength, iTupleGetItem, hasErrors, diagnostics);
            }

            MethodSymbol deconstructMethod = null;
            ImmutableArray<BoundSubpattern> deconstructionSubpatterns = default;
            if (node.DeconstructionPatternClause != null)
            {
                deconstructionSubpatterns = BindDeconstructionPatternClause(node.DeconstructionPatternClause, declType, inputValEscape, diagnostics, out deconstructMethod, ref hasErrors);
            }

            ImmutableArray<BoundSubpattern> properties = default;
            if (node.PropertyPatternClause != null)
            {
                properties = BindPropertyPatternClause(node.PropertyPatternClause, declType, inputValEscape, diagnostics, ref hasErrors);
            }

            BindPatternDesignation(node, node.Designation, declType, inputValEscape, typeSyntax, diagnostics, ref hasErrors, out Symbol variableSymbol, out BoundExpression variableAccess);
            return new BoundRecursivePattern(
                syntax: node, declaredType: boundDeclType, inputType: inputType, deconstructMethod: deconstructMethod,
                deconstruction: deconstructionSubpatterns, properties: properties, variable: variableSymbol, variableAccess: variableAccess, hasErrors: hasErrors);
        }

        private BoundPattern BindITuplePattern(
            RecursivePatternSyntax node,
            TypeSymbol inputType,
            NamedTypeSymbol iTupleType,
            MethodSymbol iTupleGetLength,
            MethodSymbol iTupleGetItem,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            var objectType = Compilation.GetSpecialType(SpecialType.System_Object);
            var deconstruction = node.DeconstructionPatternClause;
            var patterns = ArrayBuilder<BoundSubpattern>.GetInstance(deconstruction.Subpatterns.Count);
            foreach (var subpatternSyntax in deconstruction.Subpatterns)
            {
                TypeSymbol elementType = objectType;
                if (subpatternSyntax.NameColon != null)
                {
                    // error: name not permitted in ITuple deconstruction
                    diagnostics.Add(ErrorCode.ERR_ArgumentNameInITuplePattern, subpatternSyntax.NameColon.Location);
                }

                // Since the input has been cast to ITuple, it must be escapable.
                const uint valEscape = Binder.ExternalScope;
                var boundSubpattern = new BoundSubpattern(
                    subpatternSyntax,
                    null,
                    BindPattern(subpatternSyntax.Pattern, elementType, valEscape, false, diagnostics));
                patterns.Add(boundSubpattern);
            }

            return new BoundITuplePattern(node, iTupleGetLength, iTupleGetItem, patterns.ToImmutableAndFree(), inputType, hasErrors);
        }

        private ImmutableArray<BoundSubpattern> BindDeconstructionPatternClause(
            DeconstructionPatternClauseSyntax node,
            TypeSymbol declType,
            uint inputValEscape,
            DiagnosticBag diagnostics,
            out MethodSymbol deconstructMethod,
            ref bool hasErrors)
        {
            deconstructMethod = null;
            var patterns = ArrayBuilder<BoundSubpattern>.GetInstance(node.Subpatterns.Count);
            if (declType.IsTupleType)
            {
                // It is a tuple type. Work according to its elements
                ImmutableArray<TypeSymbolWithAnnotations> elementTypes = declType.TupleElementTypes;
                if (elementTypes.Length != node.Subpatterns.Count && !hasErrors)
                {
                    var location = new SourceLocation(node.SyntaxTree, new Text.TextSpan(node.OpenParenToken.SpanStart, node.CloseParenToken.Span.End - node.OpenParenToken.SpanStart));
                    diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, location, declType, elementTypes.Length, node.Subpatterns.Count);
                    hasErrors = true;
                }

                for (int i = 0; i < node.Subpatterns.Count; i++)
                {
                    var subpatternSyntax = node.Subpatterns[i];
                    bool isError = i >= elementTypes.Length;
                    TypeSymbol elementType = isError ? CreateErrorType() : elementTypes[i].TypeSymbol;
                    FieldSymbol foundField = null;
                    if (subpatternSyntax.NameColon != null)
                    {
                        string name = subpatternSyntax.NameColon.Name.Identifier.ValueText;
                        foundField = CheckIsTupleElement(subpatternSyntax.NameColon.Name, (NamedTypeSymbol)declType, name, i, diagnostics);
                    }

                    BoundSubpattern boundSubpattern = new BoundSubpattern(
                        subpatternSyntax,
                        foundField,
                        BindPattern(subpatternSyntax.Pattern, elementType, GetValEscape(elementType, inputValEscape), isError, diagnostics));
                    patterns.Add(boundSubpattern);
                }
            }
            else
            {
                // It is not a tuple type or ITuple. Seek an appropriate Deconstruct method.
                var inputPlaceholder = new BoundImplicitReceiver(node, declType); // A fake receiver expression to permit us to reuse binding logic
                BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                    node.Subpatterns.Count, inputPlaceholder, node, diagnostics, outPlaceholders: out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders);
                deconstructMethod = deconstruct.ExpressionSymbol as MethodSymbol;
                if (deconstructMethod is null)
                {
                    hasErrors = true;
                }

                int skippedExtensionParameters = deconstructMethod?.IsExtensionMethod == true ? 1 : 0;
                for (int i = 0; i < node.Subpatterns.Count; i++)
                {
                    var subPattern = node.Subpatterns[i];
                    bool isError = outPlaceholders.IsDefaultOrEmpty || i >= outPlaceholders.Length;
                    TypeSymbol elementType = isError ? CreateErrorType() : outPlaceholders[i].Type;
                    ParameterSymbol parameter = null;
                    if (subPattern.NameColon != null && !isError)
                    {
                        // Check that the given name is the same as the corresponding parameter of the method.
                        string name = subPattern.NameColon.Name.Identifier.ValueText;
                        int parameterIndex = i + skippedExtensionParameters;
                        if (parameterIndex < deconstructMethod.ParameterCount)
                        {
                            parameter = deconstructMethod.Parameters[parameterIndex];
                            string parameterName = parameter.Name;
                            if (name != parameterName)
                            {
                                diagnostics.Add(ErrorCode.ERR_DeconstructParameterNameMismatch, subPattern.NameColon.Name.Location, name, parameterName);
                            }
                        }
                        else
                        {
                            Debug.Assert(deconstructMethod is ErrorMethodSymbol);
                        }
                    }

                    var boundSubpattern = new BoundSubpattern(
                        subPattern,
                        parameter,
                        BindPattern(subPattern.Pattern, elementType, GetValEscape(elementType, inputValEscape), isError, diagnostics)
                        );
                    patterns.Add(boundSubpattern);
                }
            }

            return patterns.ToImmutableAndFree();
        }

        private bool ShouldUseITuple(
            RecursivePatternSyntax node,
            TypeSymbol declType,
            DiagnosticBag diagnostics,
            out NamedTypeSymbol iTupleType,
            out MethodSymbol iTupleGetLength,
            out MethodSymbol iTupleGetItem)
        {
            iTupleType = null;
            iTupleGetLength = iTupleGetItem = null;
            if (declType.IsTupleType)
            {
                return false;
            }

            if (node.Type != null)
            {
                // ITuple matching only applies if no type is given explicitly.
                return false;
            }

            if (node.PropertyPatternClause != null)
            {
                // ITuple matching only applies if there is no property pattern part.
                return false;
            }

            if (node.DeconstructionPatternClause == null)
            {
                // ITuple matching only applies if there is a deconstruction pattern part.
                return false;
            }

            if (node.Designation != null)
            {
                // ITuple matching only applies if there is no designation (what type would the designation be?)
                return false;
            }

            if (Compilation.LanguageVersion < MessageID.IDS_FeatureRecursivePatterns.RequiredVersion())
            {
                return false;
            }

            iTupleType = Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ITuple);
            if (iTupleType.TypeKind != TypeKind.Interface)
            {
                // When compiling to a platform that lacks the interface ITuple (i.e. it is an error type), we simply do not match using it.
                return false;
            }

            // Resolution 2017-11-20 LDM: permit matching via ITuple only for `object`, `ITuple`, and types that are
            // declared to implement `ITuple` but contain no `Deconstruct` methods.
            if (declType != (object)Compilation.GetSpecialType(SpecialType.System_Object) &&
                declType != (object)Compilation.DynamicType &&
                declType != (object)iTupleType &&
                !hasBaseInterface(declType, iTupleType))
            {
                return false;
            }

            // If there are any accessible Deconstruct method members, then we do not permit ITuple
            var lookupResult = LookupResult.GetInstance();
            try
            {
                const LookupOptions options = LookupOptions.MustBeInvocableIfMember | LookupOptions.AllMethodsOnArityZero;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                this.LookupMembersWithFallback(lookupResult, declType, WellKnownMemberNames.DeconstructMethodName, arity: 0, ref useSiteDiagnostics, basesBeingResolved: null, options: options);
                diagnostics.Add(node, useSiteDiagnostics);
                if (lookupResult.IsMultiViable)
                {
                    foreach (var symbol in lookupResult.Symbols)
                    {
                        if (symbol.Kind == SymbolKind.Method)
                        {
                            return false;
                        }
                    }
                }
            }
            finally
            {
                lookupResult.Free();
            }

            // Ensure ITuple has a Length and indexer
            iTupleGetLength = (MethodSymbol)Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ITuple__get_Length);
            iTupleGetItem = (MethodSymbol)Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ITuple__get_Item);
            if (iTupleGetLength is null || iTupleGetItem is null)
            {
                // This might not result in an ideal diagnostic
                return false;
            }

            // passed all the filters; permit using ITuple
            return true;

            bool hasBaseInterface(TypeSymbol type, NamedTypeSymbol possibleBaseInterface)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var result = Compilation.Conversions.ClassifyBuiltInConversion(type, possibleBaseInterface, ref useSiteDiagnostics).IsImplicit;
                diagnostics.Add(node, useSiteDiagnostics);
                return result;
            }
        }

        /// <summary>
        /// Check that the given name designates a tuple element at the given index, and return that element.
        /// </summary>
        private static FieldSymbol CheckIsTupleElement(SyntaxNode node, NamedTypeSymbol tupleType, string name, int tupleIndex, DiagnosticBag diagnostics)
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

        private BoundPattern BindVarPattern(VarPatternSyntax node, TypeSymbol inputType, uint inputValEscape, bool hasErrors, DiagnosticBag diagnostics)
        {
            TypeSymbol declType = inputType;
            Symbol foundSymbol = BindTypeOrAliasOrKeyword(node.VarKeyword, node, diagnostics, out bool isVar);
            if (!isVar)
            {
                // Give an error if there is a bindable type "var" in scope
                diagnostics.Add(ErrorCode.ERR_VarMayNotBindToType, node.VarKeyword.GetLocation(), foundSymbol.ToDisplayString());
                hasErrors = true;
            }

            return BindVarDesignation(node, node.Designation, inputType, inputValEscape, hasErrors, diagnostics);
        }

        private BoundPattern BindVarDesignation(VarPatternSyntax node, VariableDesignationSyntax designation, TypeSymbol inputType, uint inputValEscape, bool hasErrors, DiagnosticBag diagnostics)
        {
            switch (designation.Kind())
            {
                case SyntaxKind.DiscardDesignation:
                    {
                        return new BoundDiscardPattern(designation, inputType);
                    }
                case SyntaxKind.SingleVariableDesignation:
                    {
                        BindPatternDesignation(
                            node: node, designation: designation, declType: inputType, inputValEscape: inputValEscape, typeSyntax: null, diagnostics: diagnostics,
                            hasErrors: ref hasErrors, variableSymbol: out Symbol variableSymbol, variableAccess: out BoundExpression variableAccess);
                        var boundOperandType = new BoundTypeExpression(syntax: node, aliasOpt: null, type: inputType); // fake a type expression for the variable's type
                        // We continue to use a BoundDeclarationPattern for the var pattern, as they have more in common.
                        return new BoundDeclarationPattern(
                            designation.Parent == node ? node : (SyntaxNode)designation, // for `var x` use whole pattern, otherwise use designation for the syntax
                            variableSymbol, variableAccess, boundOperandType, isVar: true, inputType: inputType, hasErrors: hasErrors);
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        var tupleDesignation = (ParenthesizedVariableDesignationSyntax)designation;
                        var subPatterns = ArrayBuilder<BoundSubpattern>.GetInstance(tupleDesignation.Variables.Count);
                        MethodSymbol deconstructMethod = null;
                        if (inputType.IsTupleType)
                        {
                            // It is a tuple type. Work according to its elements
                            ImmutableArray<TypeSymbolWithAnnotations> elementTypes = inputType.TupleElementTypes;
                            if (elementTypes.Length != tupleDesignation.Variables.Count && !hasErrors)
                            {
                                var location = new SourceLocation(node.SyntaxTree, 
                                    new Text.TextSpan(tupleDesignation.OpenParenToken.SpanStart, tupleDesignation.CloseParenToken.Span.End - tupleDesignation.OpenParenToken.SpanStart));
                                diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, location, inputType.TupleElementTypes, elementTypes.Length, tupleDesignation.Variables.Count);
                                hasErrors = true;
                            }
                            for (int i = 0; i < tupleDesignation.Variables.Count; i++)
                            {
                                var variable = tupleDesignation.Variables[i];
                                bool isError = i >= elementTypes.Length;
                                TypeSymbol elementType = isError ? CreateErrorType() : elementTypes[i].TypeSymbol;
                                BoundPattern pattern = BindVarDesignation(node, variable, elementType, GetValEscape(elementType, inputValEscape), isError, diagnostics);
                                subPatterns.Add(new BoundSubpattern(variable, symbol: null, pattern));
                            }
                        }
                        else
                        {
                            // It is not a tuple type. Seek an appropriate Deconstruct method.
                            var inputPlaceholder = new BoundImplicitReceiver(node, inputType); // A fake receiver expression to permit us to reuse binding logic
                            BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                                tupleDesignation.Variables.Count, inputPlaceholder, node, diagnostics, outPlaceholders: out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders);
                            deconstructMethod = deconstruct.ExpressionSymbol as MethodSymbol;
                            for (int i = 0; i < tupleDesignation.Variables.Count; i++)
                            {
                                var variable = tupleDesignation.Variables[i];
                                bool isError = outPlaceholders.IsDefaultOrEmpty || i >= outPlaceholders.Length;
                                TypeSymbol elementType = isError ? CreateErrorType() : outPlaceholders[i].Type;
                                BoundPattern pattern = BindVarDesignation(node, variable, elementType, GetValEscape(elementType, inputValEscape), isError, diagnostics);
                                subPatterns.Add(new BoundSubpattern(variable, symbol: null, pattern));
                            }
                        }

                        return new BoundRecursivePattern(
                            syntax: node, declaredType: null, inputType: inputType, deconstructMethod: deconstructMethod,
                            deconstruction: subPatterns.ToImmutableAndFree(), properties: default, variable: null, variableAccess: null, hasErrors: hasErrors);
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(designation.Kind());
                    }
            }
        }

        ImmutableArray<BoundSubpattern> BindPropertyPatternClause(
            PropertyPatternClauseSyntax node,
            TypeSymbol inputType,
            uint inputValEscape,
            DiagnosticBag diagnostics,
            ref bool hasErrors)
        {
            var builder = ArrayBuilder<BoundSubpattern>.GetInstance(node.Subpatterns.Count);
            foreach (SubpatternSyntax p in node.Subpatterns)
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

                BoundPattern boundPattern = BindPattern(pattern, memberType, GetValEscape(memberType, inputValEscape), hasErrors, diagnostics);
                builder.Add(new BoundSubpattern(p, member, boundPattern));
            }

            return builder.ToImmutableAndFree();
        }

        private Symbol LookupMemberForPropertyPattern(
            TypeSymbol inputType, IdentifierNameSyntax name, DiagnosticBag diagnostics, ref bool hasErrors, out TypeSymbol memberType)
        {
            Symbol symbol = BindPropertyPatternMember(inputType, name, ref hasErrors, diagnostics);

            if (inputType.IsErrorType() || hasErrors || symbol == (object)null)
            {
                memberType = CreateErrorType();
            }
            else
            {
                memberType = symbol.GetTypeOrReturnType().TypeSymbol;
            }

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
                typeArguments: default(ImmutableArray<TypeSymbolWithAnnotations>),
                invoked: false,
                indexed: false,
                diagnostics: diagnostics);

            if (boundMember.Kind == BoundKind.PropertyGroup)
            {
                boundMember = BindIndexedPropertyAccess(
                    (BoundPropertyGroup)boundMember, mustHaveAllOptionalParameters: true, diagnostics: diagnostics);
            }

            hasErrors |= boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;

            switch (boundMember.Kind)
            {
                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                    break;

                case BoundKind.IndexerAccess:
                case BoundKind.DynamicIndexerAccess:
                case BoundKind.EventAccess:
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
                                break;
                        }
                    }

                    hasErrors = true;
                    return boundMember.ExpressionSymbol;
            }

            if (hasErrors || !CheckValueKind(node: memberName.Parent, expr: boundMember, valueKind: BindValueKind.RValue,
                                             checkingReceiver: false, diagnostics: diagnostics))
            {
                hasErrors = true;
            }

            return boundMember.ExpressionSymbol;
        }
    }
}
