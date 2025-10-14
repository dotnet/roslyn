// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// LogMessage that creates key value map lazily
/// </summary>
internal abstract class KeyValueLogMessage : LogMessage
{
    private sealed class SimpleKeyValueLogMessage : KeyValueLogMessage
    {
        private static readonly ObjectPool<SimpleKeyValueLogMessage> s_pool = new(() => new());

        private Action<Dictionary<string, object?>>? _propertySetter;

        protected override void AddBackToPool()
        {
            _propertySetter = null;
            s_pool.Free(this);
        }

        protected override void EnsureMap()
        {
            // always create _map
            if (_lazyMap == null)
            {
                _lazyMap = SharedPools.Default<Dictionary<string, object?>>().AllocateAndClear();
                _propertySetter?.Invoke(_lazyMap);
            }
        }

        private void Initialize(LogType kind, Action<Dictionary<string, object?>>? propertySetter, LogLevel logLevel)
        {
            Kind = kind;
            _propertySetter = propertySetter;
            LogLevel = logLevel;
        }

        public static new SimpleKeyValueLogMessage Create(Action<Dictionary<string, object?>> propertySetter, LogLevel logLevel = LogLevel.Information)
        {
            var logMessage = s_pool.Allocate();
            logMessage.Initialize(LogType.Trace, propertySetter, logLevel);

            return logMessage;
        }

        public static new SimpleKeyValueLogMessage Create(LogType kind, LogLevel logLevel = LogLevel.Information)
            => Create(kind, propertySetter: null, logLevel);

        public static new SimpleKeyValueLogMessage Create(LogType kind, Action<Dictionary<string, object?>>? propertySetter, LogLevel logLevel = LogLevel.Information)
        {
            var logMessage = s_pool.Allocate();
            logMessage.Initialize(kind, propertySetter, logLevel);

            return logMessage;
        }
    }

    private sealed class GenericKeyValueLogMessage<TArgs> : KeyValueLogMessage
    {
        private static readonly ObjectPool<GenericKeyValueLogMessage<TArgs>> s_pool = new(() => new());

        private Action<Dictionary<string, object?>, TArgs> _propertySetter = null!;
        private TArgs _args = default!;

        protected override void AddBackToPool()
        {
            _propertySetter = null!;
            _args = default!;
            s_pool.Free(this);
        }

        protected override void EnsureMap()
        {
            // always create _map
            if (_lazyMap == null)
            {
                _lazyMap = SharedPools.Default<Dictionary<string, object?>>().AllocateAndClear();
                _propertySetter.Invoke(_lazyMap, _args);
            }
        }

        private void Initialize(
            LogType kind, Action<Dictionary<string, object?>, TArgs> propertySetter, TArgs args, LogLevel logLevel)
        {
            Kind = kind;
            _propertySetter = propertySetter;
            _args = args;
            LogLevel = logLevel;
        }

        public static GenericKeyValueLogMessage<TArgs> Create(
            Action<Dictionary<string, object?>, TArgs> propertySetter, TArgs args, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage.Initialize(LogType.Trace, propertySetter, args, logLevel);

            return logMessage;
        }

        public static GenericKeyValueLogMessage<TArgs> Create(
            LogType kind, Action<Dictionary<string, object?>, TArgs> propertySetter, TArgs args, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage.Initialize(kind, propertySetter, args, logLevel);

            return logMessage;
        }
    }

    public static readonly KeyValueLogMessage NoProperty = new SimpleKeyValueLogMessage();

    /// <summary>
    /// Creates a <see cref="KeyValueLogMessage"/> with default <see cref="LogLevel.Information"/>, since
    /// KV Log Messages are by default more informational and should be logged as such. 
    /// </summary>
    public static KeyValueLogMessage Create(Action<Dictionary<string, object?>> propertySetter, LogLevel logLevel = LogLevel.Information)
        => SimpleKeyValueLogMessage.Create(propertySetter, logLevel);

    public static KeyValueLogMessage Create(LogType kind, LogLevel logLevel = LogLevel.Information)
        => SimpleKeyValueLogMessage.Create(kind, logLevel);

    public static KeyValueLogMessage Create(LogType kind, Action<Dictionary<string, object?>>? propertySetter, LogLevel logLevel = LogLevel.Information)
        => SimpleKeyValueLogMessage.Create(kind, propertySetter, logLevel);

    public static KeyValueLogMessage Create<TArgs>(Action<Dictionary<string, object?>, TArgs> propertySetter, TArgs args, LogLevel logLevel = LogLevel.Information)
        => GenericKeyValueLogMessage<TArgs>.Create(propertySetter, args, logLevel);

    public static KeyValueLogMessage Create<TArgs>(LogType kind, Action<Dictionary<string, object?>, TArgs> propertySetter, TArgs args, LogLevel logLevel = LogLevel.Information)
        => GenericKeyValueLogMessage<TArgs>.Create(kind, propertySetter, args, logLevel);

    private Dictionary<string, object?>? _lazyMap;

    private KeyValueLogMessage()
    {
        // prevent it from being created directly
        Kind = LogType.Trace;
    }

    public LogType Kind { get; private set; }

    public bool ContainsProperty
    {
        get
        {
            EnsureMap();
            return _lazyMap.Count > 0;
        }
    }

    public IReadOnlyDictionary<string, object?> Properties
    {
        get
        {
            EnsureMap();
            return _lazyMap;
        }
    }

    protected override string CreateMessage()
    {
        EnsureMap();

        const char PairSeparator = '|';
        const char KeyValueSeparator = '=';
        const char ItemSeparator = ',';

        using var _ = PooledStringBuilder.GetInstance(out var builder);

        foreach (var entry in _lazyMap)
        {
            if (builder.Length > 0)
            {
                builder.Append(PairSeparator);
            }

            Append(builder, entry.Key);
            builder.Append(KeyValueSeparator);

            if (entry.Value is IEnumerable<object> items)
            {
                var first = true;
                foreach (var item in items)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        builder.Append(ItemSeparator);
                    }

                    Append(builder, item);
                }
            }
            else
            {
                Append(builder, entry.Value);
            }
        }

        static void Append(StringBuilder builder, object? value)
        {
            if (value != null)
            {
                var str = value.ToString();
                Debug.Assert(str != null && !str.Contains(PairSeparator) && !str.Contains(KeyValueSeparator) && !str.Contains(ItemSeparator));
                builder.Append(str);
            }
        }

        return builder.ToString();
    }

    protected override void FreeCore()
    {
        if (this == NoProperty)
        {
            return;
        }

        if (_lazyMap != null)
        {
            SharedPools.Default<Dictionary<string, object?>>().ClearAndFree(_lazyMap);
            _lazyMap = null;
        }

        this.AddBackToPool();
    }

    protected abstract void AddBackToPool();

    [MemberNotNull(nameof(_lazyMap))]
    protected abstract void EnsureMap();
}

/// <summary>
/// Type of log it is making.
/// </summary>
internal enum LogType
{
    /// <summary>
    /// Log some traces of an activity (default)
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Log an user explicit action
    /// </summary>
    UserAction = 1,
}
