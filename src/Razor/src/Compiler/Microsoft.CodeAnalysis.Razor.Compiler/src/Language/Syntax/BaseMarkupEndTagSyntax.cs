// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class BaseMarkupEndTagSyntax
{
    private SyntaxNode? _lazyChildren;

    public SyntaxList<RazorSyntaxNode> LegacyChildren
    {
        get
        {
            var children = _lazyChildren ??
                InterlockedOperations.Initialize(ref _lazyChildren, this.ComputeEndTagLegacyChildren());

            return new SyntaxList<RazorSyntaxNode>(children);
        }
    }

    private SyntaxNode ComputeEndTagLegacyChildren()
    {
        // This method returns the children of this end tag in legacy format.
        // This is needed to generate the same classified spans as the legacy syntax tree.

        using PooledArrayBuilder<SyntaxNode> builder = [];
        using PooledArrayBuilder<SyntaxToken> tokensBuilder = [];

        // Take a ref to tokensBuilder here to avoid calling AsRef() multiple times below
        // for each call to ToListAndClear().
        ref var tokens = ref tokensBuilder.AsRef();

        var editHandler = EditHandler;
        var chunkGenerator = ChunkGenerator;

        if (IsValidToken(OpenAngle, out var openAngle))
        {
            tokens.Add(openAngle);
        }

        if (IsValidToken(ForwardSlash, out var forwardSlash))
        {
            tokens.Add(forwardSlash);
        }

        if (IsValidToken(Bang, out var bang))
        {
            SpanEditHandler? acceptsAnyHandler = null;
            SpanEditHandler? acceptsNoneHandler = null;

            if (editHandler != null)
            {
                acceptsAnyHandler = SpanEditHandler.GetDefault(AcceptedCharactersInternal.Any);
                acceptsNoneHandler = SpanEditHandler.GetDefault(AcceptedCharactersInternal.None);
            }

            // The prefix of an end tag(E.g '|</|!foo>') will have 'Any' accepted characters if a bang exists.
            builder.Add(SyntaxFactory.MarkupTextLiteral(tokens.ToListAndClear(), chunkGenerator, acceptsAnyHandler));

            // We can skip adding bang to the tokens builder, since we just cleared it.
            builder.Add(SyntaxFactory.RazorMetaCode(bang, chunkGenerator, acceptsNoneHandler));
        }

        if (IsValidToken(Name, out var name))
        {
            tokens.Add(name);
        }

        if (MiscAttributeContent?.Children is { Count: > 0 } children)
        {
            foreach (var content in children)
            {
                var literal = (MarkupTextLiteralSyntax)content;
                tokens.AddRange(literal.LiteralTokens);
            }
        }

        if (IsValidToken(CloseAngle, out var closeAngle))
        {
            tokens.Add(closeAngle);
        }

        builder.Add(SyntaxFactory.MarkupTextLiteral(tokens.ToListAndClear(), chunkGenerator, editHandler));

        return builder.ToListNode(parent: this, Position)
            .AssumeNotNull($"ToListNode should not return null since builder was not empty.");

        static bool IsValidToken(SyntaxToken token, out SyntaxToken validToken)
        {
            if (token.Kind != SyntaxKind.None && !token.IsMissing)
            {
                validToken = token;
                return true;
            }

            validToken = default;
            return false;
        }
    }

    public BaseMarkupStartTagSyntax? GetStartTag()
        => (Parent as BaseMarkupElementSyntax)?.StartTag;
}
