// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
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
    public override string Identifier => CSharpSnippetIdentifiers.RequiredProperty;

    public override string Description => CSharpFeaturesResources.required_property;

    protected override SyntaxToken[] GetAdditionalPropertyModifiers(CSharpSyntaxContext? syntaxContext) => [RequiredKeyword];

    protected override bool IsValidSnippetLocationCore(SnippetContext context, CancellationToken cancellationToken)
    {
        if (!base.IsValidSnippetLocationCore(context, cancellationToken))
            return false;

        var syntaxContext = (CSharpSyntaxContext)context.SyntaxContext;
        var precedingModifiers = syntaxContext.PrecedingModifiers;

        // The required modifier can't be applied to members of an interface
        if (syntaxContext.ContainingTypeDeclaration is InterfaceDeclarationSyntax)
            return false;

        // "protected internal" modifiers are valid for required property
        if (precedingModifiers.IsSupersetOf([SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword]))
            return true;

        // "private", "private protected", "protected" and "private protected" modifiers are NOT valid for required property
        if (precedingModifiers.Any(syntaxKind => syntaxKind is SyntaxKind.PrivateKeyword or SyntaxKind.ProtectedKeyword))
            return false;

        return true;
    }

    protected override AccessorDeclarationSyntax? GenerateSetAccessorDeclaration(CSharpSyntaxContext syntaxContext, SyntaxGenerator generator, CancellationToken cancellationToken)
    {
        // Having a property with `set` accessor in a readonly struct leads to a compiler error.
        // At the same time having a required property with no setter at all is also illegal.
        // Thus out best guess here is to generate an `init` accessor. We can assume they are available
        // as a language feature since `required` keyword has a higher minimal language version to use
        if (syntaxContext.ContainingTypeDeclaration is StructDeclarationSyntax structDeclaration &&
            syntaxContext.SemanticModel.GetDeclaredSymbol(structDeclaration, cancellationToken) is { IsReadOnly: true })
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SemicolonToken);
        }

        return base.GenerateSetAccessorDeclaration(syntaxContext, generator, cancellationToken);
    }
}
