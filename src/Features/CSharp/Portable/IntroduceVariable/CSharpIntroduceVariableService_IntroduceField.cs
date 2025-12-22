// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal sealed partial class CSharpIntroduceVariableService
{
    protected override async Task<Document> IntroduceFieldAsync(
        SemanticDocument document,
        ExpressionSyntax expression,
        bool allOccurrences,
        bool isConstant,
        CancellationToken cancellationToken)
    {
        // Get the ancestor TypeDeclarationSyntax that is NOT an ExtensionBlockDeclarationSyntax.
        // Extension blocks can't contain fields, so we need to find the containing class/struct.
        //
        // Note, this can be revised in the future as we do expect to allow constants/static in
        // extension blocks in a future version of the language.
        var oldTypeDeclaration = expression.GetAncestorsOrThis<TypeDeclarationSyntax>()
            .FirstOrDefault(t => t is not ExtensionBlockDeclarationSyntax);

        var oldType = oldTypeDeclaration != null
            ? document.SemanticModel.GetDeclaredSymbol(oldTypeDeclaration, cancellationToken)
            : document.SemanticModel.Compilation.ScriptClass;
        var newNameToken = GenerateUniqueFieldName(document, expression, isConstant, cancellationToken);
        var typeDisplayString = oldType.ToMinimalDisplayString(document.SemanticModel, expression.SpanStart);

        var newQualifiedName = oldTypeDeclaration != null
            ? MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseName(typeDisplayString), IdentifierName(newNameToken))
            : (ExpressionSyntax)IdentifierName(newNameToken);

        newQualifiedName = newQualifiedName.WithAdditionalAnnotations(Simplifier.Annotation);

        var newFieldDeclaration = FieldDeclaration(
            default,
            MakeFieldModifiers(isConstant, inScript: oldType.IsScriptClass),
            VariableDeclaration(
                GetTypeSymbol(document, expression, cancellationToken).GenerateTypeSyntax(),
                [VariableDeclarator(
                    newNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()),
                    null,
                    EqualsValueClause(expression.WithoutTrivia()))])).WithAdditionalAnnotations(Formatter.Annotation);

        if (oldTypeDeclaration != null)
        {
            var newTypeDeclaration = Rewrite(
                document, expression, newQualifiedName, document, oldTypeDeclaration, allOccurrences, cancellationToken);

            var insertionIndex = GetFieldInsertionIndex(isConstant, oldTypeDeclaration, newTypeDeclaration, cancellationToken);
            var finalTypeDeclaration = InsertMember(newTypeDeclaration, newFieldDeclaration, insertionIndex);

            var newRoot = document.Root.ReplaceNode(oldTypeDeclaration, finalTypeDeclaration);
            return document.Document.WithSyntaxRoot(newRoot);
        }
        else
        {
            var oldCompilationUnit = (CompilationUnitSyntax)document.Root;
            var newCompilationUnit = Rewrite(
                document, expression, newQualifiedName, document, oldCompilationUnit, allOccurrences, cancellationToken);

            var insertionIndex = isConstant
                ? DetermineConstantInsertPosition(oldCompilationUnit.Members, newCompilationUnit.Members)
                : DetermineFieldInsertPosition(oldCompilationUnit.Members, newCompilationUnit.Members);

            var newRoot = newCompilationUnit.WithMembers(newCompilationUnit.Members.Insert(insertionIndex, newFieldDeclaration));
            return document.Document.WithSyntaxRoot(newRoot);
        }
    }

    protected override int DetermineConstantInsertPosition(TypeDeclarationSyntax oldType, TypeDeclarationSyntax newType)
        => DetermineConstantInsertPosition(oldType.Members, newType.Members);

    private static int DetermineConstantInsertPosition(
        SyntaxList<MemberDeclarationSyntax> oldMembers,
        SyntaxList<MemberDeclarationSyntax> newMembers)
    {
        // 1) Place the constant after the last constant.
        //
        // 2) If there is no constant, place it before the first field
        //
        // 3) If the first change is before either of those, then place before the first
        // change
        //
        // 4) Otherwise, place it at the start.
        var index = 0;
        var lastConstantIndex = oldMembers.LastIndexOf(IsConstantField);

        if (lastConstantIndex >= 0)
        {
            index = lastConstantIndex + 1;
        }
        else
        {
            var firstFieldIndex = oldMembers.IndexOf(member => member is FieldDeclarationSyntax);
            if (firstFieldIndex >= 0)
            {
                index = firstFieldIndex;
            }
        }

        var firstChangeIndex = DetermineFirstChange(oldMembers, newMembers);
        if (firstChangeIndex >= 0)
        {
            index = Math.Min(index, firstChangeIndex);
        }

        return index;
    }

    protected override int DetermineFieldInsertPosition(TypeDeclarationSyntax oldType, TypeDeclarationSyntax newType)
        => DetermineFieldInsertPosition(oldType.Members, newType.Members);

    private static int DetermineFieldInsertPosition(
        SyntaxList<MemberDeclarationSyntax> oldMembers,
        SyntaxList<MemberDeclarationSyntax> newMembers)
    {
        // 1) Place the constant after the last field.
        //
        // 2) If there is no field, place it after the last constant
        //
        // 3) If the first change is before either of those, then place before the first
        // change
        //
        // 4) Otherwise, place it at the start.
        var index = 0;
        var lastFieldIndex = oldMembers.LastIndexOf(member => member is FieldDeclarationSyntax);
        if (lastFieldIndex >= 0)
        {
            index = lastFieldIndex + 1;
        }
        else
        {
            var lastConstantIndex = oldMembers.LastIndexOf(IsConstantField);
            if (lastConstantIndex >= 0)
            {
                index = lastConstantIndex + 1;
            }
        }

        var firstChangeIndex = DetermineFirstChange(oldMembers, newMembers);
        if (firstChangeIndex >= 0)
        {
            index = Math.Min(index, firstChangeIndex);
        }

        return index;
    }

    private static bool IsConstantField(MemberDeclarationSyntax member)
        => member is FieldDeclarationSyntax field && field.Modifiers.Any(SyntaxKind.ConstKeyword);

    private static int DetermineFirstChange(SyntaxList<MemberDeclarationSyntax> oldMembers, SyntaxList<MemberDeclarationSyntax> newMembers)
    {
        for (var i = 0; i < oldMembers.Count; i++)
        {
            if (!AreEquivalent(oldMembers[i], newMembers[i], topLevel: false))
            {
                return i;
            }
        }

        return -1;
    }

    private static TypeDeclarationSyntax InsertMember(
        TypeDeclarationSyntax typeDeclaration,
        MemberDeclarationSyntax memberDeclaration,
        int index)
    {
        return typeDeclaration.WithMembers(
            typeDeclaration.Members.Insert(index, memberDeclaration));
    }

    private static SyntaxTokenList MakeFieldModifiers(bool isConstant, bool inScript)
    {
        if (isConstant)
        {
            return [PrivateKeyword, ConstKeyword];
        }
        else if (inScript)
        {
            return [PrivateKeyword, ReadOnlyKeyword];
        }
        else
        {
            return [PrivateKeyword, StaticKeyword, ReadOnlyKeyword];
        }
    }
}
