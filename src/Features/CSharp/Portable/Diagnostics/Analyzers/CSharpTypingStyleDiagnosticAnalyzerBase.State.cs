// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypingStyles
{
    internal partial class CSharpTypingStyleDiagnosticAnalyzerBase
    {
        internal class State
        {
            private readonly Dictionary<TypeStyle, DiagnosticSeverity> _styleToSeverityMap;

            public TypeStyle TypeStyle { get; private set; }
            public bool IsInIntrinsicTypeContext { get; private set; }
            public bool IsTypingApparentInContext { get; private set; }
            public bool IsInVariableDeclarationContext { get; }

            public State(bool isVariableDeclarationContext)
            {
                this.IsInVariableDeclarationContext = isVariableDeclarationContext;
                _styleToSeverityMap = new Dictionary<TypeStyle, DiagnosticSeverity>();
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
                    return _styleToSeverityMap[TypeStyle.ImplicitTypeForIntrinsicTypes];
                }
                else if (IsTypingApparentInContext)
                {
                    return _styleToSeverityMap[TypeStyle.ImplicitTypeWhereApparent];
                }
                else
                {
                    return _styleToSeverityMap[TypeStyle.ImplicitTypeWherePossible];
                }
            }

            public bool ShouldNotify() =>
                GetDiagnosticSeverityPreference() != DiagnosticSeverity.Hidden;

            private void Initialize(SyntaxNode declaration, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            {
                this.TypeStyle = GetCurrentTypingStylePreferences(optionSet);

                IsTypingApparentInContext =
                        IsInVariableDeclarationContext
                     && IsTypeApparentInDeclaration((VariableDeclarationSyntax)declaration, semanticModel, TypeStyle, cancellationToken);

                IsInIntrinsicTypeContext =
                        IsPredefinedTypeInDeclaration(declaration)
                     || IsInferredPredefinedType(declaration, semanticModel, cancellationToken);
            }

            /// <summary>
            /// Returns true if type information could be gleaned by simply looking at the given statement.
            /// This typically means that the type name occurs in right hand side of an assignment.
            /// </summary>
            private bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, TypeStyle stylePreferences, CancellationToken cancellationToken)
            {
                var initializer = variableDeclaration.Variables.Single().Initializer;
                var initializerExpression = GetInitializerExpression(initializer);
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
                var predefinedType = GetTypeSyntaxFromDeclaration(declarationStatement) as PredefinedTypeSyntax;

                return predefinedType != null
                    ? SyntaxFacts.IsPredefinedType(predefinedType.Keyword.Kind())
                    : false;
            }

            private bool IsInferredPredefinedType(SyntaxNode declarationStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                TypeSyntax typeSyntax = GetTypeSyntaxFromDeclaration(declarationStatement);

                return typeSyntax != null
                     ? typeSyntax.IsTypeInferred(semanticModel) &&
                        semanticModel.GetTypeInfo(typeSyntax).Type?.IsSpecialType() == true
                     : false;
            }

            private TypeSyntax GetTypeSyntaxFromDeclaration(SyntaxNode declarationStatement)
            {
                if (declarationStatement is VariableDeclarationSyntax)
                {
                    return ((VariableDeclarationSyntax)declarationStatement).Type;
                }
                else if (declarationStatement is ForEachStatementSyntax)
                {
                    return ((ForEachStatementSyntax)declarationStatement).Type;
                }

                return null;
            }

            private TypeStyle GetCurrentTypingStylePreferences(OptionSet optionSet)
            {
                var stylePreferences = TypeStyle.None;

                var styleForIntrinsicTypes = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes);
                var styleForApparent = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent);
                var styleForElsewhere = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible);

                _styleToSeverityMap.Add(TypeStyle.ImplicitTypeForIntrinsicTypes, styleForIntrinsicTypes.Notification.Value);
                _styleToSeverityMap.Add(TypeStyle.ImplicitTypeWhereApparent, styleForApparent.Notification.Value);
                _styleToSeverityMap.Add(TypeStyle.ImplicitTypeWherePossible, styleForElsewhere.Notification.Value);

                if (styleForIntrinsicTypes.IsChecked)
                {
                    stylePreferences |= TypeStyle.ImplicitTypeForIntrinsicTypes;
                }

                if (styleForApparent.IsChecked)
                {
                    stylePreferences |= TypeStyle.ImplicitTypeWhereApparent;
                }

                if (styleForElsewhere.IsChecked)
                {
                    stylePreferences |= TypeStyle.ImplicitTypeWherePossible;
                }

                return stylePreferences;
            }
        }
    }
}
