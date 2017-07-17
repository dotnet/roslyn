﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
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

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseImplicitTypeDiagnosticAnalyzer : CSharpTypeStyleDiagnosticAnalyzerBase
    {
        private static readonly LocalizableString s_Title =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_implicit_type), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly LocalizableString s_Message =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.use_var_instead_of_explicit_type), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        public CSharpUseImplicitTypeDiagnosticAnalyzer()
            : base(diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                   title: s_Title,
                   message: s_Message)
        {
        }

        protected override bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (variableDeclaration.Type.IsVar)
            {
                // If the type is already 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeVariableDeclaration(variableDeclaration, semanticModel, cancellationToken);
        }

        protected override bool ShouldAnalyzeForEachStatement(ForEachStatementSyntax forEachStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (forEachStatement.Type.IsVar)
            {
                // If the type is already 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeForEachStatement(forEachStatement, semanticModel, cancellationToken);
        }

        protected override bool IsStylePreferred(SemanticModel semanticModel, OptionSet optionSet, State state, CancellationToken cancellationToken)
        {
            var stylePreferences = state.TypeStylePreference;
            var shouldNotify = state.ShouldNotify();

            // If notification preference is None, don't offer the suggestion.
            if (!shouldNotify)
            {
                return false;
            }

            if (state.IsInIntrinsicTypeContext)
            {
                return stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeForIntrinsicTypes);
            }
            else if (state.IsTypeApparentInContext)
            {
                return stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeWhereApparent);
            }
            else
            {
                return stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeWherePossible);
            }
        }

        protected override bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan)
        {
            Debug.Assert(!typeName.IsVar, "'var' special case should have prevented analysis of this variable.");

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
                if (AssignmentSupportsStylePreference(
                        variable.Identifier, typeName, variable.Initializer.Value,
                        semanticModel, optionSet, cancellationToken))
                {
                    issueSpan = candidateIssueSpan;
                    return true;
                }
            }
            else if (typeName.Parent is ForEachStatementSyntax foreachStatment)
            {
                var foreachStatementInfo = semanticModel.GetForEachStatementInfo(foreachStatment);
                if (foreachStatementInfo.ElementConversion.IsIdentityOrImplicitReference())
                {
                    issueSpan = candidateIssueSpan;
                    return true;
                }
            }
            else if (typeName.Parent is DeclarationExpressionSyntax declarationExpressionSyntax)
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
        protected override bool AssignmentSupportsStylePreference(
            SyntaxToken identifier,
            TypeSyntax typeName,
            ExpressionSyntax initializer,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
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

        protected override bool ShouldAnalyzeDeclarationExpression(DeclarationExpressionSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (declaration.Type.IsVar)
            {
                // If the type is already 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeDeclarationExpression(declaration, semanticModel, cancellationToken);
        }
    }
}
