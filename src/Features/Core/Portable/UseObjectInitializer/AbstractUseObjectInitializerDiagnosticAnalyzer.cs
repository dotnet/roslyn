// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;
using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.UseObjectInitializer
{
    internal abstract class AbstractUseObjectInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TVariableDeclarator>
        : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
        where TVariableDeclarator : SyntaxNode
    {
        protected abstract bool FadeOutOperatorToken { get; }

        public bool OpenFileOnly(Workspace workspace) => false;

        protected AbstractUseObjectInitializerDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
                  new LocalizableResourceString(nameof(FeaturesResources.Simplify_object_initialization), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Object_initialization_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, GetObjectCreationSyntaxKind());

        protected abstract TSyntaxKind GetObjectCreationSyntaxKind();

        protected abstract bool AreObjectInitializersSupported(SyntaxNodeAnalysisContext context);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!AreObjectInitializersSupported(context))
            {
                return;
            }

            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;

            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferObjectInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();
            var analyzer = new Analyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax, TVariableDeclarator>(
                syntaxFacts,
                objectCreationExpression);
            var matches = analyzer.Analyze();
            if (matches.Length == 0)
            {
                return;
            }

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            var severity = option.Notification.Value;
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity(severity),
                objectCreationExpression.GetLocation(),
                additionalLocations: locations));

            FadeOutCode(context, optionSet, matches, locations);
        }

        private void FadeOutCode(
            SyntaxNodeAnalysisContext context,
            OptionSet optionSet,
            ImmutableArray<Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>> matches,
            ImmutableArray<Location> locations)
        {
            var syntaxTree = context.Node.SyntaxTree;

            var fadeOutCode = optionSet.GetOption(
                CodeStyleOptions.PreferObjectInitializer_FadeOutCode, context.Node.Language);
            if (!fadeOutCode)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();

            foreach (var match in matches)
            {
                var end = this.FadeOutOperatorToken
                    ? syntaxFacts.GetOperatorTokenOfMemberAccessExpression(match.MemberAccessExpression).Span.End
                    : syntaxFacts.GetExpressionOfMemberAccessExpression(match.MemberAccessExpression).Span.End;

                var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                    match.MemberAccessExpression.SpanStart, end));

                context.ReportDiagnostic(Diagnostic.Create(
                    UnnecessaryWithSuggestionDescriptor, location1, additionalLocations: locations));

                if (match.Statement.Span.End > match.Initializer.FullSpan.End)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithoutSuggestionDescriptor,
                        Location.Create(syntaxTree, TextSpan.FromBounds(
                            match.Initializer.FullSpan.End,
                            match.Statement.Span.End)),
                        additionalLocations: locations));
                }
            }
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }
    }

    internal struct Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : SyntaxNode
    {
        public readonly TAssignmentStatementSyntax Statement;
        public readonly TMemberAccessExpressionSyntax MemberAccessExpression;
        public readonly TExpressionSyntax Initializer;

        public Match(
            TAssignmentStatementSyntax statement,
            TMemberAccessExpressionSyntax memberAccessExpression,
            TExpressionSyntax initializer)
        {
            Statement = statement;
            MemberAccessExpression = memberAccessExpression;
            Initializer = initializer;
        }
    }

    internal struct Analyzer<
            TExpressionSyntax,
            TStatementSyntax, 
            TObjectCreationExpressionSyntax, 
            TMemberAccessExpressionSyntax,
            TAssignmentStatementSyntax,
            TVariableDeclaratorSyntax>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly TObjectCreationExpressionSyntax _objectCreationExpression;

        private TStatementSyntax _containingStatement;
        private SyntaxNodeOrToken _valuePattern;

        public Analyzer(
            ISyntaxFactsService syntaxFacts, 
            TObjectCreationExpressionSyntax objectCreationExpression) : this()
        {
            _syntaxFacts = syntaxFacts;
            _objectCreationExpression = objectCreationExpression;
        }

        internal ImmutableArray<Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>> Analyze()
        {
            if (_syntaxFacts.GetObjectCreationInitializer(_objectCreationExpression) != null)
            {
                // Don't bother if this already has an initializer.
                return ImmutableArray<Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>>.Empty;
            }

            _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (_containingStatement == null)
            {
                return ImmutableArray<Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>>.Empty;
            }

            if (!TryInitializeVariableDeclarationCase() &&
                !TryInitializeAssignmentCase())
            {
                return ImmutableArray<Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>>.Empty;
            }

            var containingBlock = _containingStatement.Parent;
            var foundStatement = false;

            var matches = ArrayBuilder<Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>>.GetInstance();
            HashSet<string> seenNames = null;

            foreach (var child in containingBlock.ChildNodesAndTokens())
            {
                if (!foundStatement)
                {
                    if (child == _containingStatement)
                    {
                        foundStatement = true;
                    }

                    continue;
                }

                if (child.IsToken)
                {
                    break;
                }

                var statement = child.AsNode() as TAssignmentStatementSyntax;
                if (statement == null)
                {
                    break;
                }

                if (!_syntaxFacts.IsSimpleAssignmentStatement(statement))
                {
                    break;
                }

                _syntaxFacts.GetPartsOfAssignmentStatement(
                    statement, out var left, out var right);

                var rightExpression = right as TExpressionSyntax;
                var leftMemberAccess = left as TMemberAccessExpressionSyntax;
                if (!_syntaxFacts.IsSimpleMemberAccessExpression(leftMemberAccess))
                {
                    break;
                }

                var expression = (TExpressionSyntax)_syntaxFacts.GetExpressionOfMemberAccessExpression(leftMemberAccess);
                if (!ValuePatternMatches(expression))
                {
                    break;
                }

                // Don't offer this fix if the value we're initializing is itself referenced
                // on the RHS of the assignment.  For example:
                //
                //      var v = new X();
                //      v.Prop = v.Prop.WithSomething();
                //
                // Or with
                //
                //      v = new X();
                //      v.Prop = v.Prop.WithSomething();
                //
                // In the first case, 'v' is being initialized, and so will not be available 
                // in the object initializer we create.
                // 
                // In the second case we'd change semantics because we'd access the old value 
                // before the new value got written.
                if (ExpressionContainsValuePattern(rightExpression))
                {
                    break;
                }

                // found a match!
                seenNames = seenNames ?? new HashSet<string>();

                // If we see an assignment to the same property/field, we can't convert it
                // to an initializer.
                var name = _syntaxFacts.GetNameOfMemberAccessExpression(leftMemberAccess);
                var identifier = _syntaxFacts.GetIdentifierOfSimpleName(name);
                if (!seenNames.Add(identifier.ValueText))
                {
                    break;
                }

                matches.Add(new Match<TAssignmentStatementSyntax, TMemberAccessExpressionSyntax, TExpressionSyntax>(
                    statement, leftMemberAccess, rightExpression));
            }

            return matches.ToImmutableAndFree();
        }

        private bool ExpressionContainsValuePattern(TExpressionSyntax expression)
        {
            foreach (var subExpression in expression.DescendantNodesAndSelf().OfType<TExpressionSyntax>())
            {
                if (!_syntaxFacts.IsNameOfMemberAccessExpression(subExpression))
                {
                    if (ValuePatternMatches(subExpression))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ValuePatternMatches(TExpressionSyntax expression)
        {
            if (_valuePattern.IsToken)
            {
                return _syntaxFacts.IsIdentifierName(expression) &&
                    _syntaxFacts.AreEquivalent(
                        _valuePattern.AsToken(),
                        _syntaxFacts.GetIdentifierOfSimpleName(expression));
            }
            else
            {
                return _syntaxFacts.AreEquivalent(
                    _valuePattern.AsNode(), expression);
            }
        }

        private bool TryInitializeAssignmentCase()
        {
            if (!_syntaxFacts.IsSimpleAssignmentStatement(_containingStatement))
            {
                return false;
            }

            _syntaxFacts.GetPartsOfAssignmentStatement(
                _containingStatement, out var left, out var right);
            if (right != _objectCreationExpression)
            {
                return false;
            }

            _valuePattern = left;
            return true;
        }

        private bool TryInitializeVariableDeclarationCase()
        {
            if (!_syntaxFacts.IsLocalDeclarationStatement(_containingStatement))
            {
                return false;
            }

            var containingDeclarator = _objectCreationExpression.FirstAncestorOrSelf<TVariableDeclaratorSyntax>();
            if (containingDeclarator == null)
            {
                return false;
            }

            if (!_syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(containingDeclarator, _containingStatement))
            {
                return false;
            }

            _valuePattern = _syntaxFacts.GetIdentifierOfVariableDeclarator(containingDeclarator);
            return true;
        }
    }
}