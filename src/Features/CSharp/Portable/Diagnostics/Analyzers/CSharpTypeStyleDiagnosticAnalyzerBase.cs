// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly string _diagnosticId;
        private readonly LocalizableString _title;
        private readonly LocalizableString _message;
        private readonly DiagnosticDescriptor _noneDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _infoDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _warningDiagnosticDescriptor;
        private readonly DiagnosticDescriptor _errorDiagnosticDescriptor;
        private readonly Dictionary<DiagnosticSeverity, DiagnosticDescriptor> _severityToDescriptorMap;

        public CSharpTypeStyleDiagnosticAnalyzerBase(string diagnosticId, LocalizableString title, LocalizableString message)
        {
            _diagnosticId = diagnosticId;
            _title = title;
            _message = message;
            _noneDiagnosticDescriptor = CreateDiagnosticDescriptor(DiagnosticSeverity.Hidden);
            _infoDiagnosticDescriptor = CreateDiagnosticDescriptor(DiagnosticSeverity.Info);
            _warningDiagnosticDescriptor = CreateDiagnosticDescriptor(DiagnosticSeverity.Warning);
            _errorDiagnosticDescriptor = CreateDiagnosticDescriptor(DiagnosticSeverity.Error);
            _severityToDescriptorMap =
                new Dictionary<DiagnosticSeverity, DiagnosticDescriptor>
                {
                    {DiagnosticSeverity.Hidden, _noneDiagnosticDescriptor },
                    {DiagnosticSeverity.Info, _infoDiagnosticDescriptor },
                    {DiagnosticSeverity.Warning, _warningDiagnosticDescriptor },
                    {DiagnosticSeverity.Error, _errorDiagnosticDescriptor },
                };
        }

        private DiagnosticDescriptor CreateDiagnosticDescriptor(DiagnosticSeverity severity) =>
            new DiagnosticDescriptor(
                _diagnosticId,
                _title,
                _message,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(_noneDiagnosticDescriptor, _infoDiagnosticDescriptor,
                                  _warningDiagnosticDescriptor, _errorDiagnosticDescriptor);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() =>
            DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement);
        }

        protected abstract bool IsStylePreferred(SemanticModel semanticModel, OptionSet optionSet, State state, CancellationToken cancellationToken);
        protected abstract bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan);
        protected abstract bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, EqualsValueClauseSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        protected static ExpressionSyntax GetInitializerExpression(EqualsValueClauseSyntax initializer) =>
            initializer.Value is CheckedExpressionSyntax
                ? ((CheckedExpressionSyntax)initializer.Value).Expression.WalkDownParentheses()
                : initializer.Value.WalkDownParentheses();

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            TypeSyntax declaredType;
            State state = null;
            var shouldAnalyze = false;
            var declarationStatement = context.Node;
            var optionSet = GetOptionSet(context.Options);
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            if (declarationStatement.IsKind(SyntaxKind.VariableDeclaration))
            {
                var declaration = (VariableDeclarationSyntax)declarationStatement;
                declaredType = declaration.Type;

                shouldAnalyze = ShouldAnalyzeVariableDeclaration(declaration, semanticModel, cancellationToken);

                if (shouldAnalyze)
                {
                    state = State.Generate(declarationStatement, semanticModel, optionSet, isVariableDeclarationContext: true, cancellationToken: cancellationToken);
                    shouldAnalyze = IsStylePreferred(semanticModel, optionSet, state, cancellationToken);
                }
            }
            else if (declarationStatement.IsKind(SyntaxKind.ForEachStatement))
            {
                var declaration = (ForEachStatementSyntax)declarationStatement;
                declaredType = declaration.Type;

                state = State.Generate(declarationStatement, semanticModel, optionSet, isVariableDeclarationContext: false, cancellationToken: cancellationToken);
                shouldAnalyze = IsStylePreferred(semanticModel, optionSet, state, cancellationToken);
            }
            else
            {
                Debug.Assert(false, $"called in for unregistered node kind {declarationStatement.Kind().ToString()}");
                return;
            }

            if (shouldAnalyze)
            {
                Debug.Assert(state != null, "analyzing a declaration and state is null.");

                TextSpan diagnosticSpan;

                if (TryAnalyzeVariableDeclaration(declaredType, semanticModel, optionSet, cancellationToken, out diagnosticSpan))
                {
                    var descriptor = _severityToDescriptorMap[state.GetDiagnosticSeverityPreference()];
                    context.ReportDiagnostic(CreateDiagnostic(descriptor, declarationStatement, diagnosticSpan));
                }
            }
        }

        private Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan) =>
            Diagnostic.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan));

        private bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // implict type is applicable only for local variables and
            // such declarations cannot have multiple declarators and
            // must have an initializer.
            var isSupportedParentKind = variableDeclaration.IsParentKind(
                    SyntaxKind.LocalDeclarationStatement,
                    SyntaxKind.ForStatement,
                    SyntaxKind.UsingStatement);

            return isSupportedParentKind &&
                   variableDeclaration.Variables.Count == 1 &&
                   variableDeclaration.Variables.Single().Initializer.IsKind(SyntaxKind.EqualsValueClause);
        }

        private OptionSet GetOptionSet(AnalyzerOptions analyzerOptions)
        {
            var workspaceOptions = analyzerOptions as WorkspaceAnalyzerOptions;
            if (workspaceOptions != null)
            {
                return workspaceOptions.Workspace.Options;
            }

            return null;
        }
    }
}
