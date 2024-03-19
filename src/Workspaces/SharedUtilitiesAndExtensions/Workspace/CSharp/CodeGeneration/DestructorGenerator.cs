// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

internal static class DestructorGenerator
{
    private static MemberDeclarationSyntax? LastConstructorOrField(SyntaxList<MemberDeclarationSyntax> members)
        => LastConstructor(members) ?? LastField(members);

    internal static TypeDeclarationSyntax AddDestructorTo(
        TypeDeclarationSyntax destination,
        IMethodSymbol destructor,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var destructorDeclaration = GenerateDestructorDeclaration(destructor, info, cancellationToken);

        // Generate after the last constructor, or after the last field, or at the start of the
        // type.
        var members = Insert(destination.Members, destructorDeclaration, info,
            availableIndices, after: LastConstructorOrField, before: FirstMember);

        return AddMembersTo(destination, members, cancellationToken);
    }

    internal static DestructorDeclarationSyntax GenerateDestructorDeclaration(
        IMethodSymbol destructor, CSharpCodeGenerationContextInfo info, CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<DestructorDeclarationSyntax>(destructor, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var hasNoBody = !info.Context.GenerateMethodBodies;

        var declaration = SyntaxFactory.DestructorDeclaration(
            attributeLists: AttributeGenerator.GenerateAttributeLists(destructor.GetAttributes(), info),
            modifiers: default,
            tildeToken: SyntaxFactory.Token(SyntaxKind.TildeToken),
            identifier: CodeGenerationDestructorInfo.GetTypeName(destructor).ToIdentifierToken(),
            parameterList: SyntaxFactory.ParameterList(),
            body: hasNoBody ? null : GenerateBlock(destructor),
            semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default);

        return AddFormatterAndCodeGeneratorAnnotationsTo(
            ConditionallyAddDocumentationCommentTo(declaration, destructor, info, cancellationToken));
    }

    private static BlockSyntax GenerateBlock(
        IMethodSymbol constructor)
    {
        var statements = CodeGenerationDestructorInfo.GetStatements(constructor) == null
            ? default
            : StatementGenerator.GenerateStatements(CodeGenerationDestructorInfo.GetStatements(constructor));

        return SyntaxFactory.Block(statements);
    }
}
