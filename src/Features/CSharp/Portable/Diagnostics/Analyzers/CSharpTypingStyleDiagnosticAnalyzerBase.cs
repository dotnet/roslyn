// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypingStyles
{
    internal abstract class CSharpTypingStyleDiagnosticAnalyzerBase : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        protected enum TypingStyles
        {
            None = 0,
            Implicit = 1 << 0,
            Explicit = 1 << 1,
            ImplicitWhereApparent = 1 << 2,
            Intrinsic = 1 << 3
        }

        private readonly DiagnosticDescriptor _typingStyleDescriptor;

        public CSharpTypingStyleDiagnosticAnalyzerBase(DiagnosticDescriptor descriptor)
        {
            _typingStyleDescriptor = descriptor;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(_typingStyleDescriptor);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() =>
            DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override void Initialize(AnalysisContext context)
        {
            // TODO: check for generatedcode and bail.
            // context.ConfigureGeneratedCodeAnalysis() See https://github.com/dotnet/roslyn/pull/7526

            context.RegisterSyntaxNodeAction(HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement);
        }

        protected abstract bool IsStylePreferred(SyntaxNode declarationStatement, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);
        protected abstract bool AnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan);
        protected abstract bool AnalyzeAssignment(SyntaxToken identifier, TypeSyntax typeName, EqualsValueClauseSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        protected TypingStyles GetCurrentTypingStylePreferences(OptionSet optionSet)
        {
            var stylePreferences = TypingStyles.None;
            var style = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypingForLocals);

            stylePreferences |= style == TypeInferencePreferenceOptions.ImplicitTyping
                                ? TypingStyles.Implicit
                                : TypingStyles.Explicit;

            if (optionSet.GetOption(CSharpCodeStyleOptions.UseVarWhenTypeIsApparent))
            {
                stylePreferences |= TypingStyles.ImplicitWhereApparent;
            }

            if (optionSet.GetOption(CSharpCodeStyleOptions.DoNotUseVarForIntrinsicTypes))
            {
                stylePreferences |= TypingStyles.Intrinsic;
            }

            return stylePreferences;
        }

        protected bool IsTypeApparentFromRHS(SyntaxNode declarationStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // use var in foreach statement to make it concise.
            if (declarationStatement.IsKind(SyntaxKind.ForEachStatement))
            {
                return true;
            }

            // variable declaration cases.
            var variableDeclaration = (VariableDeclarationSyntax)declarationStatement;
            var initializer = variableDeclaration.Variables.Single().Initializer;
            var initializerExpression = initializer.Value;

            // default(T)
            if (initializerExpression.IsKind(SyntaxKind.DefaultExpression))
            {
                return true;
            }

            // constructors of form = new TypeSomething();
            // object creation expression that contains a typename and not an anonymous object creation expression.
            if (initializerExpression.IsKind(SyntaxKind.ObjectCreationExpression) &&
                !initializerExpression.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                return true;
            }

            // invocation expression
            // a. int.Parse, TextSpan.From static methods? 
            // return type or 1 ref/out type matches some part of identifier name within a dotted name.
            // also consider Generic method invocation with type parameters *and* not inferred
            if (initializerExpression.IsKind(SyntaxKind.InvocationExpression))
            {
                var invocation = (InvocationExpressionSyntax)initializerExpression;

                // literals.
                if (invocation.IsAnyLiteralExpression())
                {
                    return true;
                }

                // if memberaccessexpression, get method symbol and check IsStatic.
                var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
                if (!symbol.IsKind(SymbolKind.Method))
                {
                    return false;
                }

                var methodSymbol = (IMethodSymbol)symbol;
                if (!methodSymbol.IsStatic || methodSymbol.ReturnsVoid)
                {
                    // if its a ref/out method, then the return type may not be obvious from the name.
                    return false;
                }

                var declaredTypeSymbol = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;

                IList<string> nameParts;
                if (invocation.Expression.TryGetNameParts(out nameParts))
                {
                    var typeNameIndex = nameParts.IndexOf(methodSymbol.Name) - 1;
                    if (typeNameIndex >= 0)
                    {
                        // returned type is spelled out in the invocation.
                        return declaredTypeSymbol.Name == nameParts[typeNameIndex];
                    }
                }
            }

            // c. Factory Methods? - probably not.

            // TODO: Cast expressions.

            return false;
        }

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            TypeSyntax declaredType;
            var shouldAnalyze = false;
            var declarationStatement = context.Node;
            var optionSet = GetOptionSet(context.Options);

            if (declarationStatement.IsKind(SyntaxKind.VariableDeclaration))
            {
                var declaration = ((VariableDeclarationSyntax)declarationStatement);
                declaredType = declaration.Type;
                shouldAnalyze = ShouldAnalyze(declaration, context.SemanticModel, optionSet, context.CancellationToken);
            }
            else if (declarationStatement.IsKind(SyntaxKind.ForEachStatement))
            {
                var declaration = ((ForEachStatementSyntax)declarationStatement);
                declaredType = declaration.Type;
                shouldAnalyze = ShouldAnalyze(declaration, context.SemanticModel, optionSet, context.CancellationToken);
            }
            else
            {
                Debug.Assert(false, $"called in for unregistered node kind {declarationStatement.Kind().ToString()}");
                return;
            }

            if (shouldAnalyze)
            {
                TextSpan diagnosticSpan;
                var hasDiagnostic = AnalyzeVariableDeclaration(declaredType, context.SemanticModel, optionSet, context.CancellationToken, out diagnosticSpan);

                if (hasDiagnostic)
                {
                    context.ReportDiagnostic(CreateDiagnostic(declarationStatement, diagnosticSpan));
                }
            }
        }

        private Diagnostic CreateDiagnostic(SyntaxNode declaration, TextSpan diagnosticSpan) =>
            Diagnostic.Create(_typingStyleDescriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan));

        private bool ShouldAnalyze(VariableDeclarationSyntax declaration,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken) =>
                ShouldAnalyzeVariableDeclaration(declaration, semanticModel, cancellationToken) &&
                IsStylePreferred(declaration, semanticModel, optionSet, cancellationToken);

        private bool ShouldAnalyze(ForEachStatementSyntax declaration,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken) =>
                IsStylePreferred(declaration, semanticModel, optionSet, cancellationToken);

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
