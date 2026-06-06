// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

// The main reason for this service is auto-insert of empty double quotes when a user types
// equals "=" after Blazor component attribute. We think this is C# (correctly I guess)
// and wouldn't forward auto-insert request to HTML in this case. By essentially overriding
// language info here we allow the request to be sent over to HTML where it will insert empty
// double-quotes as it would for any other attribute value
internal sealed class PreferHtmlInAttributeValuesDocumentPositionInfoStrategy : IDocumentPositionInfoStrategy
{
    public static IDocumentPositionInfoStrategy Instance { get; } = new PreferHtmlInAttributeValuesDocumentPositionInfoStrategy();

    private PreferHtmlInAttributeValuesDocumentPositionInfoStrategy()
    {
    }

    public DocumentPositionInfo GetPositionInfo(IDocumentMappingService mappingService, RazorCodeDocument codeDocument, int hostDocumentIndex)
    {
        var positionInfo = DefaultDocumentPositionInfoStrategy.Instance.GetPositionInfo(mappingService, codeDocument, hostDocumentIndex);

        var absolutePosition = positionInfo.HostDocumentIndex;
        if (positionInfo.LanguageKind != RazorLanguageKind.CSharp ||
            absolutePosition < 1)
        {
            return positionInfo;
        }

        // Get the node at previous position to see if we are after markup tag helper attribute,
        // and more specifically after the EqualsToken of it
        var previousPosition = absolutePosition - 1;

        var syntaxRoot = codeDocument.GetRequiredSyntaxRoot();

        var owner = syntaxRoot.FindInnermostNode(previousPosition);

        if (owner is MarkupTagHelperAttributeSyntax { EqualsToken: { IsMissing: false } equalsToken } &&
            equalsToken.EndPosition == positionInfo.HostDocumentIndex)
        {
            return new DocumentPositionInfo(RazorLanguageKind.Html, codeDocument.Source.Text.GetPosition(absolutePosition), absolutePosition);
        }

        return positionInfo;
    }
}
