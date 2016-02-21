// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypingStyles
{
    internal partial class CSharpTypingStyleDiagnosticAnalyzerBase
    {
        internal class State
        {
            private readonly Dictionary<TypingStyles, DiagnosticSeverity> _styleToSeverityMap;

            public TypingStyles StylePreferences { get; private set; }
            public bool IsInIntrinsicTypeContext { get; private set; }
            public bool IsTypingApparentInContext { get; private set; }
            public bool IsInVariableDeclarationContext { get; }

            public State(bool isVariableDeclarationContext)
            {
                this.IsInVariableDeclarationContext = isVariableDeclarationContext;
                _styleToSeverityMap = new Dictionary<TypingStyles, DiagnosticSeverity>();
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
                    return _styleToSeverityMap[TypingStyles.VarForIntrinsic];
                }
                else if (IsTypingApparentInContext)
                {
                    return _styleToSeverityMap[TypingStyles.VarWhereApparent];
                }
                else
                {
                    return _styleToSeverityMap[TypingStyles.VarWherePossible];
                }
            }

            public bool ShouldNotify() => 
                GetDiagnosticSeverityPreference() != DiagnosticSeverity.Hidden;

            private void Initialize(SyntaxNode declaration, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            {
                this.StylePreferences = GetCurrentTypingStylePreferences(optionSet);

                IsTypingApparentInContext =
                    IsInVariableDeclarationContext
                        ? IsTypeApparentInDeclaration((VariableDeclarationSyntax)declaration,
                            semanticModel, StylePreferences, cancellationToken)
                        : false;

                IsInIntrinsicTypeContext = IsIntrinsicType(declaration);
            }

            /// <summary>
            /// Returns true if type information could be gleaned by simply looking at the given statement.
            /// This typically means that the type name occurs in either left hand or right hand side of an assignment.
            /// </summary>
            private bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, TypingStyles stylePreferences, CancellationToken cancellationToken)
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
                    return IsPossibleCreationOrConversionMethod(methodSymbol, declaredTypeSymbol, semanticModel, typeName, cancellationToken);
                }

                return false;
            }

            private bool IsIntrinsicType(SyntaxNode declarationStatement) =>
                declarationStatement.IsKind(SyntaxKind.VariableDeclaration)
                ? ((VariableDeclarationSyntax)declarationStatement).Variables.Single().Initializer.Value.IsAnyLiteralExpression()
                : false;

            private ExpressionSyntax GetInitializerExpression(EqualsValueClauseSyntax initializer) =>
                initializer.Value is CheckedExpressionSyntax
                    ? ((CheckedExpressionSyntax)initializer.Value).Expression
                    : initializer.Value;

            private bool IsPossibleCreationOrConversionMethod(IMethodSymbol methodSymbol, ITypeSymbol declaredType, SemanticModel semanticModel, ExpressionSyntax typeName, CancellationToken cancellationToken)
            {
                if (methodSymbol.ReturnsVoid)
                {
                    return false;
                }

                var typeInInvocation = semanticModel.GetTypeInfo(typeName, cancellationToken).Type;

                return IsPossibleCreationMethod(methodSymbol, declaredType, typeInInvocation)
                    || IsPossibleConversionMethod(methodSymbol, declaredType, typeInInvocation, semanticModel, cancellationToken);
            }

            private bool IsPossibleCreationMethod(IMethodSymbol methodSymbol, ITypeSymbol declaredType, ITypeSymbol typeInInvocation)
            {
                var isTypeInfoSame = false;

                // Pattern: Method name is a prefix match of one of these well known method names
                //          and method is a member of type being created.
                // cases: int.ParseXXX, TextSpan.FromBounds, 
                if (methodSymbol.Name.StartsWith("Parse", StringComparison.Ordinal)
                    || methodSymbol.Name.StartsWith("From", StringComparison.Ordinal))
                {
                    isTypeInfoSame = typeInInvocation.Equals(declaredType);
                }

                // Pattern: Method name is an exact match of one of these well known creation methods
                //          and method is a member of type being created.
                // cases: node.With, Tuple.Create, State.Generate
                if (methodSymbol.Name.Equals("With", StringComparison.Ordinal)
                    || methodSymbol.Name.Equals("Create", StringComparison.Ordinal)
                    || methodSymbol.Name.Equals("Generate", StringComparison.Ordinal))
                {
                    isTypeInfoSame = typeInInvocation.Equals(declaredType);
                }

                return isTypeInfoSame;
            }

            private bool IsPossibleConversionMethod(IMethodSymbol methodSymbol, ITypeSymbol declaredType, ITypeSymbol typeInInvocation, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
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

            private TypingStyles GetCurrentTypingStylePreferences(OptionSet optionSet)
            {
                var stylePreferences = TypingStyles.None;

                var styleForIntrinsicTypes = optionSet.GetOption(CSharpCodeStyleOptions.UseVarForIntrinsicTypes);
                var styleForApparent = optionSet.GetOption(CSharpCodeStyleOptions.UseVarWhenTypeIsApparent);
                var styleForElsewhere = optionSet.GetOption(CSharpCodeStyleOptions.UseVarWherePossible);

                _styleToSeverityMap.Add(TypingStyles.VarForIntrinsic, styleForIntrinsicTypes.Notification.Value);
                _styleToSeverityMap.Add(TypingStyles.VarWhereApparent, styleForApparent.Notification.Value);
                _styleToSeverityMap.Add(TypingStyles.VarWherePossible, styleForElsewhere.Notification.Value);

                if (styleForIntrinsicTypes.IsChecked)
                {
                    stylePreferences |= TypingStyles.VarForIntrinsic;
                }

                if (styleForApparent.IsChecked)
                {
                    stylePreferences |= TypingStyles.VarWhereApparent;
                }

                if (styleForElsewhere.IsChecked)
                {
                    stylePreferences |= TypingStyles.VarWherePossible;
                }

                return stylePreferences;
            }
        }
    }
}
