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

                case SyntaxKind.PositionalPattern:
                    return BindPositionalPattern(
                        (PositionalPatternSyntax)node, operand, operandType, hasErrors, diagnostics);

                case SyntaxKind.WildcardPattern:
                    return new BoundWildcardPattern(node);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundPattern BindPositionalPattern(
            PositionalPatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            var type = (NamedTypeSymbol)this.BindType(node.Type, diagnostics);

            var operators = type.GetMembers(WellKnownMemberNames.IsOperatorName);
            if (operators.IsDefaultOrEmpty && ((CSharpParseOptions)node.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeatureInferPositionalPattern))
            {
                return BindInferredPositionalPattern(node, type, operand, operandType, hasErrors, diagnostics);
            }
            else
            {
                return BindUserDefinedPositionalPattern(node, type, operators, operand, operandType, hasErrors, diagnostics);
            }
        }

        private BoundPattern BindUserDefinedPositionalPattern(
            PositionalPatternSyntax node,
            NamedTypeSymbol type,
            ImmutableArray<Symbol> operators,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            // find the unique operator to use
            MethodSymbol isOperator = null;
            var subPatterns = node.PatternList.SubPatterns;
            var subPatternsCount = subPatterns.Count;
            foreach (var op in operators)
            {
                // check that the operator is well-formed. If not, skip it.
                var candidate = op as MethodSymbol;
                if (candidate == (object)null || !ApplicableOperatorIs(candidate, node.Type, diagnostics)) continue;

                // check that its number of out parameters is the same as the arity of this pattern-matching operation.
                if (candidate.ParameterCount != subPatternsCount + 1) continue; // not the droid you're looking for

                // ok, we've found a candidate. If we already have a candidate, error.
                if (isOperator != (object)null)
                {
                    // Error: Ambiguous `operator is` declarations found in type {type}
                    diagnostics.Add(ErrorCode.ERR_AmbiguousOperatorIs, node.Location, type);
                    hasErrors = true;
                    break;
                }

                isOperator = candidate;
            }

            if (isOperator == (object)null)
            {
                // Error: No 'operator is' declaration in {type} was found with {node.PatternList.SubPatterns.Count} out parameters.
                diagnostics.Add(ErrorCode.ERR_OperatorIsParameterCount, node.Location, type, subPatternsCount);
                return new BoundWildcardPattern(node, hasErrors: true);
            }

            // Note: we bind the pattern (conversion) to the first argument of `operator is`, not the type containing `operator is`.
            // That is how we support the specification of pattern-matching external to a type (e.g. active patterns)
            hasErrors = hasErrors ||
                CheckValidPatternType(node.Type, operand, operandType, isOperator.Parameters[0].Type, isVar: false,
                                      patternTypeWasInSource: false, diagnostics: diagnostics);
            var patterns = ArrayBuilder<BoundPattern>.GetInstance(subPatternsCount);
            for (int i = 0; i < subPatternsCount; i++)
            {
                if (subPatterns[i].NameColon != null)
                {
                    // Error: names argument syntax not supported in positional patterns.
                    diagnostics.Add(ErrorCode.ERR_FeatureIsUnimplemented, subPatterns[i].NameColon.Location, "named argument syntax in positional patterns");
                    hasErrors = true;
                }

                patterns.Add(BindPattern(subPatterns[i].Pattern, null, isOperator.Parameters[i + 1].Type, hasErrors, diagnostics));
            }

            return new BoundPositionalPattern(node, type, isOperator, patterns.ToImmutableAndFree(), hasErrors);
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

        private BoundPattern BindInferredPositionalPattern(
            PositionalPatternSyntax node,
            NamedTypeSymbol type,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            hasErrors = hasErrors ||
                CheckValidPatternType(node.Type, operand, operandType, type,
                                      isVar: false, patternTypeWasInSource:false, diagnostics: diagnostics);
            var correspondingMembers = default(ImmutableArray<Symbol>);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var memberNames = type.MemberNames;
            var correspondingMembersForCtor = ArrayBuilder<Symbol>.GetInstance(node.PatternList.SubPatterns.Count);
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
                Error(diagnostics, ErrorCode.ERR_FeatureIsUnimplemented, node,
                    "cannot infer a positional pattern from any accessible constructor");
                diagnostics.Add(node, useSiteDiagnostics);
                correspondingMembersForCtor.Free();
                hasErrors = true;
                return new BoundWildcardPattern(node, hasErrors);
            }

            var properties = correspondingMembers;
            var boundPatterns = BindPositionalSubPropertyPatterns(node, properties, type, diagnostics);
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

        private ImmutableArray<BoundPattern> BindPositionalSubPropertyPatterns(
            PositionalPatternSyntax node,
            ImmutableArray<Symbol> properties,
            NamedTypeSymbol type,
            DiagnosticBag diagnostics)
        {
            var boundPatternsBuilder = ArrayBuilder<BoundPattern>.GetInstance(properties.Length);
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
            var type = this.BindType(node.Type, diagnostics);
            hasErrors = hasErrors ||
                CheckValidPatternType(node.Type, operand, operandType, type,
                                      isVar: false, patternTypeWasInSource: false, diagnostics: diagnostics);
            var boundPatterns = BindSubPropertyPatterns(node, type, diagnostics);
            return new BoundPropertyPattern(node, type, boundPatterns, hasErrors: hasErrors);
        }

        private ImmutableArray<BoundSubPropertyPattern> BindSubPropertyPatterns(
            PropertyPatternSyntax node,
            TypeSymbol type,
            DiagnosticBag diagnostics)
        {
            var result = ArrayBuilder<BoundSubPropertyPattern>.GetInstance(node.SubPatterns.Count);
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

            switch (boundMember.Kind)
            {
                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                    break;

                case BoundKind.IndexerAccess:
                case BoundKind.DynamicIndexerAccess:
                case BoundKind.EventAccess:
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
                        Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, memberName, member);
                        break;
                }
            }

            return ToBadExpression(boundMember, LookupResultKind.Inaccessible);
        }

        private BoundPattern BindConstantPattern(
            ConstantPatternSyntax node,
            BoundExpression operand,
            TypeSymbol operandType,
            bool hasErrors,
            DiagnosticBag diagnostics)
        {
            bool wasExpression;
            return BindConstantPattern(node, operand, operandType, node.Expression, hasErrors, diagnostics, out wasExpression);
        }

        private BoundPattern BindConstantPattern(
            CSharpSyntaxNode node,
            BoundExpression left,
            TypeSymbol leftType,
            ExpressionSyntax right,
            bool hasErrors,
            DiagnosticBag diagnostics,
            out bool wasExpression)
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
                hasErrors = CheckValidPatternType(typeSyntax, operand, operandType, declType,
                                                  isVar: isVar, patternTypeWasInSource: true, diagnostics: diagnostics);
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
            int n = cases.Count;
            var types = ArrayBuilder<TypeSymbol>.GetInstance(n);
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
            var sectionBuilder = ArrayBuilder<BoundMatchCase>.GetInstance(node.Sections.Count);
            foreach (var section in node.Sections)
            {
                var sectionBinder = this.GetBinder(section); // each section has its own locals.
                Debug.Assert(sectionBinder != null);

                var pattern = sectionBinder.BindPattern(section.Pattern, expression, expression.Type, section.HasErrors, diagnostics);
                var guard = (section.WhenClause != null) ? sectionBinder.BindBooleanExpression(section.WhenClause.Condition, diagnostics) : null;
                var e = sectionBinder.BindExpression(section.Expression, diagnostics);
                sectionBuilder.Add(new BoundMatchCase(section, sectionBinder.GetDeclaredLocalsForScope(section), pattern, guard, e, section.HasErrors));
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
                     // We do not support && and || because
                     // 1. The precedence would not syntactically allow it
                     // 2. It isn't clear what the semantics should be
                     // 3. It isn't clear what use cases would motivate us to change the precedence to support it
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
            var letBinder = this.GetBinder(node);
            Debug.Assert(letBinder != null);
            return letBinder.WrapWithVariablesIfAny(node, letBinder.BindLetStatementParts(node, diagnostics));
        }

        private BoundStatement BindLetStatementParts(LetStatementSyntax node, DiagnosticBag diagnostics)
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
