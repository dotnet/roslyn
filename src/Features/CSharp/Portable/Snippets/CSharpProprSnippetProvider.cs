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

using static Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTokens;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpProprSnippetProvider() : AbstractCSharpAutoPropertySnippetProvider
{
    public override string Identifier => CommonSnippetIdentifiers.RequiredProperty;

    public override string Description => FeaturesResources.required_property;

    protected override AccessorDeclarationSyntax? GenerateSetAccessorDeclaration(CSharpSyntaxContext syntaxContext, SyntaxGenerator generator, CancellationToken cancellationToken)
    {
        // Having a property with `set` accessor in a readonly struct leads to a compiler error.
        // So if user executes snippet inside a readonly struct the right thing to do is to not generate `set` accessor at all
        if (syntaxContext.ContainingTypeDeclaration is StructDeclarationSyntax structDeclaration &&
            syntaxContext.SemanticModel.GetDeclaredSymbol(structDeclaration, cancellationToken) is { IsReadOnly: true })
        {
            return null;
        }

        return base.GenerateSetAccessorDeclaration(syntaxContext, generator, cancellationToken);
    }

    protected override SyntaxToken[] GetAdditionalPropertyModifiers(CSharpSyntaxContext? syntaxContext)
    {
        return [RequiredKeyword];
    }
}
