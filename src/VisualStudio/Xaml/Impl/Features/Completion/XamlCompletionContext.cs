// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;

/// <summary>
/// Contextual information needed for processing completion requests.
/// </summary>
internal readonly struct XamlCompletionContext
{
    public XamlCompletionContext(TextDocument document, int offset, char triggerChar = '\0')
    {
        Document = document;
        Offset = offset;
        TriggerChar = triggerChar;
    }

    public TextDocument Document { get; }
    public int Offset { get; }
    public char TriggerChar { get; }
}
