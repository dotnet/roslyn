// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorEngineFeatureBase : IRazorEngineFeature
{
    private RazorEngine? _engine;

    public RazorEngine Engine
    {
        get => _engine.AssumeNotNull(Resources.FeatureMustBeInitialized);
        init => Initialize(value);
    }

    public void Initialize(RazorEngine engine)
    {
        ArgHelper.ThrowIfNull(engine);

        if (Interlocked.CompareExchange(ref _engine, engine, null) is not null)
        {
            throw new InvalidOperationException(Resources.FeatureAlreadyInitialized);
        }

        OnInitialized();
    }

    protected TFeature GetRequiredFeature<TFeature>()
        where TFeature : class, IRazorEngineFeature
    {
        if (Engine.TryGetFeature(out TFeature? feature))
        {
            return feature;
        }

        throw new InvalidOperationException(
            Resources.FormatFeatureDependencyMissing(GetType().Name, typeof(TFeature).Name, nameof(RazorEngine)));
    }

    protected void ThrowForMissingDocumentDependency<T>([NotNull] T? value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatFeatureDependencyMissing(GetType().Name, typeof(T).Name, nameof(RazorCodeDocument)));
        }
    }

    protected virtual void OnInitialized()
    {
    }
}
