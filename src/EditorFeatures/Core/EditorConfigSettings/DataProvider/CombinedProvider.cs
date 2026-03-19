// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;

internal sealed class CombinedProvider<T>(ImmutableArray<ISettingsProvider<T>> providers) : ISettingsProvider<T>
{
    private readonly ImmutableArray<ISettingsProvider<T>> _providers = providers;

    public async Task<SourceText> GetChangedEditorConfigAsync(SourceText sourceText)
    {
        foreach (var provider in _providers)
        {
            sourceText = await provider.GetChangedEditorConfigAsync(sourceText).ConfigureAwait(false);
        }

        return sourceText;
    }

    public ImmutableArray<T> GetCurrentDataSnapshot()
    {
        using var _ = ArrayBuilder<T>.GetInstance(out var builder);
        foreach (var provider in _providers)
            builder.AddRange(provider.GetCurrentDataSnapshot());

        return builder.ToImmutableAndClear();
    }

    public void RegisterViewModel(ISettingsEditorViewModel model)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterViewModel(model);
        }
    }
}
