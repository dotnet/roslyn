// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal sealed class CSharpUseExplicitTypeDiagnosticAnalyzer : CSharpTypeStyleDiagnosticAnalyzerBase
    {
        private static readonly LocalizableString s_Title =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_explicit_type), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly LocalizableString s_Message =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_explicit_type_instead_of_var), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        public CSharpUseExplicitTypeDiagnosticAnalyzer()
            : base(diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                   title: s_Title,
                   message: s_Message)
        {
        }

        protected override bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!variableDeclaration.Type.IsVar)
            {
                // If the type is not 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeVariableDeclaration(variableDeclaration, semanticModel, cancellationToken);
        }

        protected override bool ShouldAnalyzeForEachStatement(ForEachStatementSyntax forEachStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!forEachStatement.Type.IsVar)
            {
                // If the type is not 'var', this analyze has no work to do
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
                return !stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeForIntrinsicTypes);
            }
            else if (state.IsTypeApparentInContext)
            {
                return !stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeWhereApparent);
            }
            else
            {
                return !stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeWherePossible);
            }
        }

        protected override bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan)
        {
            issueSpan = default;

            // If it is currently not var, explicit typing exists, return. 
            // this also takes care of cases where var is mapped to a named type via an alias or a class declaration.
            if (!typeName.IsTypeInferred(semanticModel))
            {
                return false;
            }

            if (typeName.Parent.IsKind(SyntaxKind.VariableDeclaration) &&
                typeName.Parent.Parent.IsKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.UsingStatement))
            {
                // check assignment for variable declarations.
                var variable = ((VariableDeclarationSyntax)typeName.Parent).Variables.First();
                if (!AssignmentSupportsStylePreference(
                        variable.Identifier, typeName, variable.Initializer.Value,
                        semanticModel, optionSet, cancellationToken))
                {
                    return false;
                }
            }
            else if (typeName.Parent.IsKind(SyntaxKind.ForEachStatement))
            {
                var foreachStatement = (ForEachStatementSyntax)typeName.Parent;
                if (!AssignmentSupportsStylePreference(
                        foreachStatement.Identifier, typeName, foreachStatement.Expression, 
                        semanticModel, optionSet, cancellationToken))
                {
                    return false;
                }
            }

            issueSpan = typeName.Span;
            return true;
        }

        /// <summary>
        /// Analyzes the assignment expression and rejects a given declaration if it is unsuitable for explicit typing.
        /// </summary>
        /// <returns>
        /// false, if explicit typing cannot be used.
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
            // is or contains an anonymous type
            // cases :
            //        var anon = new { Num = 1 };
            //        var enumerableOfAnons = from prod in products select new { prod.Color, prod.Price };
            var declaredType = semanticModel.GetTypeInfo(typeName, cancellationToken).Type;
            if (declaredType.ContainsAnonymousType())
            {
                return false;
            }

            // cannot find type if initializer resolves to an ErrorTypeSymbol
            var initializerTypeInfo = semanticModel.GetTypeInfo(initializer, cancellationToken);
            return !initializerTypeInfo.Type.IsErrorType();
        }

        protected override bool ShouldAnalyzeDeclarationExpression(DeclarationExpressionSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!declaration.Type.IsVar)
            {
                // If the type is not 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeDeclarationExpression(declaration, semanticModel, cancellationToken);
        }
    }
}
