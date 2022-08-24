// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
    internal static class ConstructorGenerator
    {
        private static MemberDeclarationSyntax? LastConstructorOrField(SyntaxList<MemberDeclarationSyntax> members)
            => LastConstructor(members) ?? LastField(members);

        internal static TypeDeclarationSyntax AddConstructorTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol constructor,
            CSharpCodeGenerationOptions options,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var constructorDeclaration = GenerateConstructorDeclaration(constructor, options, cancellationToken);

            // Generate after the last constructor, or after the last field, or at the start of the
            // type.
            var members = Insert(destination.Members, constructorDeclaration, options,
                availableIndices, after: LastConstructorOrField, before: FirstMember);

            return AddMembersTo(destination, members, cancellationToken);
        }

        internal static ConstructorDeclarationSyntax GenerateConstructorDeclaration(
            IMethodSymbol constructor,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<ConstructorDeclarationSyntax>(constructor, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var hasNoBody = !options.Context.GenerateMethodBodies;

            var declaration = SyntaxFactory.ConstructorDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(constructor.GetAttributes(), options),
                modifiers: GenerateModifiers(constructor, options),
                identifier: CodeGenerationConstructorInfo.GetTypeName(constructor).ToIdentifierToken(),
                parameterList: ParameterGenerator.GenerateParameterList(constructor.Parameters, isExplicit: false, options: options),
                initializer: GenerateConstructorInitializer(constructor),
                body: hasNoBody ? null : GenerateBlock(constructor),
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default);

            declaration = UseExpressionBodyIfDesired(options, declaration);

            return AddFormatterAndCodeGeneratorAnnotationsTo(
                ConditionallyAddDocumentationCommentTo(declaration, constructor, options, cancellationToken));
        }

        private static ConstructorDeclarationSyntax UseExpressionBodyIfDesired(
            CSharpCodeGenerationOptions options, ConstructorDeclarationSyntax declaration)
        {
            if (declaration.ExpressionBody == null)
            {
                if (declaration.Body?.TryConvertToArrowExpressionBody(
                    declaration.Kind(), options.Preferences.LanguageVersion, options.Preferences.PreferExpressionBodiedConstructors,
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
                : SyntaxFactory.ConstructorInitializer(kind).WithArgumentList(GenerateArgumentList(arguments));
        }

        private static ArgumentListSyntax GenerateArgumentList(ImmutableArray<SyntaxNode> arguments)
            => SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(ArgumentGenerator.GenerateArgument)));

        private static BlockSyntax GenerateBlock(
            IMethodSymbol constructor)
        {
            var statements = CodeGenerationConstructorInfo.GetStatements(constructor) == null
                ? default
                : StatementGenerator.GenerateStatements(CodeGenerationConstructorInfo.GetStatements(constructor));

            return SyntaxFactory.Block(statements);
        }

        private static SyntaxTokenList GenerateModifiers(IMethodSymbol constructor, CSharpCodeGenerationOptions options)
        {
            var tokens = ArrayBuilder<SyntaxToken>.GetInstance();

            if (constructor.IsStatic)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }
            else
            {
                AddAccessibilityModifiers(constructor.DeclaredAccessibility, tokens, options, Accessibility.Private);
            }

            if (CodeGenerationConstructorInfo.GetIsUnsafe(constructor))
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

            return tokens.ToSyntaxTokenListAndFree();
        }
    }
}
