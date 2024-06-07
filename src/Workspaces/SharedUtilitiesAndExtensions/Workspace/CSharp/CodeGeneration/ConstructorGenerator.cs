// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CodeGenerationHelpers;
using static CSharpCodeGenerationHelpers;
using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class ConstructorGenerator
{
    private static MemberDeclarationSyntax? LastConstructorOrField(SyntaxList<MemberDeclarationSyntax> members)
        => LastConstructor(members) ?? LastField(members);

    internal static TypeDeclarationSyntax AddConstructorTo(
        TypeDeclarationSyntax destination,
        IMethodSymbol constructor,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var constructorDeclaration = GenerateConstructorDeclaration(constructor, info, cancellationToken);

        // Generate after the last constructor, or after the last field, or at the start of the
        // type.
        var members = Insert(destination.Members, constructorDeclaration, info,
            availableIndices, after: LastConstructorOrField, before: FirstMember);

        return AddMembersTo(destination, members, cancellationToken);
    }

    internal static ConstructorDeclarationSyntax GenerateConstructorDeclaration(
        IMethodSymbol constructor,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<ConstructorDeclarationSyntax>(constructor, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var hasNoBody = !info.Context.GenerateMethodBodies;

        var declaration = ConstructorDeclaration(
            attributeLists: AttributeGenerator.GenerateAttributeLists(constructor.GetAttributes(), info),
            modifiers: GenerateModifiers(constructor, info),
            identifier: CodeGenerationConstructorInfo.GetTypeName(constructor).ToIdentifierToken(),
            parameterList: ParameterGenerator.GenerateParameterList(constructor.Parameters, isExplicit: false, info: info),
            initializer: GenerateConstructorInitializer(constructor),
            body: hasNoBody ? null : GenerateBlock(constructor),
            semicolonToken: hasNoBody ? SemicolonToken : default);

        declaration = UseExpressionBodyIfDesired(info, declaration, cancellationToken);

        return AddFormatterAndCodeGeneratorAnnotationsTo(
            ConditionallyAddDocumentationCommentTo(declaration, constructor, info, cancellationToken));
    }

    private static ConstructorDeclarationSyntax UseExpressionBodyIfDesired(
        CSharpCodeGenerationContextInfo info, ConstructorDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        if (declaration.ExpressionBody == null)
        {
            if (declaration.Body?.TryConvertToArrowExpressionBody(
                declaration.Kind(), info.LanguageVersion, info.Options.PreferExpressionBodiedConstructors.Value, cancellationToken,
                out var expressionBody, out var semicolonToken) == true)
            {
                return declaration.WithBody(null)
                                  .WithExpressionBody(expressionBody)
                                  .WithSemicolonToken(semicolonToken);
            }
        }

        return declaration;
    }

    private static ConstructorInitializerSyntax? GenerateConstructorInitializer(
        IMethodSymbol constructor)
    {
        var thisArguments = CodeGenerationConstructorInfo.GetThisConstructorArgumentsOpt(constructor);

        var arguments = !thisArguments.IsDefault ? thisArguments : CodeGenerationConstructorInfo.GetBaseConstructorArgumentsOpt(constructor);
        var kind = CodeGenerationConstructorInfo.GetThisConstructorArgumentsOpt(constructor) != null
            ? SyntaxKind.ThisConstructorInitializer
            : SyntaxKind.BaseConstructorInitializer;

        return arguments == null
            ? null
            : ConstructorInitializer(kind).WithArgumentList(GenerateArgumentList(arguments));
    }

    private static ArgumentListSyntax GenerateArgumentList(ImmutableArray<SyntaxNode> arguments)
        => ArgumentList(SeparatedList(arguments.Select(ArgumentGenerator.GenerateArgument)));

    private static BlockSyntax GenerateBlock(
        IMethodSymbol constructor)
    {
        var statements = CodeGenerationConstructorInfo.GetStatements(constructor) == null
            ? default
            : StatementGenerator.GenerateStatements(CodeGenerationConstructorInfo.GetStatements(constructor));

        return Block(statements);
    }

    private static SyntaxTokenList GenerateModifiers(IMethodSymbol constructor, CSharpCodeGenerationContextInfo info)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var tokens);

        if (constructor.IsStatic)
        {
            tokens.Add(StaticKeyword);
        }
        else
        {
            AddAccessibilityModifiers(constructor.DeclaredAccessibility, tokens, info, Accessibility.Private);
        }

        if (CodeGenerationConstructorInfo.GetIsUnsafe(constructor))
            tokens.Add(UnsafeKeyword);

        return TokenList(tokens);
    }
}
