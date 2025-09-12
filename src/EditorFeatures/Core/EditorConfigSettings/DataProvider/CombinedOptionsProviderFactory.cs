// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;

internal sealed class CombinedOptionsProviderFactory<T>(ImmutableArray<ISettingsProviderFactory<T>> factories) : ISettingsProviderFactory<T>
{
    private readonly ImmutableArray<ISettingsProviderFactory<T>> _factories = factories;

    public ISettingsProvider<T> GetForFile(string filePath)
    {
        var providers = TemporaryArray<ISettingsProvider<T>>.Empty;
        foreach (var factory in _factories)
        {
            providers.Add(factory.GetForFile(filePath));
        }

        return new CombinedProvider<T>(providers.ToImmutableAndClear());
    }
}
