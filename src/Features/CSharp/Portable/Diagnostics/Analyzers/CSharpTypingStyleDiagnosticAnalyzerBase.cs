// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypingStyles
{
    internal abstract partial class CSharpTypingStyleDiagnosticAnalyzerBase : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        [Flags]
        internal enum TypingStyles
        {
            None = 0,
            VarForIntrinsic = 1 << 0,
            VarWhereApparent = 1 << 1,
            VarWherePossible = 1 << 2,
        }

        private readonly string _diagnosticId;
        private readonly LocalizableString _title;
        private readonly LocalizableString _message;
        private readonly Lazy<DiagnosticDescriptor> _noneDiagnosticDescriptor;
        private readonly Lazy<DiagnosticDescriptor> _infoDiagnosticDescriptor;
        private readonly Lazy<DiagnosticDescriptor> _warningDiagnosticDescriptor;
        private readonly Lazy<DiagnosticDescriptor> _errorDiagnosticDescriptor;
        private readonly Dictionary<DiagnosticSeverity, Lazy<DiagnosticDescriptor>> _severityToDescriptorMap;

        public CSharpTypingStyleDiagnosticAnalyzerBase(string diagnosticId, LocalizableString title, LocalizableString message)
        {
            _diagnosticId = diagnosticId;
            _title = title;
            _message = message;
            _noneDiagnosticDescriptor = new Lazy<DiagnosticDescriptor>(() => CreateDiagnosticDescriptor(DiagnosticSeverity.Hidden));
            _infoDiagnosticDescriptor = new Lazy<DiagnosticDescriptor>(() => CreateDiagnosticDescriptor(DiagnosticSeverity.Info));
            _warningDiagnosticDescriptor = new Lazy<DiagnosticDescriptor>(() => CreateDiagnosticDescriptor(DiagnosticSeverity.Warning));
            _errorDiagnosticDescriptor = new Lazy<DiagnosticDescriptor>(() => CreateDiagnosticDescriptor(DiagnosticSeverity.Error));
            _severityToDescriptorMap =
                new Dictionary<DiagnosticSeverity, Lazy<DiagnosticDescriptor>>
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
            ImmutableArray.Create(_noneDiagnosticDescriptor.Value, _infoDiagnosticDescriptor.Value,
                                  _warningDiagnosticDescriptor.Value, _errorDiagnosticDescriptor.Value);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() =>
            DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement);
        }

        protected abstract bool IsStylePreferred(SyntaxNode declarationStatement, SemanticModel semanticModel, OptionSet optionSet, State state, CancellationToken cancellationToken);
        protected abstract bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan);
        protected abstract bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, EqualsValueClauseSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            TypeSyntax declaredType;
            var shouldAnalyze = false;
            var declarationStatement = context.Node;
            var optionSet = GetOptionSet(context.Options);
            State state = null;

            if (declarationStatement.IsKind(SyntaxKind.VariableDeclaration))
            {
                var declaration = (VariableDeclarationSyntax)declarationStatement;
                declaredType = declaration.Type;

                shouldAnalyze = ShouldAnalyzeVariableDeclaration(declaration, context.SemanticModel, context.CancellationToken);

                if (shouldAnalyze)
                {
                    state = State.Generate(declarationStatement, context.SemanticModel, optionSet, isVariableDeclarationContext: true, cancellationToken: context.CancellationToken);
                    shouldAnalyze = IsStylePreferred(declaration, context.SemanticModel, optionSet, state, context.CancellationToken);
                }
            }
            else if (declarationStatement.IsKind(SyntaxKind.ForEachStatement))
            {
                var declaration = (ForEachStatementSyntax)declarationStatement;
                declaredType = declaration.Type;

                state = State.Generate(declarationStatement, context.SemanticModel, optionSet, isVariableDeclarationContext: false, cancellationToken: context.CancellationToken);
                shouldAnalyze = IsStylePreferred(declaration, context.SemanticModel, optionSet, state, context.CancellationToken);
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

                if (TryAnalyzeVariableDeclaration(declaredType, context.SemanticModel, optionSet, context.CancellationToken, out diagnosticSpan))
                {
                    var descriptor = _severityToDescriptorMap[state.GetDiagnosticSeverityPreference()];
                    context.ReportDiagnostic(CreateDiagnostic(descriptor.Value, declarationStatement, diagnosticSpan));
                }
            }
        }

        private Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan) =>
            Diagnostic.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan));

        private bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // var is applicable only for local variables.
            if (variableDeclaration.Parent.IsKind(SyntaxKind.FieldDeclaration) ||
                variableDeclaration.Parent.IsKind(SyntaxKind.EventFieldDeclaration))
            {
                return false;
            }

            // implicitly typed variables cannot have multiple declarators and
            // must have an initializer.
            if (variableDeclaration.Variables.Count > 1 ||
                !variableDeclaration.Variables.Single().Initializer.IsKind(SyntaxKind.EqualsValueClause))
            {
                return false;
            }

            return true;
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
