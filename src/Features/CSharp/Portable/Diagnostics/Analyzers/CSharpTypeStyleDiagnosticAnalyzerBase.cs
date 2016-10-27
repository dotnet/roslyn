// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly string _diagnosticId;
        private readonly LocalizableString _title;
        private readonly LocalizableString _message;

        public CSharpTypeStyleDiagnosticAnalyzerBase(string diagnosticId, LocalizableString title, LocalizableString message)
        {
            _diagnosticId = diagnosticId;
            _title = title;
            _message = message;
        }

        private DiagnosticDescriptor CreateDiagnosticDescriptor(DiagnosticSeverity severity) =>
            new DiagnosticDescriptor(
                _diagnosticId,
                _title,
                _message,
                DiagnosticCategory.Style,
                severity,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CreateDiagnosticDescriptor(DiagnosticSeverity.Hidden));

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public bool OpenFileOnly(Workspace workspace)
        {
            var forIntrinsicTypesOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes).Notification;
            var whereApparentOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent).Notification;
            var wherePossibleOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible).Notification;

            return !(forIntrinsicTypesOption == NotificationOption.Warning || forIntrinsicTypesOption == NotificationOption.Error ||
                     whereApparentOption == NotificationOption.Warning || whereApparentOption == NotificationOption.Error ||
                     wherePossibleOption == NotificationOption.Warning || wherePossibleOption == NotificationOption.Error);
        }

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
            var optionSet = context.Options.GetOptionSet();
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
                    // The severity preference is not Hidden, as indicated by shouldAnalyze.
                    var descriptor = CreateDiagnosticDescriptor(state.GetDiagnosticSeverityPreference());
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
    }
}
