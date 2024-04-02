// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class CSharpBlockStructureProvider : AbstractBlockStructureProvider
{
    private static ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> CreateDefaultNodeProviderMap()
    {
        var builder = ImmutableDictionary.CreateBuilder<Type, ImmutableArray<AbstractSyntaxStructureProvider>>();

        builder.Add<AccessorDeclarationSyntax, AccessorDeclarationStructureProvider>();
        builder.Add<AnonymousMethodExpressionSyntax, AnonymousMethodExpressionStructureProvider>();
        builder.Add<ArrowExpressionClauseSyntax, ArrowExpressionClauseStructureProvider>();
        builder.Add<BlockSyntax, BlockSyntaxStructureProvider>();
        builder.Add<ClassDeclarationSyntax, TypeDeclarationStructureProvider>();
        builder.Add<CompilationUnitSyntax, CompilationUnitStructureProvider>();
        builder.Add<ConstructorDeclarationSyntax, ConstructorDeclarationStructureProvider>();
        builder.Add<ConversionOperatorDeclarationSyntax, ConversionOperatorDeclarationStructureProvider>();
        builder.Add<DelegateDeclarationSyntax, DelegateDeclarationStructureProvider>();
        builder.Add<DestructorDeclarationSyntax, DestructorDeclarationStructureProvider>();
        builder.Add<DocumentationCommentTriviaSyntax, DocumentationCommentStructureProvider>();
        builder.Add<EnumDeclarationSyntax, EnumDeclarationStructureProvider>();
        builder.Add<EnumMemberDeclarationSyntax, EnumMemberDeclarationStructureProvider>();
        builder.Add<EventDeclarationSyntax, EventDeclarationStructureProvider>();
        builder.Add<EventFieldDeclarationSyntax, EventFieldDeclarationStructureProvider>();
        builder.Add<FieldDeclarationSyntax, FieldDeclarationStructureProvider>();
        builder.Add<FileScopedNamespaceDeclarationSyntax, FileScopedNamespaceDeclarationStructureProvider>();
        builder.Add<IndexerDeclarationSyntax, IndexerDeclarationStructureProvider>();
        builder.Add<InitializerExpressionSyntax, InitializerExpressionStructureProvider>();
        builder.Add<AnonymousObjectCreationExpressionSyntax, AnonymousObjectCreationExpressionStructureProvider>();
        builder.Add<InterfaceDeclarationSyntax, TypeDeclarationStructureProvider>();
        builder.Add<MethodDeclarationSyntax, MethodDeclarationStructureProvider>();
        builder.Add<NamespaceDeclarationSyntax, NamespaceDeclarationStructureProvider>();
        builder.Add<OperatorDeclarationSyntax, OperatorDeclarationStructureProvider>();
        builder.Add<ParenthesizedLambdaExpressionSyntax, ParenthesizedLambdaExpressionStructureProvider>();
        builder.Add<PropertyDeclarationSyntax, PropertyDeclarationStructureProvider>();
        builder.Add<RecordDeclarationSyntax, TypeDeclarationStructureProvider>();
        builder.Add<RegionDirectiveTriviaSyntax, RegionDirectiveStructureProvider>();
        builder.Add<SimpleLambdaExpressionSyntax, SimpleLambdaExpressionStructureProvider>();
        builder.Add<StructDeclarationSyntax, TypeDeclarationStructureProvider>();
        builder.Add<SwitchStatementSyntax, SwitchStatementStructureProvider>();
        builder.Add<LiteralExpressionSyntax, StringLiteralExpressionStructureProvider>();
        builder.Add<InterpolatedStringExpressionSyntax, InterpolatedStringExpressionStructureProvider>();
        builder.Add<IfDirectiveTriviaSyntax, IfDirectiveTriviaStructureProvider>();
        builder.Add<CollectionExpressionSyntax, CollectionExpressionStructureProvider>();
        builder.Add<ArgumentListSyntax, ArgumentListStructureProvider>();

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> CreateDefaultTriviaProviderMap()
    {
        var builder = ImmutableDictionary.CreateBuilder<int, ImmutableArray<AbstractSyntaxStructureProvider>>();

        builder.Add((int)SyntaxKind.DisabledTextTrivia, [new DisabledTextTriviaStructureProvider()]);
        builder.Add((int)SyntaxKind.MultiLineCommentTrivia, [new MultilineCommentBlockStructureProvider()]);

        return builder.ToImmutable();
    }

    internal CSharpBlockStructureProvider()
        : base(CreateDefaultNodeProviderMap(), CreateDefaultTriviaProviderMap())
    {
    }
}
