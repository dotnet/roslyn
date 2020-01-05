// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class CSharpBlockStructureProvider : AbstractBlockStructureProvider
    {
        private static ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> CreateDefaultNodeProviderMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<Type, ImmutableArray<AbstractSyntaxStructureProvider>>();

            builder.Add<AccessorDeclarationSyntax, AccessorDeclarationStructureProvider>();
            builder.Add<AnonymousMethodExpressionSyntax, AnonymousMethodExpressionStructureProvider>();
            builder.Add<ArrowExpressionClauseSyntax, ArrowExpressionClauseStructureProvider>();
            builder.Add<BlockSyntax, BlockSyntaxStructureProvider>();
            builder.Add<ClassDeclarationSyntax, TypeDeclarationStructureProvider, MetadataAsSource.MetadataTypeDeclarationStructureProvider>();
            builder.Add<CompilationUnitSyntax, CompilationUnitStructureProvider>();
            builder.Add<ConstructorDeclarationSyntax, ConstructorDeclarationStructureProvider, MetadataAsSource.MetadataConstructorDeclarationStructureProvider>();
            builder.Add<ConversionOperatorDeclarationSyntax, ConversionOperatorDeclarationStructureProvider, MetadataAsSource.MetadataConversionOperatorDeclarationStructureProvider>();
            builder.Add<DelegateDeclarationSyntax, DelegateDeclarationStructureProvider, MetadataAsSource.MetadataDelegateDeclarationStructureProvider>();
            builder.Add<DestructorDeclarationSyntax, DestructorDeclarationStructureProvider, MetadataAsSource.MetadataDestructorDeclarationStructureProvider>();
            builder.Add<DocumentationCommentTriviaSyntax, DocumentationCommentStructureProvider>();
            builder.Add<EnumDeclarationSyntax, EnumDeclarationStructureProvider, MetadataAsSource.MetadataEnumDeclarationStructureProvider>();
            builder.Add<EnumMemberDeclarationSyntax, MetadataAsSource.MetadataEnumMemberDeclarationStructureProvider>();
            builder.Add<EventDeclarationSyntax, EventDeclarationStructureProvider, MetadataAsSource.MetadataEventDeclarationStructureProvider>();
            builder.Add<EventFieldDeclarationSyntax, EventFieldDeclarationStructureProvider, MetadataAsSource.MetadataEventFieldDeclarationStructureProvider>();
            builder.Add<FieldDeclarationSyntax, FieldDeclarationStructureProvider, MetadataAsSource.MetadataFieldDeclarationStructureProvider>();
            builder.Add<IndexerDeclarationSyntax, IndexerDeclarationStructureProvider, MetadataAsSource.MetadataIndexerDeclarationStructureProvider>();
            builder.Add<InitializerExpressionSyntax, InitializerExpressionStructureProvider>();
            builder.Add<InterfaceDeclarationSyntax, TypeDeclarationStructureProvider, MetadataAsSource.MetadataTypeDeclarationStructureProvider>();
            builder.Add<MethodDeclarationSyntax, MethodDeclarationStructureProvider, MetadataAsSource.MetadataMethodDeclarationStructureProvider>();
            builder.Add<NamespaceDeclarationSyntax, NamespaceDeclarationStructureProvider>();
            builder.Add<OperatorDeclarationSyntax, OperatorDeclarationStructureProvider, MetadataAsSource.MetadataOperatorDeclarationStructureProvider>();
            builder.Add<ParenthesizedLambdaExpressionSyntax, ParenthesizedLambdaExpressionStructureProvider>();
            builder.Add<PropertyDeclarationSyntax, PropertyDeclarationStructureProvider, MetadataAsSource.MetadataPropertyDeclarationStructureProvider>();
            builder.Add<RegionDirectiveTriviaSyntax, RegionDirectiveStructureProvider, MetadataAsSource.MetadataRegionDirectiveStructureProvider>();
            builder.Add<SimpleLambdaExpressionSyntax, SimpleLambdaExpressionStructureProvider>();
            builder.Add<StructDeclarationSyntax, TypeDeclarationStructureProvider, MetadataAsSource.MetadataTypeDeclarationStructureProvider>();
            builder.Add<SwitchStatementSyntax, SwitchStatementStructureProvider>();
            builder.Add<LiteralExpressionSyntax, StringLiteralExpressionStructureProvider>();
            builder.Add<InterpolatedStringExpressionSyntax, InterpolatedStringExpressionStructureProvider>();

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> CreateDefaultTriviaProviderMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<int, ImmutableArray<AbstractSyntaxStructureProvider>>();

            builder.Add((int)SyntaxKind.DisabledTextTrivia, ImmutableArray.Create<AbstractSyntaxStructureProvider>(new DisabledTextTriviaStructureProvider()));

            return builder.ToImmutable();
        }

        internal CSharpBlockStructureProvider()
            : base(CreateDefaultNodeProviderMap(), CreateDefaultTriviaProviderMap())
        {
        }
    }
}
