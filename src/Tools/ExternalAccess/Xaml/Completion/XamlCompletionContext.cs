// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;

/// <summary>
/// Contextual information needed for processing completion requests.
/// </summary>
internal class XamlCompletionContext(TextDocument document, int offset, char triggerChar = '\0')
{
    public TextDocument Document { get; } = document;
    public int Offset { get; } = offset;
    public char TriggerChar { get; } = triggerChar;
}
