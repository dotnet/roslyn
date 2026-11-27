// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

#if !NET7_0_OR_GREATER
using System.Collections.Generic;
#endif

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal abstract partial class AbstractLoggerFactory : ILoggerFactory
{
    private ImmutableArray<Lazy<ILoggerProvider, LoggerProviderMetadata>> _providers;
    private ImmutableDictionary<string, AggregateLogger> _loggers;

    protected AbstractLoggerFactory(ImmutableArray<ILoggerProvider> providers)
        : this(providers.SelectAsArray(p => new Lazy<ILoggerProvider, LoggerProviderMetadata>(() => p, LoggerProviderMetadata.Empty)))
    {
    }

    protected AbstractLoggerFactory(ImmutableArray<Lazy<ILoggerProvider, LoggerProviderMetadata>> providers)
    {
        _providers = providers;
        _loggers = ImmutableDictionary.Create<string, AggregateLogger>(StringComparer.OrdinalIgnoreCase);
    }

    public ILogger GetOrCreateLogger(string categoryName)
    {
        if (_loggers.TryGetValue(categoryName, out var logger))
        {
            return logger;
        }

        using var lazyLoggers = new PooledArrayBuilder<LazyLogger>(_providers.Length);

        foreach (var provider in _providers)
        {
            lazyLoggers.Add(new(provider, categoryName));
        }

        var result = new AggregateLogger(lazyLoggers.ToImmutableAndClear());
        return ImmutableInterlocked.AddOrUpdate(ref _loggers, categoryName, result, (k, v) => v);
    }

    public void AddLoggerProvider(ILoggerProvider provider)
    {
        var lazyProvider = new Lazy<ILoggerProvider, LoggerProviderMetadata>(() => provider, LoggerProviderMetadata.Empty);

        if (ImmutableInterlocked.Update(ref _providers, (set, p) => set.Add(p), lazyProvider))
        {
            foreach (var (categoryName, logger) in _loggers)
            {
                logger.AddLogger(new(lazyProvider, categoryName));
            }
        }
    }
}
