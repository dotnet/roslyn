// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase :
        AbstractCodeStyleDiagnosticAnalyzer
    {
        protected CSharpTypeStyleDiagnosticAnalyzerBase(
            string diagnosticId, LocalizableString title, LocalizableString message)
            : base(diagnosticId, title, message)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
        {
            var forIntrinsicTypesOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes).Notification;
            var whereApparentOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent).Notification;
            var wherePossibleOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible).Notification;

            return !(forIntrinsicTypesOption == NotificationOption.Warning || forIntrinsicTypesOption == NotificationOption.Error ||
                     whereApparentOption == NotificationOption.Warning || whereApparentOption == NotificationOption.Error ||
                     wherePossibleOption == NotificationOption.Warning || wherePossibleOption == NotificationOption.Error);
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(
                HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression);

        protected abstract bool IsStylePreferred(SemanticModel semanticModel, OptionSet optionSet, State state, CancellationToken cancellationToken);
        protected abstract bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan);
        protected abstract bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, ExpressionSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        protected static ExpressionSyntax GetInitializerExpression(ExpressionSyntax initializer) =>
            initializer is CheckedExpressionSyntax
                ? ((CheckedExpressionSyntax)initializer).Expression.WalkDownParentheses()
                : initializer.WalkDownParentheses();

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            TypeSyntax declaredType;
            State state = null;
            var shouldAnalyze = false;
            var declarationStatement = context.Node;
            var options = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }
            
            var semanticModel = context.SemanticModel;

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

                shouldAnalyze = ShouldAnalyzeForEachStatement(declaration, semanticModel, cancellationToken);

                if (shouldAnalyze)
                {
                    state = State.Generate(declarationStatement, semanticModel, optionSet, isVariableDeclarationContext: false, cancellationToken: cancellationToken);
                    shouldAnalyze = IsStylePreferred(semanticModel, optionSet, state, cancellationToken);
                }
            }
            else if (declarationStatement.IsKind(SyntaxKind.DeclarationExpression))
            {
                var declaration = (DeclarationExpressionSyntax) declarationStatement;
                declaredType = declaration.Type;

                shouldAnalyze = ShouldAnalyzeDeclarationExpression(declaration, semanticModel, cancellationToken);

                if (shouldAnalyze)
                {
                    state = State.Generate(declarationStatement, semanticModel, optionSet, isVariableDeclarationContext: false, cancellationToken: cancellationToken);
                    shouldAnalyze = IsStylePreferred(semanticModel, optionSet, state, cancellationToken);
                }
            }
            else
            {
                Debug.Assert(false, $"called in for unregistered node kind {declarationStatement.Kind().ToString()}");
                return;
            }

            if (shouldAnalyze)
            {
                Debug.Assert(state != null, "analyzing a declaration and state is null.");
                if (TryAnalyzeVariableDeclaration(declaredType, semanticModel, optionSet, cancellationToken, out var diagnosticSpan))
                {
                    // The severity preference is not Hidden, as indicated by shouldAnalyze.
                    var descriptor = GetDescriptorWithSeverity(state.GetDiagnosticSeverityPreference());
                    context.ReportDiagnostic(CreateDiagnostic(descriptor, declarationStatement, diagnosticSpan));
                }
            }
        }

        private Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan) =>
            Diagnostic.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan));

        protected virtual bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
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

        protected virtual bool ShouldAnalyzeForEachStatement(ForEachStatementSyntax forEachStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
            => true;

        protected virtual bool ShouldAnalyzeDeclarationExpression(DeclarationExpressionSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
            => true;
    }
}
