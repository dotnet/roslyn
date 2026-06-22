// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed class AttributeCompletionContext
{
    public TagHelperDocumentContext DocumentContext { get; }
    public IEnumerable<string> ExistingCompletions { get; }
    public string CurrentTagName { get; }
    public string? CurrentAttributeName { get; }
    public ImmutableArray<KeyValuePair<string, string>> Attributes { get; }
    public string? CurrentParentTagName { get; }
    public bool CurrentParentIsTagHelper { get; }
    public Func<string, bool> InHTMLSchema { get; }

    public AttributeCompletionContext(
        TagHelperDocumentContext documentContext,
        IEnumerable<string> existingCompletions,
        string currentTagName,
        string? currentAttributeName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? currentParentTagName,
        bool currentParentIsTagHelper,
        Func<string, bool> inHTMLSchema)
    {
        DocumentContext = documentContext ?? throw new ArgumentNullException(nameof(documentContext));
        ExistingCompletions = existingCompletions ?? throw new ArgumentNullException(nameof(existingCompletions));
        CurrentTagName = currentTagName ?? throw new ArgumentNullException(nameof(currentTagName));
        CurrentAttributeName = currentAttributeName;
        Attributes = attributes.IsDefault ? throw new ArgumentNullException(nameof(attributes)) : attributes;
        CurrentParentTagName = currentParentTagName;
        CurrentParentIsTagHelper = currentParentIsTagHelper;
        InHTMLSchema = inHTMLSchema ?? throw new ArgumentNullException(nameof(inHTMLSchema));
    }
}
