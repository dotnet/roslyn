// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

/// <summary>
/// A projection strategy that, when given a position that occurs anywhere in an attribute name, will return the projection
/// for the position at the start of the attribute name, ignoring any prefix or suffix. eg given any location within the
/// attribute "@bind-Value:after", it will return the projection at the point of the word "Value" therein.
/// </summary>
internal sealed class PreferAttributeNameDocumentPositionInfoStrategy : IDocumentPositionInfoStrategy
{
    public static IDocumentPositionInfoStrategy Instance { get; } = new PreferAttributeNameDocumentPositionInfoStrategy();

    private PreferAttributeNameDocumentPositionInfoStrategy()
    {
    }

    public DocumentPositionInfo GetPositionInfo(IDocumentMappingService mappingService, RazorCodeDocument codeDocument, int hostDocumentIndex)
    {
        // First, lets see if we should adjust the location to get a better result from C#. For example given <Component @bi|nd-Value="Pants" />
        // where | is the cursor, we would be unable to map that location to C#. If we pretend the caret was 3 characters to the right though,
        // in the actual component property name, then the C# server would give us a result, so we fake it.
        if (RazorSyntaxFacts.TryGetAttributeNameAbsoluteIndex(codeDocument, hostDocumentIndex, out var attributeNameIndex))
        {
            hostDocumentIndex = attributeNameIndex;
        }

        return DefaultDocumentPositionInfoStrategy.Instance.GetPositionInfo(mappingService, codeDocument, hostDocumentIndex);
    }
}
