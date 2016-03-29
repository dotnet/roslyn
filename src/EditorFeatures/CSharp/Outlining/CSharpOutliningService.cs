// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    [ExportLanguageService(typeof(IOutliningService), LanguageNames.CSharp), Shared]
    internal class CSharpOutliningService : AbstractOutliningService
    {
        private static readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>> s_defaultNodeOutlinerMap = CreateDefaultNodeOutlinerMap();
        private static readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxOutliner>> s_defaultTriviaOutlinerMap = CreateDefaultTriviaOutlinerMap();

        private static ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>> CreateDefaultNodeOutlinerMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<Type, ImmutableArray<AbstractSyntaxOutliner>>();

            builder.Add<AccessorDeclarationSyntax, AccessorDeclarationOutliner>();
            builder.Add<AnonymousMethodExpressionSyntax, AnonymousMethodExpressionOutliner>();
            builder.Add<ClassDeclarationSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner>();
            builder.Add<CompilationUnitSyntax, CompilationUnitOutliner>();
            builder.Add<ConstructorDeclarationSyntax, ConstructorDeclarationOutliner, MetadataAsSource.ConstructorDeclarationOutliner>();
            builder.Add<ConversionOperatorDeclarationSyntax, ConversionOperatorDeclarationOutliner, MetadataAsSource.ConversionOperatorDeclarationOutliner>();
            builder.Add<DelegateDeclarationSyntax, DelegateDeclarationOutliner, MetadataAsSource.DelegateDeclarationOutliner>();
            builder.Add<DestructorDeclarationSyntax, DestructorDeclarationOutliner, MetadataAsSource.DestructorDeclarationOutliner>();
            builder.Add<DocumentationCommentTriviaSyntax, DocumentationCommentOutliner>();
            builder.Add<EnumDeclarationSyntax, EnumDeclarationOutliner, MetadataAsSource.EnumDeclarationOutliner>();
            builder.Add<EnumMemberDeclarationSyntax, MetadataAsSource.EnumMemberDeclarationOutliner>();
            builder.Add<EventDeclarationSyntax, EventDeclarationOutliner, MetadataAsSource.EventDeclarationOutliner>();
            builder.Add<EventFieldDeclarationSyntax, EventFieldDeclarationOutliner, MetadataAsSource.EventFieldDeclarationOutliner>();
            builder.Add<FieldDeclarationSyntax, FieldDeclarationOutliner, MetadataAsSource.FieldDeclarationOutliner>();
            builder.Add<IndexerDeclarationSyntax, IndexerDeclarationOutliner, MetadataAsSource.IndexerDeclarationOutliner>();
            builder.Add<InterfaceDeclarationSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner>();
            builder.Add<MethodDeclarationSyntax, MethodDeclarationOutliner, MetadataAsSource.MethodDeclarationOutliner>();
            builder.Add<NamespaceDeclarationSyntax, NamespaceDeclarationOutliner>();
            builder.Add<OperatorDeclarationSyntax, OperatorDeclarationOutliner, MetadataAsSource.OperatorDeclarationOutliner>();
            builder.Add<ParenthesizedLambdaExpressionSyntax, ParenthesizedLambdaExpressionOutliner>();
            builder.Add<PropertyDeclarationSyntax, PropertyDeclarationOutliner, MetadataAsSource.PropertyDeclarationOutliner>();
            builder.Add<RegionDirectiveTriviaSyntax, RegionDirectiveOutliner, MetadataAsSource.RegionDirectiveOutliner>();
            builder.Add<SimpleLambdaExpressionSyntax, SimpleLambdaExpressionOutliner>();
            builder.Add<StructDeclarationSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner>();

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<int, ImmutableArray<AbstractSyntaxOutliner>> CreateDefaultTriviaOutlinerMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<int, ImmutableArray<AbstractSyntaxOutliner>>();

            builder.Add((int)SyntaxKind.DisabledTextTrivia, ImmutableArray.Create<AbstractSyntaxOutliner>(new DisabledTextTriviaOutliner()));

            return builder.ToImmutable();
        }

        private CSharpOutliningService()
            : base(s_defaultNodeOutlinerMap, s_defaultTriviaOutlinerMap)
        {
        }
    }
}
