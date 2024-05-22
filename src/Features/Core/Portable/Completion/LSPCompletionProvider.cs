// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion;

internal abstract class LSPCompletionProvider : CommonCompletionProvider
{
    /// <summary>
    /// Defines the set of possible non-identifier trigger characters for this completion provider.
    /// Used by the LSP server to determine the trigger character set for completion.
    /// </summary>
    public abstract ImmutableHashSet<char> TriggerCharacters { get; }
}
