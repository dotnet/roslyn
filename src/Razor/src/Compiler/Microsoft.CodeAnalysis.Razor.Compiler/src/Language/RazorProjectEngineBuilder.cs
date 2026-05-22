// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorProjectEngineBuilder
{
    public RazorConfiguration Configuration { get; }
    public RazorProjectFileSystem FileSystem { get; }
    public ImmutableArray<IRazorFeature>.Builder Features { get; }
    public ImmutableArray<IRazorEnginePhase>.Builder Phases { get; }

    internal RazorProjectEngineBuilder(RazorConfiguration configuration, RazorProjectFileSystem fileSystem)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        Features = ImmutableArray.CreateBuilder<IRazorFeature>();
        Phases = ImmutableArray.CreateBuilder<IRazorEnginePhase>();
    }

    public RazorProjectEngine Build()
    {
        using var engineFeatures = new PooledArrayBuilder<IRazorEngineFeature>(Features.Count);
        using var projectEngineFeatures = new PooledArrayBuilder<IRazorProjectEngineFeature>(Features.Count);

        foreach (var feature in Features)
        {
            switch (feature)
            {
                case IRazorEngineFeature engineFeature:
                    engineFeatures.Add(engineFeature);
                    break;

                case IRazorProjectEngineFeature projectEngineFeature:
                    projectEngineFeatures.Add(projectEngineFeature);
                    break;

                default:
                    Debug.Fail($"Encountered an {nameof(IRazorFeature)} that is not an {nameof(IRazorEngineFeature)} or {nameof(IRazorProjectEngineFeature)}.");
                    break;
            }
        }

        var engine = new RazorEngine(engineFeatures.ToImmutableAndClear(), Phases.ToImmutableAndClear());

        var projectEngine = new RazorProjectEngine(Configuration, engine, FileSystem, projectEngineFeatures.ToImmutableAndClear());

        return projectEngine;
    }
}
