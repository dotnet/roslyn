// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
internal sealed class CSharpClassSnippetProvider : AbstractCSharpTypeSnippetProvider
{
    private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.AbstractKeyword,
        SyntaxKind.SealedKeyword,
        SyntaxKind.StaticKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.FileKeyword,
    };

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpClassSnippetProvider()
    {
    }

    public override string Identifier => "class";

    public override string Description => FeaturesResources.class_;

    protected override ISet<SyntaxKind> ValidModifiers => s_validModifiers;

    protected override async Task<SyntaxNode> GenerateTypeDeclarationAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var name = NameGenerator.GenerateUniqueName("MyClass", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);
        return generator.ClassDeclaration(name);
    }

    protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
    {
        return syntaxFacts.IsClassDeclaration;
    }
}
