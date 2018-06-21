// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal partial class CSharpTypeStyleHelper
    {
        protected class State
        {
            private readonly Dictionary<TypeStylePreference, ReportDiagnostic> _styleToSeverityMap;

            public TypeStylePreference TypeStylePreference { get; private set; }
            public bool IsInIntrinsicTypeContext { get; private set; }
            public bool IsTypeApparentInContext { get; private set; }
            public bool IsInVariableDeclarationContext { get; }

            private State(bool isVariableDeclarationContext)
            {
                this.IsInVariableDeclarationContext = isVariableDeclarationContext;
                _styleToSeverityMap = new Dictionary<TypeStylePreference, ReportDiagnostic>();
            }

            public static State Generate(
                SyntaxNode declaration, SemanticModel semanticModel,
                OptionSet optionSet, CancellationToken cancellationToken)
            {
                var isVariableDeclarationContext = declaration.IsKind(SyntaxKind.VariableDeclaration);
                var state = new State(isVariableDeclarationContext);
                state.Initialize(declaration, semanticModel, optionSet, cancellationToken);
                return state;
            }

            public ReportDiagnostic GetDiagnosticSeverityPreference()
            {
                if (IsInIntrinsicTypeContext)
                {
                    return _styleToSeverityMap[TypeStylePreference.ImplicitTypeForIntrinsicTypes];
                }
                else if (IsTypeApparentInContext)
                {
                    return _styleToSeverityMap[TypeStylePreference.ImplicitTypeWhereApparent];
                }
                else
                {
                    return _styleToSeverityMap[TypeStylePreference.ImplicitTypeWherePossible];
                }
            }

            private void Initialize(SyntaxNode declaration, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            {
                this.TypeStylePreference = GetCurrentTypeStylePreferences(optionSet);

                IsTypeApparentInContext =
                        IsInVariableDeclarationContext
                     && IsTypeApparentInDeclaration((VariableDeclarationSyntax)declaration, semanticModel, TypeStylePreference, cancellationToken);

                IsInIntrinsicTypeContext =
                        IsPredefinedTypeInDeclaration(declaration, semanticModel)
                     || IsInferredPredefinedType(declaration, semanticModel, cancellationToken);
            }

            /// <summary>
            /// Returns true if type information could be gleaned by simply looking at the given statement.
            /// This typically means that the type name occurs in right hand side of an assignment.
            /// </summary>
            private bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, TypeStylePreference stylePreferences, CancellationToken cancellationToken)
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
                return TypeStyleHelper.IsTypeApparentInAssignmentExpression(stylePreferences, initializerExpression, semanticModel, cancellationToken, declaredTypeSymbol);
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
            private bool IsPredefinedTypeInDeclaration(SyntaxNode declarationStatement, SemanticModel semanticModel)
            {
                var typeSyntax = GetTypeSyntaxFromDeclaration(declarationStatement);

                return typeSyntax != null
                    ? IsMadeOfSpecialTypes(semanticModel.GetTypeInfo(typeSyntax.StripRefIfNeeded()).Type)
                    : false;
            }

            /// <summary>
            /// Returns true for type that are arrays/nullable/pointer types of special types
            /// </summary>
            private bool IsMadeOfSpecialTypes(ITypeSymbol type)
            {
                if (type == null)
                {
                    return false;
                }

                while (true)
                {
                    type = type.RemoveNullableIfPresent();

                    if (type.IsArrayType())
                    {
                        type = ((IArrayTypeSymbol)type).ElementType;
                        continue;
                    }

                    if (type.IsPointerType())
                    {
                        type = ((IPointerTypeSymbol)type).PointedAtType;
                        continue;
                    }

                    return type.IsSpecialType();
                }
            }

            private bool IsInferredPredefinedType(SyntaxNode declarationStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var typeSyntax = GetTypeSyntaxFromDeclaration(declarationStatement);

                return typeSyntax != null &&
                    typeSyntax.IsTypeInferred(semanticModel) &&
                    semanticModel.GetTypeInfo(typeSyntax).Type?.IsSpecialType() == true;
            }

            private TypeSyntax GetTypeSyntaxFromDeclaration(SyntaxNode declarationStatement)
            {
                switch (declarationStatement)
                {
                    case VariableDeclarationSyntax varDecl:
                        return varDecl.Type;
                    case ForEachStatementSyntax forEach:
                        return forEach.Type;
                    case DeclarationExpressionSyntax declExpr:
                        return declExpr.Type;
                }

                return null;
            }

            private TypeStylePreference GetCurrentTypeStylePreferences(OptionSet optionSet)
            {
                var stylePreferences = TypeStylePreference.None;

                var styleForIntrinsicTypes = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes);
                var styleForApparent = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent);
                var styleForElsewhere = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible);

                _styleToSeverityMap.Add(TypeStylePreference.ImplicitTypeForIntrinsicTypes, styleForIntrinsicTypes.Notification.Severity);
                _styleToSeverityMap.Add(TypeStylePreference.ImplicitTypeWhereApparent, styleForApparent.Notification.Severity);
                _styleToSeverityMap.Add(TypeStylePreference.ImplicitTypeWherePossible, styleForElsewhere.Notification.Severity);

                if (styleForIntrinsicTypes.Value)
                {
                    stylePreferences |= TypeStylePreference.ImplicitTypeForIntrinsicTypes;
                }

                if (styleForApparent.Value)
                {
                    stylePreferences |= TypeStylePreference.ImplicitTypeWhereApparent;
                }

                if (styleForElsewhere.Value)
                {
                    stylePreferences |= TypeStylePreference.ImplicitTypeWherePossible;
                }

                return stylePreferences;
            }
        }
    }
}
