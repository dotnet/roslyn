// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages;

internal class FSharpSourceReferenceItem
{

#pragma warning disable IDE0051 // Remove unused private members
    private FSharpSourceReferenceItem(Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem roslynDefinitionItem)
#pragma warning restore IDE0051 // Remove unused private members
    {
        RoslynSourceReferenceItem = roslynDefinitionItem;
    }

    public FSharpSourceReferenceItem(FSharpDefinitionItem definition, FSharpDocumentSpan sourceSpan)
    {
        RoslynSourceReferenceItem = new Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem(definition.RoslynDefinitionItem, sourceSpan.ToRoslynDocumentSpan(), classifiedSpans: null);
    }

    /// <summary>
    /// A reference whose line is already classified, so the window that lists it does not ask the classification
    /// service for that line again.
    /// </summary>
    /// <param name="classifiedSpans">The classified text of the line that holds <paramref name="sourceSpan"/>, from its
    /// first non-whitespace character to its end, with every character covered.</param>
    /// <param name="highlightSpan">The reference within that line, relative to the start of the first classified
    /// span.</param>
    public FSharpSourceReferenceItem(FSharpDefinitionItem definition, FSharpDocumentSpan sourceSpan, ImmutableArray<ClassifiedSpan> classifiedSpans, TextSpan highlightSpan)
    {
        RoslynSourceReferenceItem = new Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem(
            definition.RoslynDefinitionItem, sourceSpan.ToRoslynDocumentSpan(), new ClassifiedSpansAndHighlightSpan(classifiedSpans, highlightSpan));
    }

    internal Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem RoslynSourceReferenceItem { get; }
}
