// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal sealed class CSharpUseExplicitTypeHelper : CSharpTypeStyleHelper
    {
        public static CSharpUseExplicitTypeHelper Instance = new();

        private CSharpUseExplicitTypeHelper()
        {
        }

        protected override bool IsStylePreferred(in State state)
        {
            var stylePreferences = state.TypeStylePreference;

            if (state.IsInIntrinsicTypeContext)
            {
                return !stylePreferences.HasFlag(UseVarPreference.ForBuiltInTypes);
            }
            else if (state.IsTypeApparentInContext)
            {
                return !stylePreferences.HasFlag(UseVarPreference.WhenTypeIsApparent);
            }
            else
            {
                return !stylePreferences.HasFlag(UseVarPreference.Elsewhere);
            }
        }

        public override bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, CancellationToken cancellationToken)
        {
            if (!variableDeclaration.Type.StripRefIfNeeded().IsVar)
            {
                // If the type is not 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeVariableDeclaration(variableDeclaration, cancellationToken);
        }

        protected override bool ShouldAnalyzeForEachStatement(ForEachStatementSyntax forEachStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!forEachStatement.Type.StripRefIfNeeded().IsVar)
            {
                // If the type is not 'var', this analyze has no work to do
                return false;
            }

            // The base analyzer may impose further limitations
            return base.ShouldAnalyzeForEachStatement(forEachStatement, semanticModel, cancellationToken);
        }

        internal override bool TryAnalyzeVariableDeclaration(
            TypeSyntax typeName, SemanticModel semanticModel,
            CSharpSimplifierOptions options, CancellationToken cancellationToken)
        {
            // var (x, y) = e;
            // foreach (var (x, y) in e) ...
            if (typeName.Parent is DeclarationExpressionSyntax declExpression &&
                declExpression.Designation.IsKind(SyntaxKind.ParenthesizedVariableDesignation))
            {
                return true;
            }

            // If it is currently not var, explicit typing exists, return. 
            // this also takes care of cases where var is mapped to a named type via an alias or a class declaration.
            if (!typeName.StripRefIfNeeded().IsTypeInferred(semanticModel))
            {
                return false;
            }

            if (typeName.Parent is VariableDeclarationSyntax variableDeclaration &&
                typeName.Parent.Parent is (kind: SyntaxKind.LocalDeclarationStatement or SyntaxKind.ForStatement or SyntaxKind.UsingStatement))
            {
                // check assignment for variable declarations.
                var variable = variableDeclaration.Variables.First();
                RoslynDebug.AssertNotNull(variable.Initializer);
                if (!AssignmentSupportsStylePreference(
                        variable.Identifier, typeName, variable.Initializer.Value,
                        semanticModel, options, cancellationToken))
                {
                    return false;
                }

                // This error case is handled by a separate code fix (UseExplicitTypeForConst).
                if ((variableDeclaration.Parent as LocalDeclarationStatementSyntax)?.IsConst == true)
                {
                    return false;
                }
            }
            else if (typeName.Parent is ForEachStatementSyntax foreachStatement &&
                     foreachStatement.Type == typeName)
            {
                if (!AssignmentSupportsStylePreference(
                        foreachStatement.Identifier, typeName, foreachStatement.Expression,
                        semanticModel, options, cancellationToken))
                {
                    return false;
                }
            }

            return true;
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
            CSharpSimplifierOptions options,
            CancellationToken cancellationToken)
        {
            // is or contains an anonymous type
            // cases :
            //        var anon = new { Num = 1 };
            //        var enumerableOfAnons = from prod in products select new { prod.Color, prod.Price };
            var declaredType = semanticModel.GetTypeInfo(typeName.StripRefIfNeeded(), cancellationToken).Type;
            if (declaredType.ContainsAnonymousType())
            {
                return false;
            }

            // cannot find type if initializer resolves to an ErrorTypeSymbol
            var initializerTypeInfo = semanticModel.GetTypeInfo(initializer, cancellationToken);
            return !initializerTypeInfo.Type.IsErrorType();
        }
    }
}
