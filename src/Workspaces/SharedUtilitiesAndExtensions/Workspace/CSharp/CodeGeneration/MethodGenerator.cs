// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CodeGenerationHelpers;
using static CSharpCodeGenerationHelpers;
using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class MethodGenerator
{
    private static readonly TypeParameterConstraintSyntax s_classConstraint = ClassOrStructConstraint(SyntaxKind.ClassConstraint);
    private static readonly TypeParameterConstraintSyntax s_structConstraint = ClassOrStructConstraint(SyntaxKind.StructConstraint);
    private static readonly TypeParameterConstraintSyntax s_defaultConstraint = DefaultConstraint();

    internal static BaseNamespaceDeclarationSyntax AddMethodTo(
        BaseNamespaceDeclarationSyntax destination,
        IMethodSymbol method,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var declaration = GenerateMethodDeclaration(method, CodeGenerationDestination.Namespace, info, cancellationToken);

        var members = Insert(destination.Members, declaration, info, availableIndices, after: LastMethod);
        return destination.WithMembers(List(members));
    }

    internal static CompilationUnitSyntax AddMethodTo(
        CompilationUnitSyntax destination,
        IMethodSymbol method,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var declaration = GenerateMethodDeclaration(
            method, CodeGenerationDestination.CompilationUnit, info,
            cancellationToken);

        var members = Insert(destination.Members, declaration, info, availableIndices, after: LastMethod);
        return destination.WithMembers(List(members));
    }

    internal static TypeDeclarationSyntax AddMethodTo(
        TypeDeclarationSyntax destination,
        IMethodSymbol method,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var methodDeclaration = GenerateMethodDeclaration(
            method, GetDestination(destination), info, cancellationToken);

        // Create a clone of the original type with the new method inserted. 
        var members = Insert(destination.Members, methodDeclaration, info, availableIndices, after: LastMethod);

        return AddMembersTo(destination, members, cancellationToken);
    }

    public static MethodDeclarationSyntax GenerateMethodDeclaration(
        IMethodSymbol method, CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<MethodDeclarationSyntax>(method, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var declaration = GenerateMethodDeclarationWorker(
            method, destination, info, cancellationToken);

        return AddAnnotationsTo(method,
            ConditionallyAddDocumentationCommentTo(declaration, method, info, cancellationToken));
    }

    public static LocalFunctionStatementSyntax GenerateLocalFunctionDeclaration(
        IMethodSymbol method,
        CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<LocalFunctionStatementSyntax>(method, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var declaration = GenerateLocalFunctionDeclarationWorker(
            method, destination, info, cancellationToken);

        return AddAnnotationsTo(method,
            ConditionallyAddDocumentationCommentTo(declaration, method, info, cancellationToken));
    }

    private static MethodDeclarationSyntax GenerateMethodDeclarationWorker(
        IMethodSymbol method, CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info, CancellationToken cancellationToken)
    {
        // Don't rely on destination to decide if method body should be generated.
        // Users of this service need to express their intention explicitly, either by  
        // setting `CodeGenerationOptions.GenerateMethodBodies` to true, or making 
        // `method` abstract. This would provide more flexibility.
        var hasNoBody = !info.Context.GenerateMethodBodies || method.IsAbstract;

        var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(method.ExplicitInterfaceImplementations);

        var methodDeclaration = MethodDeclaration(
            attributeLists: GenerateAttributes(method, info, explicitInterfaceSpecifier != null),
            modifiers: GenerateModifiers(method, destination, info),
            returnType: method.GenerateReturnTypeSyntax(),
            explicitInterfaceSpecifier: explicitInterfaceSpecifier,
            identifier: method.Name.ToIdentifierToken(),
            typeParameterList: GenerateTypeParameterList(method, info),
            parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, explicitInterfaceSpecifier != null, info),
            constraintClauses: GenerateConstraintClauses(method),
            body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
            expressionBody: null,
            semicolonToken: hasNoBody ? SemicolonToken : default);

        methodDeclaration = UseExpressionBodyIfDesired(info, methodDeclaration, cancellationToken);
        return AddFormatterAndCodeGeneratorAnnotationsTo(methodDeclaration);
    }

    private static LocalFunctionStatementSyntax GenerateLocalFunctionDeclarationWorker(
        IMethodSymbol method, CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info, CancellationToken cancellationToken)
    {
        var localFunctionDeclaration = LocalFunctionStatement(
            modifiers: GenerateModifiers(method, destination, info),
            returnType: method.GenerateReturnTypeSyntax(),
            identifier: method.Name.ToIdentifierToken(),
            typeParameterList: GenerateTypeParameterList(method, info),
            parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: false, info),
            constraintClauses: GenerateConstraintClauses(method),
            body: StatementGenerator.GenerateBlock(method),
            expressionBody: null,
            semicolonToken: default);

        localFunctionDeclaration = UseExpressionBodyIfDesired(info, localFunctionDeclaration, cancellationToken);
        return AddFormatterAndCodeGeneratorAnnotationsTo(localFunctionDeclaration);
    }

    private static MethodDeclarationSyntax UseExpressionBodyIfDesired(
        CSharpCodeGenerationContextInfo info, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        if (methodDeclaration.ExpressionBody == null)
        {
            if (methodDeclaration.Body?.TryConvertToArrowExpressionBody(
                methodDeclaration.Kind(), info.LanguageVersion, info.Options.PreferExpressionBodiedMethods.Value, cancellationToken,
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
        CSharpCodeGenerationContextInfo info, LocalFunctionStatementSyntax localFunctionDeclaration, CancellationToken cancellationToken)
    {
        if (localFunctionDeclaration.ExpressionBody == null)
        {
            if (localFunctionDeclaration.Body?.TryConvertToArrowExpressionBody(
                localFunctionDeclaration.Kind(), info.LanguageVersion, info.Options.PreferExpressionBodiedLocalFunctions.Value, cancellationToken,
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
        IMethodSymbol method, CSharpCodeGenerationContextInfo info, bool isExplicit)
    {
        var attributes = new List<AttributeListSyntax>();

        if (!isExplicit)
        {
            attributes.AddRange(AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), info));
            attributes.AddRange(AttributeGenerator.GenerateAttributeLists(method.GetReturnTypeAttributes(), info, ReturnKeyword));
        }

        return List(attributes);
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

        using var _ = ArrayBuilder<TypeParameterConstraintClauseSyntax>.GetInstance(out var listOfClauses);

        var referencedTypeParameters = method.Parameters
            .SelectMany(p => p.Type.GetReferencedTypeParameters())
            .Concat(method.ReturnType.GetReferencedTypeParameters())
            .Where(tp => tp.NullableAnnotation == NullableAnnotation.Annotated)
            .ToImmutableHashSet();

        foreach (var typeParameter in method.TypeParameters)
        {
            if (!referencedTypeParameters.Contains(typeParameter))
                continue;

            var constraint = typeParameter switch
            {
                { HasReferenceTypeConstraint: true } => s_classConstraint,
                { HasValueTypeConstraint: true } => s_structConstraint,
                _ => s_defaultConstraint
            };

            listOfClauses.Add(TypeParameterConstraintClause(
                typeParameter.Name.ToIdentifierName(),
                SeparatedList<TypeParameterConstraintSyntax>().Add(constraint)));
        }

        return List(listOfClauses);
    }

    private static TypeParameterListSyntax? GenerateTypeParameterList(
        IMethodSymbol method, CSharpCodeGenerationContextInfo info)
    {
        return TypeParameterGenerator.GenerateTypeParameterList(method.TypeParameters, info);
    }

    private static SyntaxTokenList GenerateModifiers(
        IMethodSymbol method, CodeGenerationDestination destination, CSharpCodeGenerationContextInfo info)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var tokens);

        // Only "static" and "unsafe" modifiers allowed if we're an explicit impl.
        if (method.ExplicitInterfaceImplementations.Any())
        {
            if (method.IsStatic)
                tokens.Add(StaticKeyword);

            if (CodeGenerationMethodInfo.GetIsUnsafe(method))
                tokens.Add(UnsafeKeyword);
        }
        else
        {
            // If we're generating into an interface, then allow modifiers for static abstract members
            if (destination is CodeGenerationDestination.InterfaceType)
            {
                if (method.IsStatic)
                {
                    tokens.Add(StaticKeyword);

                    // We only generate the abstract keyword in interfaces for static abstract members
                    if (method.IsAbstract)
                        tokens.Add(AbstractKeyword);
                }
            }
            else if (destination is not CodeGenerationDestination.CompilationUnit and
                not CodeGenerationDestination.Namespace)
            {
                AddAccessibilityModifiers(method.DeclaredAccessibility, tokens, info, Accessibility.Private);

                if (method.IsStatic)
                    tokens.Add(StaticKeyword);

                if (method.IsAbstract)
                    tokens.Add(AbstractKeyword);

                if (method.IsSealed)
                    tokens.Add(SealedKeyword);

                // Don't show the readonly modifier if the containing type is already readonly
                // ContainingSymbol is used to guard against methods which are not members of their ContainingType (e.g. lambdas and local functions)
                if (method.IsReadOnly && (method.ContainingSymbol as INamedTypeSymbol)?.IsReadOnly != true)
                    tokens.Add(ReadOnlyKeyword);

                if (method.IsOverride)
                    tokens.Add(OverrideKeyword);

                if (method.IsVirtual)
                    tokens.Add(VirtualKeyword);

                if (CodeGenerationMethodInfo.GetIsPartial(method) && !method.IsAsync)
                    tokens.Add(PartialKeyword);
            }
            else if (destination is CodeGenerationDestination.CompilationUnit)
            {
                if (method.IsStatic)
                    tokens.Add(StaticKeyword);
            }

            if (CodeGenerationMethodInfo.GetIsUnsafe(method))
                tokens.Add(UnsafeKeyword);

            if (CodeGenerationMethodInfo.GetIsNew(method))
                tokens.Add(NewKeyword);
        }

        if (destination != CodeGenerationDestination.InterfaceType)
        {
            if (CodeGenerationMethodInfo.GetIsAsyncMethod(method))
                tokens.Add(AsyncKeyword);
        }

        if (CodeGenerationMethodInfo.GetIsPartial(method) && method.IsAsync)
            tokens.Add(PartialKeyword);

        return [.. tokens];
    }
}
