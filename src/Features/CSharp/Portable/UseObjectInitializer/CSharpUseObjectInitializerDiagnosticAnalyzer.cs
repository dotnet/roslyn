// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseObjectInitializerDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly string Id = IDEDiagnosticIds.UseObjectInitializerDiagnosticId;

        private static readonly DiagnosticDescriptor s_descriptor =
            CreateDescriptor(Id, DiagnosticSeverity.Hidden);

        private static readonly DiagnosticDescriptor s_unnecessaryWithSuggestionDescriptor =
            CreateDescriptor(Id, DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);

        private static readonly DiagnosticDescriptor s_unnecessaryWithoutSuggestionDescriptor =
            CreateDescriptor(Id + "WithoutSuggestion",
                DiagnosticSeverity.Hidden, DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(s_descriptor, s_unnecessaryWithoutSuggestionDescriptor, s_unnecessaryWithSuggestionDescriptor);

        public bool OpenFileOnly(Workspace workspace) => false;

        private static DiagnosticDescriptor CreateDescriptor(string id, DiagnosticSeverity severity, params string[] customTags)
            => new DiagnosticDescriptor(
                id,
                FeaturesResources.Object_initialization_can_be_simplified,
                FeaturesResources.Object_initialization_can_be_simplified,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true,
                customTags: customTags);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferObjectInitializer, LanguageNames.CSharp);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            var objectCreationExpression = (ObjectCreationExpressionSyntax)context.Node;

            var matches = new Analyzer(objectCreationExpression).Analyze();
            if (matches == null)
            {
                return;
            }

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            var severity = option.Notification.Value;
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptor(Id, severity), 
                objectCreationExpression.GetLocation(),
                additionalLocations: locations));

            var syntaxTree = objectCreationExpression.SyntaxTree;

            foreach (var match in matches)
            {
                var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                    match.MemberAccessExpression.SpanStart, match.MemberAccessExpression.OperatorToken.Span.End));

                context.ReportDiagnostic(Diagnostic.Create(
                    s_unnecessaryWithSuggestionDescriptor, location1, additionalLocations: locations));
                context.ReportDiagnostic(Diagnostic.Create(
                    s_unnecessaryWithoutSuggestionDescriptor,
                    match.ExpressionStatement.SemicolonToken.GetLocation(),
                    additionalLocations: locations));
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }


    }

    internal struct Match
    {
        public readonly ExpressionStatementSyntax ExpressionStatement;
        public readonly MemberAccessExpressionSyntax MemberAccessExpression;
        public readonly ExpressionSyntax Initializer;

        public Match(ExpressionStatementSyntax expressionStatement, MemberAccessExpressionSyntax memberAccessExpression, ExpressionSyntax initializer)
        {
            ExpressionStatement = expressionStatement;
            MemberAccessExpression = memberAccessExpression;
            Initializer = initializer;
        }
    }

    internal struct Analyzer
    {
        private readonly ObjectCreationExpressionSyntax _objectCreationExpression;
        private StatementSyntax _containingStatement;
        private SyntaxNodeOrToken _valuePattern;

        public Analyzer(ObjectCreationExpressionSyntax objectCreationExpression) : this()
        {
            _objectCreationExpression = objectCreationExpression;
        }

        internal List<Match> Analyze()
        {
            if (_objectCreationExpression.Initializer != null)
            {
                // Don't bother if this already has an initializer.
                return null;
            }

            if (!TryInitializeVariableDeclarationCase() &&
                !TryInitializeAssignmentCase())
            {
                return null;
            }

            var containingBlock = _containingStatement.Parent as BlockSyntax;
            if (containingBlock == null)
            {
                return null;
            }

            List<Match> matches = null;
            HashSet<string> seenNames = null;
            var statementIndex = containingBlock.Statements.IndexOf(_containingStatement);
            for (var i = statementIndex + 1; i < containingBlock.Statements.Count; i++)
            {
                var expressionStatement = containingBlock.Statements[i] as ExpressionStatementSyntax;
                if (expressionStatement == null)
                {
                    break;
                }

                var assignExpression = expressionStatement.Expression as AssignmentExpressionSyntax;
                if (assignExpression?.Kind() != SyntaxKind.SimpleAssignmentExpression)
                {
                    break;
                }

                var leftMemberAccess = assignExpression.Left as MemberAccessExpressionSyntax;
                if (leftMemberAccess?.Kind() != SyntaxKind.SimpleMemberAccessExpression)
                {
                    break;
                }

                var expression = leftMemberAccess.Expression;
                if (!ValuePatternMatches(expression))
                {
                    break;
                }

                // found a match!
                seenNames = seenNames ?? new HashSet<string>();
                matches = matches ?? new List<Match>();

                // If we see an assignment to the same property/field, we can't convert it
                // to an initializer.
                if (!seenNames.Add(leftMemberAccess.Name.Identifier.ValueText))
                {
                    break;
                }

                matches.Add(new Match(expressionStatement, leftMemberAccess, assignExpression.Right));
            }

            return matches;
        }

        private bool ValuePatternMatches(ExpressionSyntax expression)
        {
            if (_valuePattern.IsToken)
            {
                return expression.IsKind(SyntaxKind.IdentifierName) &&
                    SyntaxFactory.AreEquivalent(
                        _valuePattern.AsToken(),
                        ((IdentifierNameSyntax)expression).Identifier);
            }
            else
            {
                return SyntaxFactory.AreEquivalent(
                    _valuePattern.AsNode(),
                    expression);
            }
        }

        private bool TryInitializeAssignmentCase()
        {
            if (!IsRightSideOfAssignment())
            {
                return false;
            }

            _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<StatementSyntax>();
            _valuePattern = ((AssignmentExpressionSyntax)_objectCreationExpression.Parent).Left;
            return true;
        }

        private bool IsRightSideOfAssignment()
        {
            return _objectCreationExpression.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
                ((AssignmentExpressionSyntax)_objectCreationExpression.Parent).Right == _objectCreationExpression &&
                _objectCreationExpression.Parent.IsParentKind(SyntaxKind.ExpressionStatement);
        }

        private bool TryInitializeVariableDeclarationCase()
        {
            if (!IsVariableDeclarationInitializer())
            {
                return false;
            }

            _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<StatementSyntax>();
            _valuePattern = ((VariableDeclaratorSyntax)_objectCreationExpression.Parent.Parent).Identifier;
            return true;
        }

        private bool IsVariableDeclarationInitializer()
        {
            return
                _objectCreationExpression.IsParentKind(SyntaxKind.EqualsValueClause) &&
                _objectCreationExpression.Parent.IsParentKind(SyntaxKind.VariableDeclarator) &&
                _objectCreationExpression.Parent.Parent.IsParentKind(SyntaxKind.VariableDeclaration) &&
                _objectCreationExpression.Parent.Parent.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement);
        }
    }

}
