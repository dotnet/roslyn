// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion
{
    internal interface IXamlCompletionItem
    {
        string[] CommitCharacters { get; }
        string DisplayText { get; }
        string InsertText { get; }
        string Detail { get; }
        string FilterText { get; }
        string SortText { get; }
        bool? Preselect { get; }
        XamlCompletionKind Kind { get; }
        ClassifiedTextElement Description { get; }
        ImageElement Icon { get; }
    }
}
