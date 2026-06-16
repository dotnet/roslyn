// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class BaseMarkupStartTagSyntax
{
    private SyntaxNode? _lazyChildren;

    public BaseMarkupElementSyntax ParentElement => (BaseMarkupElementSyntax)Parent;

    public SyntaxList<RazorSyntaxNode> LegacyChildren
    {
        get
        {
            var children = _lazyChildren ??
                InterlockedOperations.Initialize(ref _lazyChildren, ComputeStartTagLegacyChildren());

            return new SyntaxList<RazorSyntaxNode>(children);
        }
    }

    public bool IsVoidElement()
    {
        return ParserHelpers.VoidElements.Contains(Name.Content);
    }

    public bool IsSelfClosing()
    {
        return ForwardSlash.Kind != SyntaxKind.None &&
            !ForwardSlash.IsMissing &&
            !CloseAngle.IsMissing;
    }

    /// <summary>
    ///  This method returns the children of this start tag in legacy format.
    ///  This is needed to generate the same classified spans as the legacy syntax tree.
    /// </summary>
    private SyntaxNode ComputeStartTagLegacyChildren()
    {
        using PooledArrayBuilder<SyntaxNode> builder = [];
        using PooledArrayBuilder<SyntaxToken> tokensBuilder = [];

        // Take a ref to tokensBuilder here to avoid calling AsRef() multiple times below
        // for each call to ToListAndClear().
        ref var tokens = ref tokensBuilder.AsRef();

        SpanEditHandler? acceptsAnyHandler = null;
        SpanEditHandler? acceptsNoneHandler = null;

        var containsAttributesContent = false;

        var editHandler = EditHandler;
        if (editHandler != null)
        {
            acceptsAnyHandler = SpanEditHandler.GetDefault(AcceptedCharactersInternal.Any);
            acceptsNoneHandler = SpanEditHandler.GetDefault(AcceptedCharactersInternal.None);

            // We want to know if this tag contains non-whitespace attribute content to set
            // the appropriate AcceptedCharacters. The prefix of a start tag(E.g '|<foo| attr>')
            // will have 'Any' accepted characters if non-whitespace attribute content exists.

            foreach (var attribute in Attributes)
            {
                foreach (var token in attribute.DescendantTokens())
                {
                    if (!string.IsNullOrWhiteSpace(token.Content))
                    {
                        containsAttributesContent = true;
                        break;
                    }
                }
            }
        }

        var chunkGenerator = ChunkGenerator;

        if (IsValidToken(OpenAngle, out var openAngle))
        {
            tokens.Add(openAngle);
        }

        if (IsValidToken(Bang, out var bang))
        {
            builder.Add(SyntaxFactory.MarkupTextLiteral(tokens.ToListAndClear(), chunkGenerator, acceptsAnyHandler));

            // We can skip adding bang to the tokens builder, since we just cleared it.
            builder.Add(SyntaxFactory.RazorMetaCode(bang, chunkGenerator, acceptsNoneHandler));
        }

        if (IsValidToken(Name, out var name))
        {
            tokens.Add(name);
        }

        builder.Add(SyntaxFactory.MarkupTextLiteral(
            tokens.ToListAndClear(), chunkGenerator, containsAttributesContent ? acceptsAnyHandler : editHandler));

        builder.AddRange(Attributes);

        if (IsValidToken(ForwardSlash, out var forwardSlash))
        {
            tokens.Add(forwardSlash);
        }

        if (IsValidToken(CloseAngle, out var closeAngle))
        {
            tokens.Add(closeAngle);
        }

        if (tokens.Count > 0)
        {
            builder.Add(SyntaxFactory.MarkupTextLiteral(tokens.ToListAndClear(), chunkGenerator, editHandler));
        }

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

    public BaseMarkupEndTagSyntax? GetEndTag()
        => (Parent as BaseMarkupElementSyntax)?.EndTag;
}
