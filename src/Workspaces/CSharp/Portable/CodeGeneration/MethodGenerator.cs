// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class MethodGenerator
    {
        private static readonly TypeParameterConstraintSyntax s_classConstraint = SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint);
        private static readonly TypeParameterConstraintSyntax s_structConstraint = SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint);
        private static readonly TypeParameterConstraintSyntax s_defaultConstraint = SyntaxFactory.DefaultConstraint();

        internal static BaseNamespaceDeclarationSyntax AddMethodTo(
            BaseNamespaceDeclarationSyntax destination,
            IMethodSymbol method,
            CSharpCodeGenerationOptions options,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateMethodDeclaration(method, CodeGenerationDestination.Namespace, options, cancellationToken);

            var members = Insert(destination.Members, declaration, options, availableIndices, after: LastMethod);
            return destination.WithMembers(members.ToSyntaxList());
        }

        internal static CompilationUnitSyntax AddMethodTo(
            CompilationUnitSyntax destination,
            IMethodSymbol method,
            CSharpCodeGenerationOptions options,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateMethodDeclaration(
                method, CodeGenerationDestination.CompilationUnit, options,
                cancellationToken);

            var members = Insert(destination.Members, declaration, options, availableIndices, after: LastMethod);
            return destination.WithMembers(members.ToSyntaxList());
        }

        internal static TypeDeclarationSyntax AddMethodTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol method,
            CSharpCodeGenerationOptions options,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = GenerateMethodDeclaration(
                method, GetDestination(destination), options, cancellationToken);

            // Create a clone of the original type with the new method inserted. 
            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastMethod);

            return AddMembersTo(destination, members, cancellationToken);
        }

        public static MethodDeclarationSyntax GenerateMethodDeclaration(
            IMethodSymbol method, CodeGenerationDestination destination,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<MethodDeclarationSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = GenerateMethodDeclarationWorker(
                method, destination, options);

            return AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options, cancellationToken));
        }

        public static LocalFunctionStatementSyntax GenerateLocalFunctionDeclaration(
            IMethodSymbol method,
            CodeGenerationDestination destination,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<LocalFunctionStatementSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = GenerateLocalFunctionDeclarationWorker(
                method, destination, options);

            return AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options, cancellationToken));
        }

        private static MethodDeclarationSyntax GenerateMethodDeclarationWorker(
            IMethodSymbol method, CodeGenerationDestination destination,
            CSharpCodeGenerationOptions options)
        {
            // Don't rely on destination to decide if method body should be generated.
            // Users of this service need to express their intention explicitly, either by  
            // setting `CodeGenerationOptions.GenerateMethodBodies` to true, or making 
            // `method` abstract. This would provide more flexibility.
            var hasNoBody = !options.Context.GenerateMethodBodies || method.IsAbstract;

            var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(method.ExplicitInterfaceImplementations);

            var methodDeclaration = SyntaxFactory.MethodDeclaration(
                attributeLists: GenerateAttributes(method, options, explicitInterfaceSpecifier != null),
                modifiers: GenerateModifiers(method, destination, options),
                returnType: method.GenerateReturnTypeSyntax(),
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                identifier: method.Name.ToIdentifierToken(),
                typeParameterList: GenerateTypeParameterList(method, options),
                parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, explicitInterfaceSpecifier != null, options),
                constraintClauses: GenerateConstraintClauses(method),
                body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
                expressionBody: null,
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default);

            methodDeclaration = UseExpressionBodyIfDesired(options, methodDeclaration);
            return AddFormatterAndCodeGeneratorAnnotationsTo(methodDeclaration);
        }

        private static LocalFunctionStatementSyntax GenerateLocalFunctionDeclarationWorker(
            IMethodSymbol method, CodeGenerationDestination destination,
            CSharpCodeGenerationOptions options)
        {
            var localFunctionDeclaration = SyntaxFactory.LocalFunctionStatement(
                modifiers: GenerateModifiers(method, destination, options),
                returnType: method.GenerateReturnTypeSyntax(),
                identifier: method.Name.ToIdentifierToken(),
                typeParameterList: GenerateTypeParameterList(method, options),
                parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: false, options),
                constraintClauses: GenerateConstraintClauses(method),
                body: StatementGenerator.GenerateBlock(method),
                expressionBody: null,
                semicolonToken: default);

            localFunctionDeclaration = UseExpressionBodyIfDesired(options, localFunctionDeclaration);
            return AddFormatterAndCodeGeneratorAnnotationsTo(localFunctionDeclaration);
        }

        private static MethodDeclarationSyntax UseExpressionBodyIfDesired(
            CSharpCodeGenerationOptions options, MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.ExpressionBody == null)
            {
                if (methodDeclaration.Body?.TryConvertToArrowExpressionBody(
                    methodDeclaration.Kind(), options.Preferences.LanguageVersion, options.Preferences.PreferExpressionBodiedMethods,
                    out var expressionBody, out var semicolonToken) == true)
                {
                    return methodDeclaration.WithBody(null)
                                            .WithExpressionBody(expressionBody)
                                            .WithSemicolonToken(semicolonToken);
                }
            }

            return methodDeclaration;
        }

        private static LocalFunctionStatementSyntax UseExpressionBodyIfDesired(
            CSharpCodeGenerationOptions options, LocalFunctionStatementSyntax localFunctionDeclaration)
        {
            if (localFunctionDeclaration.ExpressionBody == null)
            {
                if (localFunctionDeclaration.Body?.TryConvertToArrowExpressionBody(
                    localFunctionDeclaration.Kind(), options.Preferences.LanguageVersion, options.Preferences.PreferExpressionBodiedLocalFunctions,
                    out var expressionBody, out var semicolonToken) == true)
                {
                    return localFunctionDeclaration.WithBody(null)
                                                 .WithExpressionBody(expressionBody)
                                                 .WithSemicolonToken(semicolonToken);
                }
            }

            return localFunctionDeclaration;
        }

        private static SyntaxList<AttributeListSyntax> GenerateAttributes(
            IMethodSymbol method, CSharpCodeGenerationOptions options, bool isExplicit)
        {
            var attributes = new List<AttributeListSyntax>();

            if (!isExplicit)
            {
                attributes.AddRange(AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), options));
                attributes.AddRange(AttributeGenerator.GenerateAttributeLists(method.GetReturnTypeAttributes(), options, SyntaxFactory.Token(SyntaxKind.ReturnKeyword)));
            }

            return attributes.ToSyntaxList();
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> GenerateConstraintClauses(
            IMethodSymbol method)
        {
            return !method.ExplicitInterfaceImplementations.Any() && !method.IsOverride
                ? method.TypeParameters.GenerateConstraintClauses()
                : GenerateDefaultConstraints(method);
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> GenerateDefaultConstraints(IMethodSymbol method)
        {
            Debug.Assert(method.ExplicitInterfaceImplementations.Any() || method.IsOverride);

            using var _1 = PooledHashSet<string>.GetInstance(out var seenTypeParameters);
            using var _2 = ArrayBuilder<TypeParameterConstraintClauseSyntax>.GetInstance(out var listOfClauses);
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Type is not ITypeParameterSymbol { NullableAnnotation: NullableAnnotation.Annotated } typeParameter)
                {
                    continue;
                }

                if (!seenTypeParameters.Add(parameter.Type.Name))
                {
                    continue;
                }

                var constraint = typeParameter switch
                {
                    { HasReferenceTypeConstraint: true } => s_classConstraint,
                    { HasValueTypeConstraint: true } => s_structConstraint,
                    _ => s_defaultConstraint
                };

                listOfClauses.Add(SyntaxFactory.TypeParameterConstraintClause(
                    name: parameter.Type.Name.ToIdentifierName(),
                    constraints: SyntaxFactory.SingletonSeparatedList(constraint)));
            }

            return SyntaxFactory.List(listOfClauses);
        }

        private static TypeParameterListSyntax? GenerateTypeParameterList(
            IMethodSymbol method, CSharpCodeGenerationOptions options)
        {
            return TypeParameterGenerator.GenerateTypeParameterList(method.TypeParameters, options);
        }

        private static SyntaxTokenList GenerateModifiers(
            IMethodSymbol method, CodeGenerationDestination destination, CSharpCodeGenerationOptions options)
        {
            var tokens = ArrayBuilder<SyntaxToken>.GetInstance();

            // Only "static" and "unsafe" modifiers allowed if we're an explicit impl.
            if (method.ExplicitInterfaceImplementations.Any())
            {
                if (method.IsStatic)
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                }

                if (CodeGenerationMethodInfo.GetIsUnsafe(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
                }
            }
            else
            {
                // If we're generating into an interface, then we don't use any modifiers.
                if (destination is not CodeGenerationDestination.CompilationUnit and
                    not CodeGenerationDestination.Namespace and
                    not CodeGenerationDestination.InterfaceType)
                {
                    AddAccessibilityModifiers(method.DeclaredAccessibility, tokens, options, Accessibility.Private);

                    if (method.IsStatic)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    }

                    if (method.IsAbstract)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
                    }

                    if (method.IsSealed)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
                    }

                    // Don't show the readonly modifier if the containing type is already readonly
                    // ContainingSymbol is used to guard against methods which are not members of their ContainingType (e.g. lambdas and local functions)
                    if (method.IsReadOnly && (method.ContainingSymbol as INamedTypeSymbol)?.IsReadOnly != true)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                    }

                    if (method.IsOverride)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
                    }

                    if (method.IsVirtual)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
                    }

                    if (CodeGenerationMethodInfo.GetIsPartial(method) && !method.IsAsync)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                    }
                }
                else if (destination is CodeGenerationDestination.CompilationUnit)
                {
                    if (method.IsStatic)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    }
                }

                if (CodeGenerationMethodInfo.GetIsUnsafe(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
                }

                if (CodeGenerationMethodInfo.GetIsNew(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.NewKeyword));
                }
            }

            if (destination != CodeGenerationDestination.InterfaceType)
            {
                if (CodeGenerationMethodInfo.GetIsAsyncMethod(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
                }
            }

            if (CodeGenerationMethodInfo.GetIsPartial(method) && method.IsAsync)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }

            return tokens.ToSyntaxTokenListAndFree();
        }
    }
}
