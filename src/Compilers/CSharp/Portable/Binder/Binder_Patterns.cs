// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;
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
            var hasErrors = IsOperandErrors(node, expression, diagnostics);
            var pattern = BindPattern(node.Pattern, expression, expression.Type, hasErrors, diagnostics);
            return new BoundIsPatternExpression(node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node), hasErrors);
        }

        private BoundPattern BindPattern(PatternSyntax node, BoundExpression operand, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern((DeclarationPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.ConstantPattern:
                    return BindConstantPattern((ConstantPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.PropertyPattern:
                    return BindPropertyPattern((PropertyPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.RecursivePattern:
                    return BindRecursivePattern((RecursivePatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.WildcardPattern:
                    return new BoundWildcardPattern(node);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundPattern BindRecursivePattern(RecursivePatternSyntax node, BoundExpression operand, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            Error(diagnostics, ErrorCode.ERR_FeatureIsUnimplemented, node, "recursive pattern matching to a user-defined \"is\" operator");
            return new BoundWildcardPattern(node, true);
        }

        private BoundPattern BindPropertyPattern(PropertyPatternSyntax node, BoundExpression operand, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            var type = (NamedTypeSymbol)this.BindType(node.Type, diagnostics);
            hasErrors = hasErrors || CheckValidPatternType(node.Type, operand, operandType, type, false, diagnostics);
            var properties = ArrayBuilder<Symbol>.GetInstance();
            var boundPatterns = BindSubPropertyPatterns(node, properties, type, diagnostics);
            hasErrors |= properties.Count != boundPatterns.Length;
            return new BoundPropertyPattern(node, type, boundPatterns, properties.ToImmutableAndFree(), hasErrors: hasErrors);
        }

        private ImmutableArray<BoundPattern> BindSubPropertyPatterns(PropertyPatternSyntax node, ArrayBuilder<Symbol> properties, TypeSymbol type, DiagnosticBag diagnostics)
        {
            var boundPatternsBuilder = ArrayBuilder<BoundPattern>.GetInstance();
            foreach (var syntax in node.PatternList.SubPatterns)
            {
                var propName = syntax.Left;
                BoundPattern pattern;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                Symbol property = FindPropertyByName(type, propName, ref useSiteDiagnostics);
                if ((object)property != null)
                {
                    bool hasErrors = false;
                    if (property.IsStatic)
                    {
                        Error(diagnostics, ErrorCode.ERR_ObjectProhibited, propName, property);
                        hasErrors = true;
                    }
                    else
                    {
                        diagnostics.Add(node, useSiteDiagnostics);
                    }

                    properties.Add(property);
                    pattern = this.BindPattern(syntax.Pattern, null, property.GetTypeOrReturnType(), hasErrors, diagnostics);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMember, propName, type, propName.Identifier.ValueText);
                    pattern = new BoundWildcardPattern(node, hasErrors: true);
                }

                boundPatternsBuilder.Add(pattern);
            }

            return boundPatternsBuilder.ToImmutableAndFree();
        }

        private Symbol FindPropertyByName(TypeSymbol type, IdentifierNameSyntax name, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var symbols = ArrayBuilder<Symbol>.GetInstance();
            var result = LookupResult.GetInstance();
            this.LookupMembersWithFallback(result, type, name.Identifier.ValueText, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics);

            if (result.IsMultiViable)
            {
                foreach (var symbol in result.Symbols)
                {
                    if (symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Field)
                    {
                        return symbol;
                    }
                }
            }

            return null;
        }

        private BoundPattern BindConstantPattern(ConstantPatternSyntax node, BoundExpression operand, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            var expression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            if (!node.HasErrors && expression.ConstantValue == null)
            {
                diagnostics.Add(ErrorCode.ERR_ConstantExpected, node.Expression.Location);
                hasErrors = true;
            }

            // TODO: check that the constant is valid for the given operand or operandType.
            return new BoundConstantPattern(node, expression, hasErrors);
        }

        private bool CheckValidPatternType(CSharpSyntaxNode typeSyntax, BoundExpression operand, TypeSymbol operandType, TypeSymbol patternType, bool isVar, DiagnosticBag diagnostics)
        {
            if (patternType.IsNullableType() && !isVar)
            {
                // It is an error to use pattern-matching with a nullable type, because you'll never get null. Use the underlying type.
                Error(diagnostics, ErrorCode.ERR_PatternNullableType, typeSyntax, patternType, patternType.GetNullableUnderlyingType());
                return true;
            }
            else if (operand != null && (object)operandType == null && !operand.HasAnyErrors)
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
                    case ConversionKind.ExplicitNumeric: // TODO: we should constrain this to integral?
                    case ConversionKind.ExplicitReference:
                    case ConversionKind.Identity:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.Unboxing:
                    case ConversionKind.NullLiteral:
                    case ConversionKind.ImplicitConstant:
                    case ConversionKind.ImplicitNumeric:
                        // these are the conversions allowed by a pattern match
                        break;
                    default:
                        Error(diagnostics, ErrorCode.ERR_NoExplicitConv, typeSyntax, operandType, patternType);
                        return true;
                }
            }

            return false;
        }

        private BoundPattern BindDeclarationPattern(
            DeclarationPatternSyntax node, BoundExpression operand, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
        {
            Debug.Assert(operand != null || (object)operandType != null);
            var typeSyntax = node.Type;
            var identifier = node.Identifier;

            bool isVar;
            AliasSymbol aliasOpt;
            TypeSymbol declType = BindType(typeSyntax, diagnostics, out isVar, out aliasOpt);
            if (isVar && operandType != null) declType = operandType;
            var boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: isVar, type: declType);
            if (IsOperatorErrors(node, operandType, boundDeclType, diagnostics))
            {
                hasErrors = true;
            }
            else
            {
                hasErrors = CheckValidPatternType(typeSyntax, operand, operandType, declType, isVar, diagnostics);
            }

            SourceLocalSymbol localSymbol = this.LookupLocal(identifier);

            // In error scenarios with misplaced code, it is possible we can't bind the local declaration.
            // This occurs through the semantic model.  In that case concoct a plausible result.
            if ((object)localSymbol == null)
            {
                localSymbol = SourceLocalSymbol.MakeLocal(
                    ContainingMemberOrLambda,
                    this,
                    typeSyntax,
                    identifier,
                    LocalDeclarationKind.PatternVariable);
            }

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
