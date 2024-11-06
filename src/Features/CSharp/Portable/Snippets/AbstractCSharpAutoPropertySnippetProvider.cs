// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

using static CSharpSyntaxTokens;

internal abstract class AbstractCSharpAutoPropertySnippetProvider : AbstractPropertySnippetProvider<PropertyDeclarationSyntax>
{
    protected virtual AccessorDeclarationSyntax? GenerateGetAccessorDeclaration(CSharpSyntaxContext syntaxContext, SyntaxGenerator generator, CancellationToken cancellationToken)
        => (AccessorDeclarationSyntax)generator.GetAccessorDeclaration();

    protected virtual AccessorDeclarationSyntax? GenerateSetAccessorDeclaration(CSharpSyntaxContext syntaxContext, SyntaxGenerator generator, CancellationToken cancellationToken)
        => (AccessorDeclarationSyntax)generator.SetAccessorDeclaration();

    protected virtual SyntaxToken[] GetAdditionalPropertyModifiers(CSharpSyntaxContext? syntaxContext) => [];

    protected override bool IsValidSnippetLocationCore(SnippetContext context, CancellationToken cancellationToken)
    {
        return context.SyntaxContext.SyntaxTree.IsMemberDeclarationContext(context.Position, (CSharpSyntaxContext)context.SyntaxContext,
            SyntaxKindSet.AllMemberModifiers, SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: true, cancellationToken);
    }

    protected override async Task<PropertyDeclarationSyntax> GenerateSnippetSyntaxAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var generator = SyntaxGenerator.GetGenerator(document);
        var identifierName = NameGenerator.GenerateUniqueName("MyProperty",
            n => semanticModel.LookupSymbols(position, name: n).IsEmpty);
        var syntaxContext = CSharpSyntaxContext.CreateContext(document, semanticModel, position, cancellationToken);
        var accessors = new AccessorDeclarationSyntax?[]
        {
            GenerateGetAccessorDeclaration(syntaxContext, generator, cancellationToken),
            GenerateSetAccessorDeclaration(syntaxContext, generator, cancellationToken),
        };

        SyntaxTokenList modifiers = default;

        // If there are no preceding accessibility modifiers create default `public` one
        if (!syntaxContext.PrecedingModifiers.Any(SyntaxFacts.IsAccessibilityModifier))
        {
            modifiers = SyntaxTokenList.Create(PublicKeyword);
        }

        modifiers = modifiers.AddRange(GetAdditionalPropertyModifiers(syntaxContext));

        return SyntaxFactory.PropertyDeclaration(
            attributeLists: default,
            modifiers: modifiers,
            type: compilation.GetSpecialType(SpecialType.System_Int32).GenerateTypeSyntax(allowVar: false),
            explicitInterfaceSpecifier: null,
            identifier: identifierName.ToIdentifierToken(),
            accessorList: SyntaxFactory.AccessorList([.. (IEnumerable<AccessorDeclarationSyntax>)accessors.Where(a => a is not null)]));
    }

    protected override int GetTargetCaretPosition(PropertyDeclarationSyntax propertyDeclaration, SourceText sourceText)
        => propertyDeclaration.AccessorList!.CloseBraceToken.Span.End;

    protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(PropertyDeclarationSyntax propertyDeclaration, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        var identifier = propertyDeclaration.Identifier;
        var type = propertyDeclaration.Type;

        return
        [
            new SnippetPlaceholder(type.ToString(), type.SpanStart),
            new SnippetPlaceholder(identifier.ValueText, identifier.SpanStart),
        ];
    }

    protected override PropertyDeclarationSyntax? FindAddedSnippetSyntaxNode(SyntaxNode root, int position)
    {
        var node = root.FindNode(TextSpan.FromBounds(position, position));
        return node.GetAncestorOrThis<PropertyDeclarationSyntax>();
    }
}
