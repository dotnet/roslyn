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

        internal BoundPattern BindPattern(PatternSyntax node, BoundExpression operand, TypeSymbol operandType, bool hasErrors, DiagnosticBag diagnostics)
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
            if (operandType?.IsErrorType() == true || patternType?.IsErrorType() == true)
            {
                return false;
            }
            else if (patternType.IsNullableType() && !isVar)
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

        private TypeSymbol BestType(MatchExpressionSyntax node, ArrayBuilder<BoundMatchCase> cases, DiagnosticBag diagnostics)
        {
            var types = ArrayBuilder<TypeSymbol>.GetInstance();

            int n = cases.Count;
            for (int i = 0; i < n; i++)
            {
                var e = cases[i].Expression;
                if (e.Type != null && !types.Contains(e.Type)) types.Add(e.Type);
            }

            var allTypes = types.ToImmutableAndFree();

            TypeSymbol bestType;
            if (allTypes.IsDefaultOrEmpty)
            {
                diagnostics.Add(ErrorCode.ERR_AmbigMatch0, node.MatchToken.GetLocation());
                bestType = CreateErrorType();
            }
            else if (allTypes.Length == 1)
            {
                bestType = allTypes[0];
            }
            else
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                bestType = BestTypeInferrer.InferBestType(
                    allTypes,
                    Conversions,
                    ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
                if ((object)bestType == null)
                {
                    diagnostics.Add(ErrorCode.ERR_AmbigMatch1, node.MatchToken.GetLocation());
                    bestType = CreateErrorType();
                }
            }

            for (int i = 0; i < n; i++)
            {
                var c = cases[i];
                var e = c.Expression;
                var converted = GenerateConversionForAssignment(bestType, e, diagnostics);
                if (e != converted)
                {
                    cases[i] = new BoundMatchCase(c.Syntax, c.Locals, c.Pattern, c.Guard, converted);
                }
            }

            return bestType;
        }

        private BoundExpression BindMatchExpression(MatchExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var expression = BindValue(node.Left, diagnostics, BindValueKind.RValue);
            // TODO: any constraints on a switch expression must be enforced here. For example,
            // it must have a type (not be target-typed, lambda, null, etc)

            var sectionBuilder = ArrayBuilder<BoundMatchCase>.GetInstance();
            foreach (var section in node.Sections)
            {
                var sectionBinder = new PatternVariableBinder(section, this); // each section has its own locals.
                var pattern = sectionBinder.BindPattern(section.Pattern, expression, expression.Type, section.HasErrors, diagnostics);
                var guard = (section.WhenClause != null) ? sectionBinder.BindBooleanExpression(section.WhenClause.Condition, diagnostics) : null;
                var e = sectionBinder.BindExpression(section.Expression, diagnostics);
                sectionBuilder.Add(new BoundMatchCase(section, sectionBinder.Locals, pattern, guard, e, section.HasErrors));
            }

            var resultType = BestType(node, sectionBuilder, diagnostics);
            return new BoundMatchExpression(node, expression, sectionBuilder.ToImmutableAndFree(), resultType);
        }

        private BoundExpression BindThrowExpression(ThrowExpressionSyntax node, DiagnosticBag diagnostics)
        {
            bool hasErrors = false;
            if (node.Parent != null && !node.HasErrors)
            {
                switch (node.Parent.Kind())
                {
                    case SyntaxKind.ConditionalExpression:
                        {
                            var papa = (ConditionalExpressionSyntax)node.Parent;
                            if (node == papa.WhenTrue || node == papa.WhenFalse) goto syntaxOk;
                            break;
                        }
                    case SyntaxKind.CoalesceExpression:
                        {
                            var papa = (BinaryExpressionSyntax)node.Parent;
                            if (node == papa.Right) goto syntaxOk;
                            break;
                        }
                    case SyntaxKind.MatchSection:
                        {
                            var papa = (MatchSectionSyntax)node.Parent;
                            if (node == papa.Expression) goto syntaxOk;
                            break;
                        }
                    case SyntaxKind.ArrowExpressionClause:
                        {
                            var papa = (ArrowExpressionClauseSyntax)node.Parent;
                            if (node == papa.Expression) goto syntaxOk;
                            break;
                        }
                    default:
                        break;
                }

                diagnostics.Add(ErrorCode.ERR_ThrowMisplaced, node.ThrowKeyword.GetLocation());
                hasErrors = true;
                syntaxOk:;
            }

            var thrownExpression = BindThrownExpression(node.Expression, diagnostics, ref hasErrors);
            return new BoundThrowExpression(node, thrownExpression, null, hasErrors);
        }

        private BoundStatement BindLetStatement(LetStatementSyntax node, DiagnosticBag diagnostics)
        {
            var expression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            // TODO: any constraints on the expression must be enforced here. For example,
            // it must have a type (not be target-typed, lambda, null, etc)
            var hasErrors = IsOperandErrors(node.Expression, expression, diagnostics);
            if (!hasErrors && expression.IsLiteralNull())
            {
                diagnostics.Add(ErrorCode.ERR_NullNotValid, node.Expression.Location);
                hasErrors = true;
            }
            if (hasErrors && expression.Type == (object)null)
            {
                expression = new BoundBadExpression(node.Expression, LookupResultKind.Viable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(expression), CreateErrorType());
            }

            BoundPattern pattern;
            if (node.Pattern == null)
            {
                SourceLocalSymbol localSymbol = this.LookupLocal(node.Identifier);

                // In error scenarios with misplaced code, it is possible we can't bind the local.
                // This occurs through the semantic model.  In that case concoct a plausible result.
                if ((object)localSymbol == null)
                {
                    localSymbol = SourceLocalSymbol.MakeLocal(
                        ContainingMemberOrLambda,
                        this,
                        RefKind.None,
                        null,
                        node.Identifier,
                        LocalDeclarationKind.PatternVariable,
                        null);
                }

                localSymbol.SetTypeSymbol(expression.Type);
                pattern = new BoundDeclarationPattern(node, localSymbol, null, true, expression.HasErrors);
            }
            else
            {
                pattern = BindPattern(node.Pattern, expression, expression?.Type, expression.HasErrors, diagnostics);
            }

            var guard = (node.WhenClause != null) ? BindBooleanExpression(node.WhenClause.Condition, diagnostics) : null;
            var elseClause = (node.ElseClause != null) ? BindPossibleEmbeddedStatement(node.ElseClause.Statement, diagnostics) : null;

            // If a guard is present, an else clause is required
            if (guard != null && elseClause == null)
            {
                diagnostics.Add(ErrorCode.ERR_ElseClauseRequiredWithWhenClause, node.WhenClause.WhenKeyword.GetLocation());
            }

            return new BoundLetStatement(node, pattern, expression, guard, elseClause, hasErrors);
        }
    }
}
