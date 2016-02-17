// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
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
        [Flags]
        protected enum TypingStyles
        {
            None = 0,
            VarForIntrinsic = 1 << 0,
            VarWhereApparent = 1 << 1,
            VarWherePossible = 1 << 2,
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
            context.RegisterSyntaxNodeAction(HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement);
        }

        protected abstract bool IsStylePreferred(SyntaxNode declarationStatement, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);
        protected abstract bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan);
        protected abstract bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, EqualsValueClauseSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        protected TypingStyles GetCurrentTypingStylePreferences(OptionSet optionSet)
        {
            var stylePreferences = TypingStyles.None;

            if (optionSet.GetOption(CSharpCodeStyleOptions.UseVarForIntrinsicTypes))
            {
                stylePreferences |= TypingStyles.VarForIntrinsic;
            }

            if (optionSet.GetOption(CSharpCodeStyleOptions.UseVarWhenTypeIsApparent))
            {
                stylePreferences |= TypingStyles.VarWhereApparent;
            }

            if (optionSet.GetOption(CSharpCodeStyleOptions.UseVarWherePossible))
            {
                stylePreferences |= TypingStyles.VarWherePossible;
            }

            return stylePreferences;
        }

        /// <summary>
        /// Returns true if type information could be gleaned by simply looking at the given statement.
        /// This typically means that the type name occurs in either left hand or right hand side of an assignment.
        /// </summary>
        protected bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, TypingStyles stylePreferences, CancellationToken cancellationToken)
        {
            var initializer = variableDeclaration.Variables.Single().Initializer;
            var initializerExpression = GetInitializerExpression(initializer);

            // default(type)
            if (initializerExpression.IsKind(SyntaxKind.DefaultExpression))
            {
                return true;
            }

            // literals, use var if options allow usage here.
            if (initializerExpression.IsAnyLiteralExpression())
            {
                return stylePreferences.HasFlag(TypingStyles.VarForIntrinsic);
            }

            // constructor invocations cases:
            //      = new type();
            if (initializerExpression.IsKind(SyntaxKind.ObjectCreationExpression) &&
                !initializerExpression.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                return true;
            }

            // explicit conversion cases: 
            //      (type)expr, expr is type, expr as type
            if (initializerExpression.IsKind(SyntaxKind.CastExpression) ||
                initializerExpression.IsKind(SyntaxKind.IsExpression) ||
                initializerExpression.IsKind(SyntaxKind.AsExpression))
            {
                return true;
            }

            // other Conversion cases:
            //      a. conversion with helpers like: int.Parse, TextSpan.From methods 
            //      b. types that implement IConvertible and then invoking .ToType()
            //      c. System.Convert.Totype()
            var declaredTypeSymbol = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;
            var expressionOnRightSide = initializerExpression.WalkDownParentheses();

            var memberName = expressionOnRightSide.GetRightmostName();
            if (memberName == null)
            {
                return false;
            }

            var methodSymbol = semanticModel.GetSymbolInfo(memberName, cancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            if (memberName.IsRightSideOfDot())
            {
                var typeName = memberName.GetLeftSideOfDot();
                return IsPossibleConversionMethod(methodSymbol, declaredTypeSymbol, semanticModel, typeName, cancellationToken);
            }

            return false;
        }

        protected bool IsIntrinsicType(SyntaxNode declarationStatement) =>
            declarationStatement.IsKind(SyntaxKind.VariableDeclaration)
            ? ((VariableDeclarationSyntax)declarationStatement).Variables.Single().Initializer.Value.IsAnyLiteralExpression()
            : false;

        private ExpressionSyntax GetInitializerExpression(EqualsValueClauseSyntax initializer) =>
            initializer.Value is CheckedExpressionSyntax
                ? ((CheckedExpressionSyntax)initializer.Value).Expression
                : initializer.Value;

        private bool IsPossibleConversionMethod(IMethodSymbol methodSymbol, ITypeSymbol declaredType, SemanticModel semanticModel, ExpressionSyntax typeName, CancellationToken cancellationToken)
        {
            if (methodSymbol.ReturnsVoid)
            {
                return false;
            }

            var typeInInvocation = semanticModel.GetTypeInfo(typeName, cancellationToken).Type;

            // case: int.Parse or TextSpan.FromBounds
            if (methodSymbol.Name.StartsWith("Parse", StringComparison.Ordinal) 
                || methodSymbol.Name.StartsWith("From", StringComparison.Ordinal))
            {
                return typeInInvocation.Equals(declaredType);
            }

            // take `char` from `char? c = `
            var declaredTypeName = declaredType.IsNullable() 
                    ? declaredType.GetTypeArguments().First().Name
                    : declaredType.Name;

            // case: Convert.ToString or iConvertible.ToChar
            if (methodSymbol.Name.Equals("To" + declaredTypeName, StringComparison.Ordinal))
            {
                var convertType = semanticModel.Compilation.ConvertType();
                var iConvertibleType = semanticModel.Compilation.IConvertibleType();

                return typeInInvocation.Equals(convertType)
                    || typeInInvocation.Equals(iConvertibleType);
            }

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
                var declaration = (VariableDeclarationSyntax)declarationStatement;
                declaredType = declaration.Type;
                shouldAnalyze = ShouldAnalyze(declaration, context.SemanticModel, optionSet, context.CancellationToken);
            }
            else if (declarationStatement.IsKind(SyntaxKind.ForEachStatement))
            {
                var declaration = (ForEachStatementSyntax)declarationStatement;
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

                if (TryAnalyzeVariableDeclaration(declaredType, context.SemanticModel, optionSet, context.CancellationToken, out diagnosticSpan))
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
