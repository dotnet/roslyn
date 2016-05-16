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
            var expression = BindExpression(node.Expression, diagnostics);
            var hasErrors = node.HasErrors || IsOperandErrors(node, expression, diagnostics);
            var pattern = BindPattern(node.Pattern, expression, expression.Type, hasErrors, diagnostics);
            return new BoundIsPatternExpression(
                node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node), hasErrors);
        }

        internal BoundPattern BindPattern(
            PatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics,
            bool wasSwitchCase = false)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern(
                        (DeclarationPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.ConstantPattern:
                    return BindConstantPattern(
                        (ConstantPatternSyntax)node, operand, operandType, hasErrors, diagnostics, wasSwitchCase);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }
        /// <summary>
        /// Is a user-defined `operator is` applicable? At the use site, we ignore those that are not.
        /// </summary>
        private bool ApplicableOperatorIs(MethodSymbol candidate, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            // must be a user-defined operator, and requires at least one parameter
            if (candidate.MethodKind != MethodKind.UserDefinedOperator || candidate.ParameterCount == 0)
            {
                return false;
            }

            // must be static.
            if (!candidate.IsStatic)
            {
                return false;
            }

            // the first parameter must be a value. The remaining parameters must be out.
            foreach (var parameter in candidate.Parameters)
            {
                if (parameter.RefKind != ((parameter.Ordinal == 0) ? RefKind.None : RefKind.Out))
                {
                    return false;
                }
            }

            // must return void or bool
            switch (candidate.ReturnType.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Boolean:
                    break;
                default:
                    return false;
            }

            // must not be generic
            if (candidate.Arity != 0)
            {
                return false;
            }

            // it should be accessible
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            bool isAccessible = this.IsAccessible(candidate, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            if (!isAccessible)
            {
                return false;
            }

            // all requirements are satisfied
            return true;
        }

        private BoundPattern BindConstantPattern(
            ConstantPatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics,
            bool wasSwitchCase)
        {
            bool wasExpression;
            return BindConstantPattern(node, operand, operandType, node.Expression, hasErrors, diagnostics, out wasExpression, wasSwitchCase);
        }

        internal BoundPattern BindConstantPattern(
            CSharpSyntaxNode node,
            BoundExpression left,
            TypeSymbol leftType,
            ExpressionSyntax right,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression,
            bool wasSwitchCase)
        {
            var expression = BindValue(right, diagnostics, BindValueKind.RValue);
            wasExpression = expression.Type?.IsErrorType() != true;
            if (!node.HasErrors && expression.ConstantValue == null)
            {
                diagnostics.Add(ErrorCode.ERR_ConstantExpected, right.Location);
                hasErrors = true;
            }

            return new BoundConstantPattern(node, expression, hasErrors);
        }

        private bool CheckValidPatternType(
            CSharpSyntaxNode typeSyntax,
            BoundExpression operand,
            TypeSymbol operandType,
            TypeSymbol patternType,
            bool patternTypeWasInSource,
            bool isVar,
            DiagnosticBag diagnostics)
        {
            if (operandType?.IsErrorType() == true || patternType?.IsErrorType() == true)
            {
                return false;
            }
            else if (patternType.IsNullableType() && !isVar && patternTypeWasInSource)
            {
                // It is an error to use pattern-matching with a nullable type, because you'll never get null. Use the underlying type.
                Error(diagnostics, ErrorCode.ERR_PatternNullableType, typeSyntax, patternType, patternType.GetNullableUnderlyingType());
                return true;
            }
            else if (operand != null && operandType == (object)null && !operand.HasAnyErrors)
            {
                // It is an error to use pattern-matching with a null, method group, or lambda
                Error(diagnostics, ErrorCode.ERR_BadIsPatternExpression, operand.Syntax);
                return true;
            }
            else if (!isVar)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                Conversion conversion =
                    operand != null
                    ? this.Conversions.ClassifyConversionForCast(operand, patternType, ref useSiteDiagnostics)
                    : this.Conversions.ClassifyConversionForCast(operandType, patternType, ref useSiteDiagnostics);
                diagnostics.Add(typeSyntax, useSiteDiagnostics);
                switch (conversion.Kind)
                {
                    case ConversionKind.Boxing:
                    case ConversionKind.ExplicitNullable:
                    case ConversionKind.ExplicitReference:
                    case ConversionKind.Identity:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.Unboxing:
                    case ConversionKind.NullLiteral:
                    case ConversionKind.ImplicitNullable:
                        // these are the conversions allowed by a pattern match
                        break;
                    //case ConversionKind.ExplicitNumeric:  // we do not perform numeric conversions of the operand
                    //case ConversionKind.ImplicitConstant:
                    //case ConversionKind.ImplicitNumeric:
                    default:
                        Error(diagnostics, ErrorCode.ERR_NoExplicitConv, typeSyntax, operandType, patternType);
                        return true;
                }
            }

            return false;
        }

        private BoundPattern BindDeclarationPattern(
            DeclarationPatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(operand != null || operandType != (object)null);
            var typeSyntax = node.Type;
            var identifier = node.Identifier;

            bool isVar;
            AliasSymbol aliasOpt;
            TypeSymbol declType = BindType(typeSyntax, diagnostics, out isVar, out aliasOpt);
            if (isVar && operandType != (object)null) declType = operandType;
            if (declType == (object)null)
            {
                Debug.Assert(hasErrors);
                declType = this.CreateErrorType();
            }

            var boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: isVar, type: declType);
            if (IsOperatorErrors(node, operandType, boundDeclType, diagnostics))
            {
                hasErrors = true;
            }
            else
            {
                hasErrors |= CheckValidPatternType(typeSyntax, operand, operandType, declType,
                                                  isVar: isVar, patternTypeWasInSource: true, diagnostics: diagnostics);
            }

            SourceLocalSymbol localSymbol = this.LookupLocal(identifier);

            // In error scenarios with misplaced code, it is possible we can't bind the local declaration.
            // This occurs through the semantic model.  In that case concoct a plausible result.
            if (localSymbol == (object)null)
            {
                localSymbol = SourceLocalSymbol.MakeLocal(
                    ContainingMemberOrLambda,
                    this,
                    RefKind.None,
                    typeSyntax,
                    identifier,
                    LocalDeclarationKind.PatternVariable);
            }

            if (isVar) localSymbol.SetTypeSymbol(operandType);

            // Check for variable declaration errors.
            hasErrors |= this.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

            if (this.ContainingMemberOrLambda.Kind == SymbolKind.Method
                && ((MethodSymbol)this.ContainingMemberOrLambda).IsAsync
                && declType.IsRestrictedType()
                && !hasErrors)
            {
                Error(diagnostics, ErrorCode.ERR_BadSpecialByRefLocal, typeSyntax, declType);
                hasErrors = true;
            }

            DeclareLocalVariable(localSymbol, identifier, declType);
            return new BoundDeclarationPattern(node, localSymbol, boundDeclType, isVar, hasErrors);
        }
    }
}
