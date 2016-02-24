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

                IsInIntrinsicTypeContext = IsIntrinsicType(declaration);
            }

            /// <summary>
            /// Returns true if type information could be gleaned by simply looking at the given statement.
            /// This typically means that the type name occurs in either left hand or right hand side of an assignment.
            /// </summary>
            private bool IsTypeApparentInDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, TypeStyle stylePreferences, CancellationToken cancellationToken)
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
                    return stylePreferences.HasFlag(TypeStyle.ImplicitTypeForIntrinsicTypes);
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
                //      a. conversion with helpers like: int.Parse methods
                //      b. types that implement IConvertible and then invoking .ToType()
                //      c. System.Convert.Totype()
                var declaredTypeSymbol = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;

                var memberName = initializerExpression.GetRightmostName();
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
                (declarationStatement as VariableDeclarationSyntax)?.Variables.Single().Initializer.Value.IsAnyLiteralExpression() == true;

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

            /// <summary>
            /// Looks for types that have static methods that return the same type as the container.
            /// e.g: int.Parse, XElement.Load, Tuple.Create etc.
            /// </summary>
            private bool IsPossibleCreationMethod(IMethodSymbol methodSymbol, ITypeSymbol declaredType, ITypeSymbol typeInInvocation)
            {
                if (!methodSymbol.IsStatic)
                {
                    return false;
                }

                return IsDeclaredTypeEqualToReturnType(methodSymbol, declaredType, typeInInvocation);
            }

            /// <summary>
            /// If we have a method ToXXX and its return type is also XXX, then type name is apparent
            /// e.g: Convert.ToString.
            /// </summary>
            private bool IsPossibleConversionMethod(IMethodSymbol methodSymbol, ITypeSymbol declaredType, ITypeSymbol typeInInvocation, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                // take `char` from `char? c = `
                var declaredTypeName = declaredType.IsNullable()
                        ? declaredType.GetTypeArguments().First().Name
                        : declaredType.Name;

                var returnType = methodSymbol.ReturnType;

                if (methodSymbol.Name.Equals("To" + declaredTypeName, StringComparison.Ordinal))
                {
                    return IsDeclaredTypeEqualToReturnType(methodSymbol, declaredType, typeInInvocation);
                }

                return false;
            }

            /// <remarks>
            /// If there are type arguments on either side of assignment, we match type names instead of type equality 
            /// to account for inferred generic type arguments.
            /// e.g: Tuple.Create(0, true) returns Tuple&lt;X,y&gt; which isn't the same as type Tuple.
            /// otherwise, we match for type equivalence
            /// </remarks>
            private static bool IsDeclaredTypeEqualToReturnType(IMethodSymbol methodSymbol, ITypeSymbol declaredType, ITypeSymbol typeInInvocation)
            {
                var returnType = methodSymbol.ReturnType;

                if (declaredType.GetTypeArguments().Length > 0 ||
                    typeInInvocation.GetTypeArguments().Length > 0)
                {
                    return declaredType.Name.Equals(returnType.Name);
                }
                else
                {
                    return declaredType.Equals(returnType);
                }
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
