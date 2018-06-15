// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Binder
    {
        private BoundExpression BindIsPatternExpression(IsPatternExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var expression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            var hasErrors = IsOperandErrors(node, ref expression, diagnostics);
            var expressionType = expression.Type;
            if ((object)expressionType == null || expressionType.SpecialType == SpecialType.System_Void)
            {
                expressionType = CreateErrorType();
                if (!hasErrors)
                {
                    // value expected
                    diagnostics.Add(ErrorCode.ERR_BadIsPatternExpression, node.Expression.Location, expression.Display);
                    hasErrors = true;
                }
            }

            var pattern = BindPattern(expression, node.Pattern, expressionType, hasErrors, diagnostics);
            if (!hasErrors && pattern is BoundDeclarationPattern p && !p.IsVar && expression.ConstantValue == ConstantValue.Null)
            {
                diagnostics.Add(ErrorCode.WRN_IsAlwaysFalse, node.Location, p.DeclaredType.Type);
            }

            return new BoundIsPatternExpression(
                node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node), hasErrors);
        }

        internal BoundPattern BindPattern(
            BoundExpression sourceExpression,
            PatternSyntax node,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern(
                        sourceExpression, (DeclarationPatternSyntax)node, operandType, hasErrors, diagnostics);

                case SyntaxKind.ConstantPattern:
                    var constantPattern = (ConstantPatternSyntax)node;
                    return BindConstantPattern(
                        constantPattern, operandType, constantPattern.Expression, hasErrors, diagnostics, out bool wasExpression);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        internal BoundConstantPattern BindConstantPattern(
            CSharpSyntaxNode node,
            TypeSymbol operandType,
            ExpressionSyntax patternExpression,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression)
        {
            var innerExpression = patternExpression.SkipParens();
            if (innerExpression.Kind() == SyntaxKind.DefaultLiteralExpression)
            {
                diagnostics.Add(ErrorCode.ERR_DefaultInPattern, innerExpression.Location);
                hasErrors = true;
            }

            return BindConstantAsPattern(node, operandType, patternExpression, hasErrors, diagnostics, out wasExpression);
        }

        internal BoundConstantPattern BindConstantAsPattern(
            CSharpSyntaxNode node,
            TypeSymbol operandType,
            ExpressionSyntax patternExpression,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression)
        {
            var expression = BindValue(patternExpression, diagnostics, BindValueKind.RValue);
            ConstantValue constantValueOpt = null;
            var convertedExpression = ConvertPatternExpression(operandType, patternExpression, expression, ref constantValueOpt, diagnostics);
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
                var operand = conversion.Operand;
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
                var matchPossible = ExpressionOfTypeMatchesPatternType(Conversions, operandType, patternType, ref useSiteDiagnostics, out Conversion conversion, operandConstantValue: null, operandCouldBeNull: true);
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

            conversion = conversions.ClassifyBuiltInConversion(expressionType, patternType, ref useSiteDiagnostics);
            var result = Binder.GetIsOperatorConstantResult(expressionType, patternType, conversion.Kind, operandConstantValue, operandCouldBeNull);
            return
                (result == null) ? (bool?)null :
                (result == ConstantValue.True) ? true :
                (result == ConstantValue.False) ? false :
                throw ExceptionUtilities.UnexpectedValue(result);
        }

        private BoundPattern BindDeclarationPattern(
            BoundExpression sourceExpression,
            DeclarationPatternSyntax node,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(operandType != (object)null);

            var typeSyntax = node.Type;

            bool isVar;
            AliasSymbol aliasOpt;
            TypeSymbol declType = BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar, out aliasOpt);
            if (isVar)
            {
                declType = operandType;
            }

            if (declType == (object)null)
            {
                Debug.Assert(hasErrors);
                declType = this.CreateErrorType("var");
            }

            var boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: isVar, type: declType);
            if (IsOperatorErrors(node, operandType, boundDeclType, diagnostics))
            {
                hasErrors = true;
            }
            else
            {
                hasErrors |= CheckValidPatternType(typeSyntax, operandType, declType,
                                                   isVar: isVar, patternTypeWasInSource: true, diagnostics: diagnostics);
            }

            switch (node.Designation.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    break;
                case SyntaxKind.DiscardDesignation:
                    return new BoundDeclarationPattern(node, null, boundDeclType, isVar, hasErrors);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Designation.Kind());
            }

            var designation = (SingleVariableDesignationSyntax)node.Designation;
            var identifier = designation.Identifier;
            SourceLocalSymbol localSymbol = this.LookupLocal(identifier);

            if (localSymbol != (object)null)
            {
                if ((InConstructorInitializer || InFieldInitializer) && ContainingMemberOrLambda.ContainingSymbol.Kind == SymbolKind.NamedType)
                {
                    CheckFeatureAvailability(node, MessageID.IDS_FeatureExpressionVariablesInQueriesAndInitializers, diagnostics);
                }

                localSymbol.SetType(declType);
                localSymbol.SetValEscape(GetValEscape(sourceExpression, LocalScopeDepth));

                // Check for variable declaration errors.
                hasErrors |= localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

                if (!hasErrors)
                {
                    hasErrors = CheckRestrictedTypeInAsync(this.ContainingMemberOrLambda, declType, diagnostics, typeSyntax);
                }

                return new BoundDeclarationPattern(node, localSymbol, boundDeclType, isVar, hasErrors);
            }
            else
            {
                // We should have the right binder in the chain for a script or interactive, so we use the field for the pattern.
                Debug.Assert(node.SyntaxTree.Options.Kind != SourceCodeKind.Regular);
                GlobalExpressionVariable expressionVariableField = LookupDeclaredField(designation);
                DiagnosticBag tempDiagnostics = DiagnosticBag.GetInstance();
                expressionVariableField.SetType(declType, tempDiagnostics);
                tempDiagnostics.Free();
                BoundExpression receiver = SynthesizeReceiver(node, expressionVariableField, diagnostics);
                var variableAccess = new BoundFieldAccess(node, receiver, expressionVariableField, null, hasErrors);
                return new BoundDeclarationPattern(node, expressionVariableField, variableAccess, boundDeclType, isVar, hasErrors);
            }
        }

    }
}
