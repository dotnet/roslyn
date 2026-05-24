// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorProjectEngineFeatureBase : IRazorProjectEngineFeature
{
    private RazorProjectEngine? _projectEngine;

    public RazorProjectEngine ProjectEngine
    {
        get => _projectEngine.AssumeNotNull(Resources.FeatureMustBeInitialized);
        init => Initialize(value);
    }

    public void Initialize(RazorProjectEngine projectEngine)
    {
        ArgHelper.ThrowIfNull(projectEngine);

        if (Interlocked.CompareExchange(ref _projectEngine, projectEngine, null) is not null)
        {
            throw new InvalidOperationException(Resources.FeatureAlreadyInitialized);
        }

        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }
}
