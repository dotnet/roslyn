// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

using static CSharpSyntaxTokens;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpPropiSnippetProvider() : AbstractCSharpAutoPropertySnippetProvider
{
    public override string Identifier => CSharpSnippetIdentifiers.InitOnlyProperty;

    public override string Description => CSharpFeaturesResources.init_only_property;

    protected override AccessorDeclarationSyntax? GenerateSetAccessorDeclaration(CSharpSyntaxContext syntaxContext, SyntaxGenerator generator, CancellationToken cancellationToken)
        => SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SemicolonToken);
}
