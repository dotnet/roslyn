﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase
    {
        internal class State
        {
            private readonly Dictionary<TypeStylePreference, DiagnosticSeverity> _styleToSeverityMap;

            public TypeStylePreference TypeStylePreference { get; private set; }
            public bool IsInIntrinsicTypeContext { get; private set; }
            public bool IsTypeApparentInContext { get; private set; }
            public bool IsInVariableDeclarationContext { get; }

            public State(bool isVariableDeclarationContext)
            {
                this.IsInVariableDeclarationContext = isVariableDeclarationContext;
                _styleToSeverityMap = new Dictionary<TypeStylePreference, DiagnosticSeverity>();
            }

            public static State Generate(SyntaxNode declaration, SemanticModel semanticModel, OptionSet optionSet, bool isVariableDeclarationContext, CancellationToken cancellationToken)
            {
                var state = new State(isVariableDeclarationContext);
                state.Initialize(declaration, semanticModel, optionSet, cancellationToken);
                return state;
            }

            public DiagnosticSeverity GetDiagnosticSeverityPreference()
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

            public bool ShouldNotify() =>
                GetDiagnosticSeverityPreference() != DiagnosticSeverity.Hidden;

            private void Initialize(SyntaxNode declaration, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            {
                this.TypeStylePreference = GetCurrentTypeStylePreferences(optionSet);

                IsTypeApparentInContext =
                        IsInVariableDeclarationContext
                     && IsTypeApparentInDeclaration((VariableDeclarationSyntax)declaration, semanticModel, TypeStylePreference, cancellationToken);

                IsInIntrinsicTypeContext =
                        IsPredefinedTypeInDeclaration(declaration)
                     || IsInferredPredefinedType(declaration, semanticModel, cancellationToken);
            }

            /// <summary>
            /// Returns true if type information could be gleaned by simply looking at the given statement.
            /// This typically means that the type name occurs in right hand side of an assignment.
            /// </summary>
            private bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, TypeStylePreference stylePreferences, CancellationToken cancellationToken)
            {
                var initializer = variableDeclaration.Variables.Single().Initializer;
                var initializerExpression = GetInitializerExpression(initializer.Value);
                var declaredTypeSymbol = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;
                return TypeStyleHelper.IsTypeApparentInAssignmentExpression(stylePreferences, initializerExpression, semanticModel,cancellationToken, declaredTypeSymbol);
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
            private bool IsPredefinedTypeInDeclaration(SyntaxNode declarationStatement)
            {
                return TypeStyleHelper.IsPredefinedType(
                    GetTypeSyntaxFromDeclaration(declarationStatement));
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
                if (declarationStatement is VariableDeclarationSyntax varDecl)
                {
                    return varDecl.Type;
                }
                else if (declarationStatement is ForEachStatementSyntax forEach)
                {
                    return forEach.Type;
                }

                return null;
            }

            private TypeStylePreference GetCurrentTypeStylePreferences(OptionSet optionSet)
            {
                var stylePreferences = TypeStylePreference.None;

                var styleForIntrinsicTypes = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes);
                var styleForApparent = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent);
                var styleForElsewhere = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible);

                _styleToSeverityMap.Add(TypeStylePreference.ImplicitTypeForIntrinsicTypes, styleForIntrinsicTypes.Notification.Value);
                _styleToSeverityMap.Add(TypeStylePreference.ImplicitTypeWhereApparent, styleForApparent.Notification.Value);
                _styleToSeverityMap.Add(TypeStylePreference.ImplicitTypeWherePossible, styleForElsewhere.Notification.Value);

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
