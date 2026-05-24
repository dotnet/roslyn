// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Simple cache for <see cref="IRazorProjectEngineFeature">s and <see cref="IRazorEngineFeature"/>s used
/// by <see cref="RazorEngine"/> and <see cref="RazorProjectEngine"/>.
/// </summary>
internal sealed class FeatureCache<T>(ImmutableArray<T> features)
    where T : class
{
    private readonly ImmutableArray<T> _features = features;

    private readonly ReaderWriterLockSlim _gate = new();
    private Dictionary<Type, object[]>? _typeToFeaturesMap;

    public ImmutableArray<TFeature> GetFeatures<TFeature>()
        where TFeature : class, T
    {
        using var upgradeableRead = _gate.DisposableUpgradeableRead();

        var key = typeof(TFeature);

        if (_typeToFeaturesMap?.TryGetValue(key, out var values) is true)
        {
            return ConvertValuesToResult(values);
        }

        upgradeableRead.EnterWrite();
        _typeToFeaturesMap ??= [];

        using var builder = new PooledArrayBuilder<TFeature>(capacity: _features.Length);

        foreach (var feature in _features)
        {
            if (feature is TFeature featureOfType)
            {
                builder.Add(featureOfType);
            }
        }

        values = builder.ToArray();
        _typeToFeaturesMap.Add(key, values);

        return ConvertValuesToResult(values);

        static ImmutableArray<TFeature> ConvertValuesToResult(object[] values)
        {
            return values is []
                ? []
                : ImmutableCollectionsMarshal.AsImmutableArray((TFeature[])values);
        }
    }
}
