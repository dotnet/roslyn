// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using static Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle.CSharpTypeStyleDiagnosticAnalyzerBase;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    internal abstract partial class CSharpTypeStyleHelper
    {
        internal abstract bool IsStylePreferred(SemanticModel semanticModel, OptionSet optionSet, State state, CancellationToken cancellationToken);
        internal abstract bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);
        protected abstract bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, ExpressionSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        internal TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            Debug.Assert(node.IsKind(SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression));

            switch (node)
            {
                case VariableDeclarationSyntax variableDeclaration:
                    return ShouldAnalyzeVariableDeclaration(variableDeclaration, semanticModel, cancellationToken)
                        ? variableDeclaration.Type
                        : null;
                case ForEachStatementSyntax forEachStatement:
                    return ShouldAnalyzeForEachStatement(forEachStatement, semanticModel, cancellationToken)
                        ? forEachStatement.Type
                        : null;
                case DeclarationExpressionSyntax declarationExpression:
                    return ShouldAnalyzeDeclarationExpression(declarationExpression, semanticModel, cancellationToken)
                        ? declarationExpression.Type
                        : null;
            }

            return null;
        }

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

    internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase :
        AbstractCodeStyleDiagnosticAnalyzer
    {
        protected abstract CSharpTypeStyleHelper Helper { get; }

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

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
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
            var declaredType = Helper.FindAnalyzableType(declarationStatement, semanticModel, cancellationToken);
            if (declaredType == null)
            {
                return;
            }

            var state = State.Generate(declarationStatement, semanticModel, optionSet,
                isVariableDeclarationContext: declarationStatement.IsKind(SyntaxKind.VariableDeclaration), cancellationToken: cancellationToken);

            if (!Helper.IsStylePreferred(semanticModel, optionSet, state, cancellationToken))
            {
                return;
            }

            Debug.Assert(state != null, "analyzing a declaration and state is null.");
            if (!Helper.TryAnalyzeVariableDeclaration(declaredType, semanticModel, optionSet, cancellationToken))
            {
                return;
            }

            // The severity preference is not Hidden, as indicated by IsStylePreferred.
            var descriptor = GetDescriptorWithSeverity(state.GetDiagnosticSeverityPreference());
            context.ReportDiagnostic(CreateDiagnostic(descriptor, declarationStatement, declaredType.Span));
        }

        internal static ExpressionSyntax GetInitializerExpression(ExpressionSyntax initializer) =>
            initializer is CheckedExpressionSyntax
                ? ((CheckedExpressionSyntax)initializer).Expression.WalkDownParentheses()
                : initializer.WalkDownParentheses();

        private Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan) =>
            Diagnostic.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan));
    }
}
