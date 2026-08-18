// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.AutoInsert;

internal class AutoInsertService(IEnumerable<IOnAutoInsertProvider> onAutoInsertProviders) : IAutoInsertService
{
    private readonly ImmutableArray<IOnAutoInsertProvider> _onAutoInsertProviders = onAutoInsertProviders.ToImmutableArray();

    public static FrozenSet<string> HtmlAllowedAutoInsertTriggerCharacters { get; }
        = new string[] { "=" }.ToFrozenSet(StringComparer.Ordinal);
    public static FrozenSet<string> CSharpAllowedAutoInsertTriggerCharacters { get; }
        = new string[] { "'", "/", "\n", "\"" }.ToFrozenSet(StringComparer.Ordinal);

    private readonly ImmutableArray<string> _triggerCharacters = CalculateTriggerCharacters(onAutoInsertProviders);

    private static ImmutableArray<string> CalculateTriggerCharacters(IEnumerable<IOnAutoInsertProvider> onAutoInsertProviders)
    {
        using var builder = new PooledArrayBuilder<string>();
        using var _ = SpecializedPools.GetPooledStringHashSet(out var set);
        foreach (var provider in onAutoInsertProviders)
        {
            var triggerCharacter = provider.TriggerCharacter;
            if (set.Add(triggerCharacter))
            {
                builder.Add(triggerCharacter);
            }
        }

        return builder.ToImmutableAndClear();
    }

    public ImmutableArray<string> TriggerCharacters => _triggerCharacters;

    public bool TryResolveInsertion(
        RazorCodeDocument codeDocument,
        Position position,
        string character,
        bool autoCloseTags,
        [NotNullWhen(true)] out VSInternalDocumentOnAutoInsertResponseItem? insertTextEdit)
    {
        using var applicableProviders = new PooledArrayBuilder<IOnAutoInsertProvider>(capacity: _onAutoInsertProviders.Length);
        foreach (var provider in _onAutoInsertProviders)
        {
            if (provider.TriggerCharacter == character)
            {
                applicableProviders.Add(provider);
            }
        }

        if (applicableProviders.Count == 0)
        {
            // There's currently a bug in the LSP platform where other language clients OnAutoInsert trigger characters influence every language clients trigger characters.
            // To combat this we need to preemptively return so we don't try having our providers handle characters that they can't.
            insertTextEdit = null;
            return false;
        }

        foreach (var provider in applicableProviders)
        {
            if (provider.TryResolveInsertion(position, codeDocument, autoCloseTags, out insertTextEdit))
            {
                return true;
            }
        }

        // No provider could handle the text edit.
        insertTextEdit = null;
        return false;
    }
}
