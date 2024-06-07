// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CodeGenerationHelpers;
using static CSharpCodeGenerationHelpers;
using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class FieldGenerator
{
    private static MemberDeclarationSyntax? LastField(
        SyntaxList<MemberDeclarationSyntax> members,
        FieldDeclarationSyntax fieldDeclaration)
    {
        var lastConst = members.OfType<FieldDeclarationSyntax>()
                               .Where(f => f.Modifiers.Any(SyntaxKind.ConstKeyword))
                               .LastOrDefault();

        // Place a const after the last existing const.  If we don't have a last const
        // we'll just place the const before the first member in the type.
        if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
        {
            return lastConst;
        }

        var lastReadOnly = members.OfType<FieldDeclarationSyntax>()
                                  .Where(f => f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                                  .LastOrDefault();

        var lastNormal = members.OfType<FieldDeclarationSyntax>()
                                .Where(f => !f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) && !f.Modifiers.Any(SyntaxKind.ConstKeyword))
                                .LastOrDefault();

        // Place a readonly field after the last readonly field if we have one.  Otherwise
        // after the last field/const.
        return fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
            ? lastReadOnly ?? lastConst ?? lastNormal
            : lastNormal ?? lastReadOnly ?? lastConst;
    }

    internal static CompilationUnitSyntax AddFieldTo(
        CompilationUnitSyntax destination,
        IFieldSymbol field,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var declaration = GenerateFieldDeclaration(field, info, cancellationToken);

        // Place the field after the last field or const, or at the start of the type
        // declaration.
        var members = Insert(destination.Members, declaration, info, availableIndices,
            after: m => LastField(m, declaration), before: FirstMember);
        return destination.WithMembers([.. members]);
    }

    internal static TypeDeclarationSyntax AddFieldTo(
        TypeDeclarationSyntax destination,
        IFieldSymbol field,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var declaration = GenerateFieldDeclaration(field, info, cancellationToken);

        // Place the field after the last field or const, or at the start of the type
        // declaration.
        var members = Insert(destination.Members, declaration, info, availableIndices,
            after: m => LastField(m, declaration), before: FirstMember);

        return AddMembersTo(destination, members, cancellationToken);
    }

    public static FieldDeclarationSyntax GenerateFieldDeclaration(
        IFieldSymbol field, CSharpCodeGenerationContextInfo info, CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<FieldDeclarationSyntax>(field, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var initializer = CodeGenerationFieldInfo.GetInitializer(field) is ExpressionSyntax initializerNode
            ? EqualsValueClause(initializerNode)
            : GenerateEqualsValue(info.Generator, field);

        var fieldDeclaration = FieldDeclaration(
            AttributeGenerator.GenerateAttributeLists(field.GetAttributes(), info),
            GenerateModifiers(field, info),
            VariableDeclaration(
                field.Type.GenerateTypeSyntax(),
                SeparatedList<VariableDeclaratorSyntax>().Add(AddAnnotationsTo(field, VariableDeclarator(field.Name.ToIdentifierToken(), null, initializer)))));

        return AddFormatterAndCodeGeneratorAnnotationsTo(
            ConditionallyAddDocumentationCommentTo(fieldDeclaration, field, info, cancellationToken));
    }

    private static EqualsValueClauseSyntax? GenerateEqualsValue(SyntaxGenerator generator, IFieldSymbol field)
    {
        if (field.HasConstantValue)
        {
            var canUseFieldReference = field.Type != null && !field.Type.Equals(field.ContainingType);
            return EqualsValueClause(ExpressionGenerator.GenerateExpression(generator, field.Type, field.ConstantValue, canUseFieldReference));
        }

        return null;
    }

    private static SyntaxTokenList GenerateModifiers(IFieldSymbol field, CSharpCodeGenerationContextInfo info)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var tokens);

        AddAccessibilityModifiers(field.DeclaredAccessibility, tokens, info, Accessibility.Private);
        if (field.IsConst)
        {
            tokens.Add(ConstKeyword);
        }
        else
        {
            if (field.IsStatic)
                tokens.Add(StaticKeyword);

            if (field.IsReadOnly)
                tokens.Add(ReadOnlyKeyword);

            if (field.IsRequired)
                tokens.Add(RequiredKeyword);
        }

        if (CodeGenerationFieldInfo.GetIsUnsafe(field))
            tokens.Add(UnsafeKeyword);

        return [.. tokens];
    }
}
