// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;

internal sealed class XamlCompletionItem
{
    public string[] CommitCharacters { get; set; }
    public XamlCommitCharacters? XamlCommitCharacters { get; set; }
    public string DisplayText { get; set; }
    public string InsertText { get; set; }
    public string Detail { get; set; }
    public string FilterText { get; set; }
    public string SortText { get; set; }
    public bool? Preselect { get; set; }
    public TextSpan? Span { get; set; }
    public XamlCompletionKind Kind { get; set; }
    public ClassifiedTextElement Description { get; set; }
    public ImageElement Icon { get; set; }
    public ISymbol Symbol { get; set; }
    public XamlEventDescription? EventDescription { get; set; }
    public bool RetriggerCompletion { get; set; }
    public bool IsSnippet { get; set; }
}
