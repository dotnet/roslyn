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
            private readonly Dictionary<UseVarPreference, ReportDiagnostic> _styleToSeverityMap;

            public UseVarPreference TypeStylePreference { get; private set; }
            public bool IsInIntrinsicTypeContext { get; private set; }
            public bool IsTypeApparentInContext { get; private set; }
            public bool IsInVariableDeclarationContext { get; }

            private State(bool isVariableDeclarationContext)
            {
                this.IsInVariableDeclarationContext = isVariableDeclarationContext;
                _styleToSeverityMap = new Dictionary<UseVarPreference, ReportDiagnostic>();
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
                    return _styleToSeverityMap[UseVarPreference.ForBuiltInTypes];
                }
                else if (IsTypeApparentInContext)
                {
                    return _styleToSeverityMap[UseVarPreference.WhenTypeIsApparent];
                }
                else
                {
                    return _styleToSeverityMap[UseVarPreference.Elsewhere];
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
                     || IsInferredPredefinedType(declaration, semanticModel);
            }

            /// <summary>
            /// Returns true if type information could be gleaned by simply looking at the given statement.
            /// This typically means that the type name occurs in right hand side of an assignment.
            /// </summary>
            private bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, UseVarPreference stylePreferences, CancellationToken cancellationToken)
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

            private bool IsInferredPredefinedType(SyntaxNode declarationStatement, SemanticModel semanticModel)
            {
                var typeSyntax = GetTypeSyntaxFromDeclaration(declarationStatement);

                return typeSyntax != null &&
                    typeSyntax.IsTypeInferred(semanticModel) &&
                    semanticModel.GetTypeInfo(typeSyntax).Type?.IsSpecialType() == true;
            }

            private TypeSyntax GetTypeSyntaxFromDeclaration(SyntaxNode declarationStatement)
                => declarationStatement switch
                {
                    VariableDeclarationSyntax varDecl => varDecl.Type,
                    ForEachStatementSyntax forEach => forEach.Type,
                    DeclarationExpressionSyntax declExpr => declExpr.Type,
                    _ => null,
                };

            private UseVarPreference GetCurrentTypeStylePreferences(OptionSet optionSet)
            {
                var stylePreferences = UseVarPreference.None;

                var styleForIntrinsicTypes = optionSet.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes);
                var styleForApparent = optionSet.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent);
                var styleForElsewhere = optionSet.GetOption(CSharpCodeStyleOptions.VarElsewhere);

                _styleToSeverityMap.Add(UseVarPreference.ForBuiltInTypes, styleForIntrinsicTypes.Notification.Severity);
                _styleToSeverityMap.Add(UseVarPreference.WhenTypeIsApparent, styleForApparent.Notification.Severity);
                _styleToSeverityMap.Add(UseVarPreference.Elsewhere, styleForElsewhere.Notification.Severity);

                if (styleForIntrinsicTypes.Value)
                {
                    stylePreferences |= UseVarPreference.ForBuiltInTypes;
                }

                if (styleForApparent.Value)
                {
                    stylePreferences |= UseVarPreference.WhenTypeIsApparent;
                }

                if (styleForElsewhere.Value)
                {
                    stylePreferences |= UseVarPreference.Elsewhere;
                }

                return stylePreferences;
            }
        }
    }
}
