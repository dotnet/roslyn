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
            return new BoundIsPatternExpression(
                node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node), hasErrors);
        }

        internal BoundPattern BindPattern(
            PatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern(
                        (DeclarationPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.ConstantPattern:
                    return BindConstantPattern(
                        (ConstantPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.PropertyPattern:
                    return BindPropertyPattern(
                        (PropertyPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.RecursivePattern:
                    return BindRecursivePattern(
                        (RecursivePatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.WildcardPattern:
                    return new BoundWildcardPattern(node);

                default:
                    // PROTOTYPE: Can this occur due to parser error recovery? If so, how to handle?
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundPattern BindRecursivePattern(
            RecursivePatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            var type = (NamedTypeSymbol)this.BindType(node.Type, diagnostics);
            hasErrors = hasErrors || CheckValidPatternType(node.Type, operand, operandType, type, false, diagnostics);

            // PROTOTYPE: We intend that (positional) recursive pattern-matching should be defined in terms of
            // PROTOTYPE: a pattern of user-defined methods or operators. As currently specified it is `operator is`.
            // PROTOTYPE: But for now we try to *infer* a positional pattern-matching operation from the presence of
            // PROTOTYPE: an accessible constructor.
            var correspondingMembers = default(ImmutableArray<Symbol>);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var memberNames = type.MemberNames;
            var correspondingMembersForCtor = ArrayBuilder<Symbol>.GetInstance();
            foreach (var m in type.GetMembers(WellKnownMemberNames.InstanceConstructorName))
            {
                var ctor = m as MethodSymbol;
                if (ctor?.ParameterCount != node.PatternList.SubPatterns.Count) continue;
                if (!IsAccessible(ctor, useSiteDiagnostics: ref useSiteDiagnostics)) continue;
                correspondingMembersForCtor.Clear();
                foreach (var parameter in ctor.Parameters)
                {
                    var name = CaseInsensitiveComparison.ToLower(parameter.Name);
                    Symbol correspondingMember = null;
                    foreach (var memberName in memberNames)
                    {
                        if (!name.Equals(CaseInsensitiveComparison.ToLower(memberName))) continue;
                        var candidate = LookupMatchableMemberInType(type, memberName, ref useSiteDiagnostics);
                        if (candidate?.IsStatic != false) continue;
                        if (candidate.Kind != SymbolKind.Property && candidate.Kind != SymbolKind.Field) continue;
                        if ((candidate as PropertySymbol)?.IsIndexedProperty == true) continue;
                        if (!IsAccessible(candidate, useSiteDiagnostics: ref useSiteDiagnostics)) continue;
                        if (correspondingMember != null)
                        {
                            // We have two candidates for this property. Cannot use the constructor.
                            goto tryAnotherConstructor;
                        }
                        correspondingMember = candidate;
                    }

                    if (correspondingMember == null) goto tryAnotherConstructor;
                    correspondingMembersForCtor.Add(correspondingMember);
                }
                Debug.Assert(correspondingMembersForCtor.Count == node.PatternList.SubPatterns.Count);
                if (correspondingMembers.IsDefault)
                {
                    correspondingMembers = correspondingMembersForCtor.ToImmutable();
                }
                else
                {
                    if (!correspondingMembersForCtor.SequenceEqual(correspondingMembers, (s1, s2) => s1 == s2))
                    {
                        correspondingMembersForCtor.Free();
                        // PROTOTYPE: If we decide to support this, we need a properly i18n'd diagnostic.
                        Error(diagnostics, ErrorCode.ERR_FeatureIsUnimplemented, node,
                            "cannot infer a positional pattern from conflicting constructors");
                        diagnostics.Add(node, useSiteDiagnostics);
                        hasErrors = true;
                        return new BoundWildcardPattern(node, hasErrors);
                    }
                }
                tryAnotherConstructor:;
            }

            if (correspondingMembers == null)
            {
                // PROTOTYPE: If we decide to support this, we need a properly i18n'd diagnostic.
                Error(diagnostics, ErrorCode.ERR_FeatureIsUnimplemented, node,
                    "cannot infer a positional pattern from any accessible constructor");
                diagnostics.Add(node, useSiteDiagnostics);
                correspondingMembersForCtor.Free();
                hasErrors = true;
                return new BoundWildcardPattern(node, hasErrors);
            }

            // Given that we infer a set of properties to match, we record the result as a BoundPropertyPattern.
            // Once we translate recursive (positional) patterns into an invocation of GetValues, or "operator is", we'll
            // use a dedicated bound node for that form.
            var properties = correspondingMembers;
            var boundPatterns = BindRecursiveSubPropertyPatterns(node, properties, type, diagnostics);
            var builder = ArrayBuilder<BoundSubPropertyPattern>.GetInstance(properties.Length);
            for (int i = 0; i < properties.Length; i++)
            {
                var member = new BoundPropertyPatternMember(
                    syntax: node.PatternList.SubPatterns[i],
                    memberSymbol: properties[i],
                    //arguments: ImmutableArray<BoundExpression>.Empty,
                    //argumentNamesOpt: ImmutableArray<string>.Empty,
                    //argumentRefKindsOpt: ImmutableArray<RefKind>.Empty,
                    //expanded: false,
                    //argsToParamsOpt: default(ImmutableArray<int>),
                    resultKind: LookupResultKind.Empty,
                    type: properties[i].GetTypeOrReturnType(),
                    hasErrors: hasErrors);
                builder.Add(new BoundSubPropertyPattern(node.PatternList.SubPatterns[i], member, boundPatterns[i], hasErrors));
            }

            return new BoundPropertyPattern(node, type, builder.ToImmutableAndFree(), hasErrors: hasErrors);
        }

        private ImmutableArray<BoundPattern> BindRecursiveSubPropertyPatterns(
            RecursivePatternSyntax node,
            ImmutableArray<Symbol> properties,
            NamedTypeSymbol type,
            DiagnosticBag diagnostics)
        {
            var boundPatternsBuilder = ArrayBuilder<BoundPattern>.GetInstance();
            for (int i = 0; i < properties.Length; i++)
            {
                var syntax = node.PatternList.SubPatterns[i];
                var property = properties[i];
                var pattern = syntax.Pattern;
                bool hasErrors = false;
                Debug.Assert(!property.IsStatic);
                var boundPattern = this.BindPattern(pattern, null, property.GetTypeOrReturnType(), hasErrors, diagnostics);
                boundPatternsBuilder.Add(boundPattern);
            }

            return boundPatternsBuilder.ToImmutableAndFree();
        }

        private Symbol LookupMatchableMemberInType(
            TypeSymbol operandType,
            string name,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var lookupResult = LookupResult.GetInstance();
            this.LookupMembersInType(
                lookupResult,
                operandType,
                name,
                arity: 0,
                basesBeingResolved: null,
                options: LookupOptions.Default,
                originalBinder: this,
                diagnose: false,
                useSiteDiagnostics: ref useSiteDiagnostics);
            var result = lookupResult.SingleSymbolOrDefault;
            lookupResult.Free();
            return result;
        }

        private BoundPattern BindPropertyPattern(
            PropertyPatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            var type = (NamedTypeSymbol)this.BindType(node.Type, diagnostics);
            hasErrors = hasErrors || CheckValidPatternType(node.Type, operand, operandType, type, false, diagnostics);
            var boundPatterns = BindSubPropertyPatterns(node, type, diagnostics);
            return new BoundPropertyPattern(node, type, boundPatterns, hasErrors: hasErrors);
        }

        private ImmutableArray<BoundSubPropertyPattern> BindSubPropertyPatterns(
            PropertyPatternSyntax node,
            TypeSymbol type,
            DiagnosticBag diagnostics)
        {
            var result = ArrayBuilder<BoundSubPropertyPattern>.GetInstance();
            foreach (var e in node.SubPatterns)
            {
                var syntax = e as IsPatternExpressionSyntax;
                var identifier = syntax?.Expression as IdentifierNameSyntax;
                if (identifier == null) throw ExceptionUtilities.UnexpectedValue(syntax?.Expression.Kind() ?? e.Kind());
                var boundMember = BindPropertyPatternMember(type, identifier, diagnostics);
                var boundPattern = BindPattern(syntax.Pattern, null, boundMember.Type, boundMember.HasErrors, diagnostics);
                result.Add(new BoundSubPropertyPattern(e, boundMember, boundPattern, boundPattern.HasErrors));
            }

            return result.ToImmutableAndFree();
        }

        // returns BadBoundExpression or BoundPropertyPatternMember
        private BoundExpression BindPropertyPatternMember(
            TypeSymbol patternType,
            IdentifierNameSyntax memberName,
            DiagnosticBag diagnostics)
        {
            // TODO: consider refactoring out common code with BindObjectInitializerMember

            BoundImplicitReceiver implicitReceiver = new BoundImplicitReceiver(memberName.Parent, patternType);

            BoundExpression boundMember = BindInstanceMemberAccess(
                node: memberName,
                right: memberName,
                boundLeft: implicitReceiver,
                rightName: memberName.Identifier.ValueText,
                rightArity: 0,
                typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>),
                typeArguments: default(ImmutableArray<TypeSymbol>),
                invoked: false,
                diagnostics: diagnostics);
            LookupResultKind resultKind = boundMember.ResultKind;
            if (boundMember.Kind == BoundKind.PropertyGroup)
            {
                boundMember = BindIndexedPropertyAccess(
                    (BoundPropertyGroup)boundMember, mustHaveAllOptionalParameters: true, diagnostics: diagnostics);
            }

            bool hasErrors = boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;

            // PROTOTYPE: the following may be needed if we end up supporting an indexed property.
            //ImmutableArray<BoundExpression> arguments = ImmutableArray<BoundExpression>.Empty;
            //ImmutableArray<string> argumentNamesOpt = default(ImmutableArray<string>);
            //ImmutableArray<int> argsToParamsOpt = default(ImmutableArray<int>);
            //ImmutableArray<RefKind> argumentRefKindsOpt = default(ImmutableArray<RefKind>);
            //bool expanded = false;

            switch (boundMember.Kind)
            {
                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                    break;

                case BoundKind.IndexerAccess:
                // PROTOTYPE: Should a property pattern be capable of referencing an indexed property?
                // PROTOTYPE: This is an open issue in the language specification
                // PROTOTYPE: See https://github.com/dotnet/roslyn/issues/9375
                //{
                //    var indexer = (BoundIndexerAccess)boundMember;
                //    arguments = indexer.Arguments;
                //    argumentNamesOpt = indexer.ArgumentNamesOpt;
                //    argsToParamsOpt = indexer.ArgsToParamsOpt;
                //    argumentRefKindsOpt = indexer.ArgumentRefKindsOpt;
                //    expanded = indexer.Expanded;
                //    break;
                //}

                case BoundKind.DynamicIndexerAccess:
                // PROTOTYPE: Should a property pattern be capable of referencing a dynamic indexer?
                // PROTOTYPE: This is an open issue in the language specification
                // PROTOTYPE: See https://github.com/dotnet/roslyn/issues/9375
                //{
                //    var indexer = (BoundDynamicIndexerAccess)boundMember;
                //    arguments = indexer.Arguments;
                //    argumentNamesOpt = indexer.ArgumentNamesOpt;
                //    argumentRefKindsOpt = indexer.ArgumentRefKindsOpt;
                //    break;
                //}

                case BoundKind.EventAccess:
                // PROTOTYPE: Should a property pattern be capable of referencing an event?
                // PROTOTYPE: This is an open issue in the language specification
                // PROTOTYPE: See https://github.com/dotnet/roslyn/issues/9515

                default:
                    return BadSubpatternMemberAccess(boundMember, implicitReceiver, memberName, diagnostics, hasErrors);
            }

            if (!hasErrors && !CheckValueKind(boundMember, BindValueKind.RValue, diagnostics))
            {
                hasErrors = true;
                resultKind = LookupResultKind.NotAValue;
            }

            return new BoundPropertyPatternMember(
                memberName,
                boundMember.ExpressionSymbol,
                //arguments,
                //argumentNamesOpt,
                //argumentRefKindsOpt,
                //expanded,
                //argsToParamsOpt,
                resultKind,
                boundMember.Type,
                hasErrors);
        }

        private BoundExpression BadSubpatternMemberAccess(
            BoundExpression boundMember,
            BoundImplicitReceiver implicitReceiver,
            IdentifierNameSyntax memberName,
            DiagnosticBag diagnostics,
            bool suppressErrors)
        {
            if (!suppressErrors)
            {
                string member = memberName.Identifier.ValueText;
                switch (boundMember.ResultKind)
                {
                    case LookupResultKind.Empty:
                        Error(diagnostics, ErrorCode.ERR_NoSuchMember, memberName, implicitReceiver.Type, member);
                        break;

                    case LookupResultKind.Inaccessible:
                        boundMember = CheckValue(boundMember, BindValueKind.RValue, diagnostics);
                        Debug.Assert(boundMember.HasAnyErrors);
                        break;

                    default:
                        // PROTOTYPE: We need to review and test this code path.
                        // https://github.com/dotnet/roslyn/issues/9542
                        Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, memberName, member);
                        break;
                }
            }

            // PROTOTYPE: review and test this code path. Is LookupResultKind.Inaccessible appropriate?
            // https://github.com/dotnet/roslyn/issues/9542
            return ToBadExpression(boundMember, LookupResultKind.Inaccessible);
        }

        private BoundPattern BindConstantPattern(
            ConstantPatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            var expression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            if (!node.HasErrors && expression.ConstantValue == null)
            {
                diagnostics.Add(ErrorCode.ERR_ConstantExpected, node.Expression.Location);
                hasErrors = true;
            }

            // PROTOTYPE: we still need to check that the constant is valid for the given operand or operandType.
            return new BoundConstantPattern(node, expression, hasErrors);
        }

        private bool CheckValidPatternType(
            CSharpSyntaxNode typeSyntax,
            BoundExpression operand,
            TypeSymbol operandType,
            TypeSymbol patternType,
            bool isVar,
            DiagnosticBag diagnostics)
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
                    case ConversionKind.ExplicitNumeric: // PROTOTYPE: we should constrain this to integral? Need LDM decision
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
            DeclarationPatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
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

        private TypeSymbol BestType(
            MatchExpressionSyntax node,
            ArrayBuilder<BoundMatchCase> cases,
            DiagnosticBag diagnostics)
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

        private BoundExpression BindMatchExpression(
            MatchExpressionSyntax node,
            DiagnosticBag diagnostics)
        {
            var expression = BindValue(node.Left, diagnostics, BindValueKind.RValue);
            // PROTOTYPE: any constraints on a switch expression must be enforced here. For example,
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
                    case SyntaxKind.ConditionalExpression: // ?:
                        {
                            var papa = (ConditionalExpressionSyntax)node.Parent;
                            if (node == papa.WhenTrue || node == papa.WhenFalse) goto syntaxOk;
                            break;
                        }
                    case SyntaxKind.CoalesceExpression: // ??
                        {
                            var papa = (BinaryExpressionSyntax)node.Parent;
                            if (node == papa.Right) goto syntaxOk;
                            break;
                        }
                    case SyntaxKind.MatchSection: // match
                        {
                            var papa = (MatchSectionSyntax)node.Parent;
                            if (node == papa.Expression) goto syntaxOk;
                            break;
                        }
                    case SyntaxKind.ArrowExpressionClause: // =>
                        {
                            var papa = (ArrowExpressionClauseSyntax)node.Parent;
                            if (node == papa.Expression) goto syntaxOk;
                            break;
                        }
                    // PROTOTYPE: It has been suggested that we allow throw on the right of && and ||, but
                    // PROTOTYPE: due to the relative precedence the parser already rejects that.
                    //case SyntaxKind.LogicalAndExpression: // &&
                    //case SyntaxKind.LogicalOrExpression: // ||
                    //    {
                    //        // PROTOTYPE: it isn't clear what the semantics should be
                    //        var papa = (BinaryExpressionSyntax)node.Parent;
                    //        if (node == papa.Right) goto syntaxOk;
                    //        break;
                    //    }
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
                expression = new BoundBadExpression(
                    syntax: node.Expression,
                    resultKind: LookupResultKind.Viable,
                    symbols: ImmutableArray<Symbol>.Empty,
                    childBoundNodes: ImmutableArray.Create<BoundNode>(expression),
                    type: CreateErrorType());
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

                pattern = new BoundDeclarationPattern(
                    node, localSymbol, null, true,
                    // Check for variable declaration errors.
                    expression.HasErrors | this.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics));
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
