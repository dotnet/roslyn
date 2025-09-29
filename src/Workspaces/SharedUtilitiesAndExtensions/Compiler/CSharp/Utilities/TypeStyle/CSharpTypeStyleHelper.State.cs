// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Utilities;

internal partial class CSharpTypeStyleHelper
{
    public enum Context
    {
        BuiltInType,
        TypeIsApparent,
        Elsewhere
    }

    protected readonly struct State
    {
        public readonly UseVarPreference TypeStylePreference;

        private readonly NotificationOption2 _forBuiltInTypes;
        private readonly NotificationOption2 _whenTypeIsApparent;
        private readonly NotificationOption2 _elsewhere;

        public readonly Context Context;

        public State(
            SyntaxNode declaration, SemanticModel semanticModel,
            CSharpSimplifierOptions options, CancellationToken cancellationToken)
        {
            TypeStylePreference = default;

            var styleForIntrinsicTypes = options.VarForBuiltInTypes;
            var styleForApparent = options.VarWhenTypeIsApparent;
            var styleForElsewhere = options.VarElsewhere;

            _forBuiltInTypes = styleForIntrinsicTypes.Notification;
            _whenTypeIsApparent = styleForApparent.Notification;
            _elsewhere = styleForElsewhere.Notification;

            this.TypeStylePreference = options.GetUseVarPreference();

            if (IsPredefinedTypeInDeclaration(declaration, semanticModel) ||
                IsInferredPredefinedType(declaration, semanticModel))
            {
                this.Context = Context.BuiltInType;
            }
            else if (declaration is VariableDeclarationSyntax varDecl &&
                IsTypeApparentInDeclaration(varDecl, semanticModel, TypeStylePreference, cancellationToken))
            {
                this.Context = Context.TypeIsApparent;
            }
            else
            {
                this.Context = Context.Elsewhere;
            }
        }

        public NotificationOption2 GetDiagnosticSeverityPreference()
            => Context switch
            {
                Context.BuiltInType => _forBuiltInTypes,
                Context.TypeIsApparent => _whenTypeIsApparent,
                Context.Elsewhere => _elsewhere,
                _ => throw ExceptionUtilities.UnexpectedValue(Context),
            };

        /// <summary>
        /// Returns true if type information could be gleaned by simply looking at the given statement.
        /// This typically means that the type name occurs in right hand side of an assignment.
        /// </summary>
        private static bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, UseVarPreference stylePreferences, CancellationToken cancellationToken)
        {
            if (variableDeclaration.Variables.Count != 1)
            {
                return false;
            }

            var initializer = variableDeclaration.Variables[0].Initializer;
            if (initializer == null)
            {
                return false;
            }

            var initializerExpression = CSharpUseImplicitTypeHelper.GetInitializerExpression(initializer.Value);
            var declaredTypeSymbol = semanticModel.GetTypeInfo(variableDeclaration.Type.StripRefIfNeeded(), cancellationToken).Type;
            return TypeStyleHelper.IsTypeApparentInAssignmentExpression(stylePreferences, initializerExpression, semanticModel, declaredTypeSymbol, cancellationToken);
        }

        /// <summary>
        /// checks if the type represented by the given symbol is one of the
        /// simple types defined in the compiler.
        /// </summary>
        /// <remarks>
        /// From the IDE perspective, we also include object and string to be simplified
        /// to var. <see cref="SyntaxFacts.IsPredefinedType(SyntaxKind)"/> considers string
        /// and object but the compiler's implementation of IsIntrinsicType does not.
        /// </remarks>
        private static bool IsPredefinedTypeInDeclaration(SyntaxNode declarationStatement, SemanticModel semanticModel)
        {
            var typeSyntax = GetTypeSyntaxFromDeclaration(declarationStatement);

            return typeSyntax != null
                ? IsMadeOfSpecialTypes(semanticModel.GetTypeInfo(typeSyntax.StripRefIfNeeded()).Type)
                : false;
        }

        /// <summary>
        /// Returns true for type that are arrays/nullable/pointer types of special types
        /// </summary>
        private static bool IsMadeOfSpecialTypes([NotNullWhen(true)] ITypeSymbol? type)
        {
            if (type == null)
            {
                return false;
            }

            while (true)
            {
                type = type.RemoveNullableIfPresent();

                if (type is IArrayTypeSymbol arrayType)
                {
                    type = arrayType.ElementType;
                    continue;
                }

                if (type is IPointerTypeSymbol pointerType)
                {
                    type = pointerType.PointedAtType;
                    continue;
                }

                return type.IsSpecialType();
            }
        }

        private static bool IsInferredPredefinedType(SyntaxNode declarationStatement, SemanticModel semanticModel)
        {
            var typeSyntax = GetTypeSyntaxFromDeclaration(declarationStatement);

            return typeSyntax != null &&
                typeSyntax.IsTypeInferred(semanticModel) &&
                semanticModel.GetTypeInfo(typeSyntax).Type?.IsSpecialType() == true;
        }

        private static TypeSyntax? GetTypeSyntaxFromDeclaration(SyntaxNode declarationStatement)
            => declarationStatement switch
            {
                VariableDeclarationSyntax varDecl => varDecl.Type,
                ForEachStatementSyntax forEach => forEach.Type,
                DeclarationExpressionSyntax declExpr => declExpr.Type,
                _ => null,
            };
    }
}
