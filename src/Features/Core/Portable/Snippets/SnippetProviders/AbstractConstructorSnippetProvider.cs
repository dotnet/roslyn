// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractConstructorSnippetProvider<TConstructorDeclarationSyntax> : AbstractSingleChangeSnippetProvider<TConstructorDeclarationSyntax>
    where TConstructorDeclarationSyntax : SyntaxNode
{
    public sealed override string Identifier => CommonSnippetIdentifiers.Constructor;

    public sealed override string Description => FeaturesResources.constructor;

    public sealed override ImmutableArray<string> AdditionalFilterTexts { get; } = ["constructor"];
}
