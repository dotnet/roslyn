// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// a logger that aggregate multiple loggers
/// </summary>
internal sealed class AggregateLogger : ILogger
{
    private readonly ImmutableArray<ILogger> _loggers;

    public static AggregateLogger Create(params ILogger[] loggers)
    {
        var set = new HashSet<ILogger>();

        // flatten loggers
        foreach (var logger in loggers.WhereNotNull())
        {
            if (logger is AggregateLogger aggregateLogger)
            {
                set.UnionWith(aggregateLogger._loggers);
                continue;
            }

            set.Add(logger);
        }

        return new AggregateLogger(set.ToImmutableArray());
    }

    public static ILogger AddOrReplace(ILogger newLogger, ILogger oldLogger, Func<ILogger, bool> predicate)
    {
        if (newLogger == null)
        {
            return oldLogger;
        }

        if (oldLogger == null)
        {
            return newLogger;
        }

        var aggregateLogger = oldLogger as AggregateLogger;
        if (aggregateLogger == null)
        {
            // replace old logger with new logger
            if (predicate(oldLogger))
            {
                // this might not aggregate logger
                return newLogger;
            }

            // merge two
            return new AggregateLogger([newLogger, oldLogger]);
        }

        var set = new HashSet<ILogger>();
        foreach (var logger in aggregateLogger._loggers)
        {
            // replace this logger with new logger
            if (predicate(logger))
            {
                set.Add(newLogger);
                continue;
            }

            // add old one back
            set.Add(logger);
        }

        // add new logger. if we already added one, this will be ignored.
        set.Add(newLogger);
        return new AggregateLogger(set.ToImmutableArray());
    }

    public static ILogger Remove(ILogger logger, Func<ILogger, bool> predicate)
    {
        var aggregateLogger = logger as AggregateLogger;
        if (aggregateLogger == null)
        {
            // remove the logger
            if (predicate(logger))
            {
                return null;
            }

            return logger;
        }

        // filter out loggers
        var set = aggregateLogger._loggers.Where(l => !predicate(l)).ToSet();
        if (set.Count == 1)
        {
            return set.Single();
        }

        return new AggregateLogger(set.ToImmutableArray());
    }

    private AggregateLogger(ImmutableArray<ILogger> loggers)
        => _loggers = loggers;

    public bool IsEnabled(FunctionId functionId)
        => true;

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
        for (var i = 0; i < _loggers.Length; i++)
        {
            var logger = _loggers[i];
            if (!logger.IsEnabled(functionId))
            {
                continue;
            }

            logger.Log(functionId, logMessage);
        }
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
        for (var i = 0; i < _loggers.Length; i++)
        {
            var logger = _loggers[i];
            if (!logger.IsEnabled(functionId))
            {
                continue;
            }

            logger.LogBlockStart(functionId, logMessage, uniquePairId, cancellationToken);
        }
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        for (var i = 0; i < _loggers.Length; i++)
        {
            var logger = _loggers[i];
            if (!logger.IsEnabled(functionId))
            {
                continue;
            }

            logger.LogBlockEnd(functionId, logMessage, uniquePairId, delta, cancellationToken);
        }
    }
}
