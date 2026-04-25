// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorEnginePhaseBase : IRazorEnginePhase
{
    private RazorEngine? _engine;

    public RazorEngine Engine
    {
        get => _engine.AssumeNotNull(Resources.PhaseMustBeInitialized);
        init => Initialize(value);
    }

    public void Initialize(RazorEngine engine)
    {
        ArgHelper.ThrowIfNull(engine);

        if (Interlocked.CompareExchange(ref _engine, engine, null) is not null)
        {
            throw new InvalidOperationException(Resources.PhaseAlreadyInitialized);
        }

        OnInitialized();
    }

    public RazorCodeDocument Execute(RazorCodeDocument codeDocument, CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(codeDocument);
        Assumed.NotNull(_engine, Resources.PhaseMustBeInitialized);

        return ExecuteCore(codeDocument, cancellationToken);
    }

    protected T GetRequiredFeature<T>()
        where T : class, IRazorEngineFeature
    {
        if (Engine.GetFeatures<T>() is [var feature, ..])
        {
            return feature;
        }

        throw new InvalidOperationException(
            Resources.FormatPhaseDependencyMissing(GetType().Name, typeof(T).Name, nameof(RazorEngine)));
    }

    protected void ThrowForMissingDocumentDependency<T>([NotNull] T? value)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                Resources.FormatPhaseDependencyMissing(GetType().Name, typeof(T).Name, nameof(RazorCodeDocument)));
        }
    }

    protected virtual void OnInitialized()
    {
    }

    protected abstract RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken);
}
