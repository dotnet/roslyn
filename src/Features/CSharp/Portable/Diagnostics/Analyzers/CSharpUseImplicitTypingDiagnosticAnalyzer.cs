// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypingStyles
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseImplicitTypingDiagnosticAnalyzer : CSharpTypingStyleDiagnosticAnalyzerBase
    {
        private static readonly LocalizableString s_Title =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.UseImplicitTypeDiagnosticTitle), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly LocalizableString s_Message =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.UseImplicitType), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        public CSharpUseImplicitTypingDiagnosticAnalyzer()
            : base(diagnosticId: IDEDiagnosticIds.UseImplicitTypingDiagnosticId,
                   title: s_Title,
                   message: s_Message)
        {
        }

        protected override bool IsStylePreferred(SemanticModel semanticModel, OptionSet optionSet, State state, CancellationToken cancellationToken)
        {
            var stylePreferences = state.TypeStyle;
            var shouldNotify = state.ShouldNotify();

            // If notification preference is None, don't offer the suggestion.
            if (!shouldNotify)
            {
                return false;
            }

            if (state.IsInIntrinsicTypeContext)
            {
                return stylePreferences.HasFlag(TypeStyle.ImplicitTypeForIntrinsicTypes);
            }
            else if (state.IsTypingApparentInContext)
            {
                return stylePreferences.HasFlag(TypeStyle.ImplicitTypeWhereApparent);
            }
            else
            {
                return stylePreferences.HasFlag(TypeStyle.ImplicitTypeWherePossible);
            }
        }

        protected override bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan)
        {
            // If it is already var, return.
            if (typeName.IsTypeInferred(semanticModel))
            {
                issueSpan = default(TextSpan);
                return false;
            }

            var candidateReplacementNode = SyntaxFactory.IdentifierName("var");
            var candidateIssueSpan = typeName.Span;

            // If there exists a type named var, return.
            var conflict = semanticModel.GetSpeculativeSymbolInfo(typeName.SpanStart, candidateReplacementNode, SpeculativeBindingOption.BindAsTypeOrNamespace).Symbol;
            if (conflict?.IsKind(SymbolKind.NamedType) == true)
            {
                issueSpan = default(TextSpan);
                return false;
            }

            if (typeName.Parent.IsKind(SyntaxKind.VariableDeclaration) &&
                typeName.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.UsingStatement))
            {
                var variableDeclaration = (VariableDeclarationSyntax)typeName.Parent;

                // implicitly typed variables cannot be constants.
                if ((variableDeclaration.Parent as LocalDeclarationStatementSyntax)?.IsConst == true)
                {
                    issueSpan = default(TextSpan);
                    return false;
                }

                var variable = variableDeclaration.Variables.Single();
                if (AssignmentSupportsStylePreference(variable.Identifier, typeName, variable.Initializer, semanticModel, optionSet, cancellationToken))
                {
                    issueSpan = candidateIssueSpan;
                    return true;
                }
            }
            else if (typeName.IsParentKind(SyntaxKind.ForEachStatement))
            {
                issueSpan = candidateIssueSpan;
                return true;
            }

            issueSpan = default(TextSpan);
            return false;
        }

        /// <summary>
        /// Analyzes the assignment expression and rejects a given declaration if it is unsuitable for implicit typing.
        /// </summary>
        /// <returns>
        /// false, if implicit typing cannot be used.
        /// true, otherwise.
        /// </returns>
        protected override bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, EqualsValueClauseSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            var expression = GetInitializerExpression(initializer);

            // var cannot be assigned null
            if (expression.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return false;
            }

            // cannot use implicit typing on method group, anonymous function or on dynamic
            var declaredType = semanticModel.GetTypeInfo(typeName, cancellationToken).Type;
            if (declaredType != null &&
               (declaredType.TypeKind == TypeKind.Delegate || declaredType.TypeKind == TypeKind.Dynamic))
            {
                return false;
            }

            // variables declared using var cannot be used further in the same initialization expression.
            if (initializer.DescendantNodesAndSelf()
                    .Where(n => (n as IdentifierNameSyntax)?.Identifier.ValueText.Equals(identifier.ValueText) == true)
                    .Any(n => semanticModel.GetSymbolInfo(n, cancellationToken).Symbol?.IsKind(SymbolKind.Local) == true))
            {
                return false;
            }

            // Get the conversion that occurred between the expression's type and type implied by the expression's context
            // and filter out implicit conversions. If an implicit conversion (other than identity) exists
            // and if we're replacing the declaration with 'var' we'd be changing the semantics by inferring type of
            // initializer expression and thereby losing the conversion.
            var conversion = semanticModel.GetConversion(expression, cancellationToken);
            if (conversion.Exists && conversion.IsImplicit && !conversion.IsIdentity)
            {
                return false;
            }

            // final check to compare type information on both sides of assignment.
            var initializerType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            return declaredType.Equals(initializerType);
        }
    }
}