// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// log message that can generate string lazily
/// </summary>
internal abstract class LogMessage
{
    public LogLevel LogLevel { get; protected set; } = LogLevel.Debug;

    public static LogMessage Create(string message, LogLevel logLevel)
        => StaticLogMessage.Construct(message, logLevel);

    public static LogMessage Create(Func<string> messageGetter, LogLevel logLevel)
        => LazyLogMessage.Construct(messageGetter, logLevel);

    public static LogMessage Create<TArg>(Func<TArg, string> messageGetter, TArg arg, LogLevel logLevel)
        => LazyLogMessage<TArg>.Construct(messageGetter, arg, logLevel);

    public static LogMessage Create<TArg0, TArg1>(Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, LogLevel logLevel)
        => LazyLogMessage<TArg0, TArg1>.Construct(messageGetter, arg0, arg1, logLevel);

    public static LogMessage Create<TArg0, TArg1, TArg2>(Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, LogLevel logLevel)
        => LazyLogMessage<TArg0, TArg1, TArg2>.Construct(messageGetter, arg0, arg1, arg2, logLevel);

    public static LogMessage Create<TArg0, TArg1, TArg2, TArg3>(Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, LogLevel logLevel)
        => LazyLogMessage<TArg0, TArg1, TArg2, TArg3>.Construct(messageGetter, arg0, arg1, arg2, arg3, logLevel);

    // message will be either initially set or lazily set by caller
    private string? _message;

    protected abstract string CreateMessage();

    /// <summary>
    /// Logger will call this to return LogMessage to its pool
    /// </summary>
    protected abstract void FreeCore();

    public string GetMessage()
    {
        _message ??= CreateMessage();

        return _message;
    }

    public void Free()
    {
        _message = null;

        FreeCore();
    }

    private sealed class StaticLogMessage : LogMessage
    {
        private static readonly ObjectPool<StaticLogMessage> s_pool = SharedPools.Default<StaticLogMessage>();

        public static LogMessage Construct(string message, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage._message = message;
            logMessage.LogLevel = logLevel;

            return logMessage;
        }

        protected override string CreateMessage()
            => _message!;

        protected override void FreeCore()
        {
            if (_message == null)
            {
                return;
            }

            _message = null;
            s_pool.Free(this);
        }
    }

    private sealed class LazyLogMessage : LogMessage
    {
        private static readonly ObjectPool<LazyLogMessage> s_pool = SharedPools.Default<LazyLogMessage>();

        private Func<string>? _messageGetter;

        public static LogMessage Construct(Func<string> messageGetter, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage._messageGetter = messageGetter;
            logMessage.LogLevel = logLevel;

            return logMessage;
        }

        protected override string CreateMessage()
            => _messageGetter!();

        protected override void FreeCore()
        {
            if (_messageGetter == null)
            {
                return;
            }

            _messageGetter = null;
            s_pool.Free(this);
        }
    }

    private sealed class LazyLogMessage<TArg0> : LogMessage
    {
        private static readonly ObjectPool<LazyLogMessage<TArg0>> s_pool = SharedPools.Default<LazyLogMessage<TArg0>>();

        private Func<TArg0, string>? _messageGetter;
        private TArg0? _arg;

        public static LogMessage Construct(Func<TArg0, string> messageGetter, TArg0 arg, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage._messageGetter = messageGetter;
            logMessage._arg = arg;
            logMessage.LogLevel = logLevel;

            return logMessage;
        }

        protected override string CreateMessage()
            => _messageGetter!(_arg!);

        protected override void FreeCore()
        {
            if (_messageGetter == null)
            {
                return;
            }

            _messageGetter = null;
            _arg = default;
            s_pool.Free(this);
        }
    }

    private sealed class LazyLogMessage<TArg0, TArg1> : LogMessage
    {
        private static readonly ObjectPool<LazyLogMessage<TArg0, TArg1>> s_pool = SharedPools.Default<LazyLogMessage<TArg0, TArg1>>();

        private Func<TArg0, TArg1, string>? _messageGetter;
        private TArg0? _arg0;
        private TArg1? _arg1;

        internal static LogMessage Construct(Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage._messageGetter = messageGetter;
            logMessage._arg0 = arg0;
            logMessage._arg1 = arg1;
            logMessage.LogLevel = logLevel;

            return logMessage;
        }

        protected override string CreateMessage()
            => _messageGetter!(_arg0!, _arg1!);

        protected override void FreeCore()
        {
            if (_messageGetter == null)
            {
                return;
            }

            _messageGetter = null;
            _arg0 = default;
            _arg1 = default;
            s_pool.Free(this);
        }
    }

    private sealed class LazyLogMessage<TArg0, TArg1, TArg2> : LogMessage
    {
        private static readonly ObjectPool<LazyLogMessage<TArg0, TArg1, TArg2>> s_pool = SharedPools.Default<LazyLogMessage<TArg0, TArg1, TArg2>>();

        private Func<TArg0, TArg1, TArg2, string>? _messageGetter;
        private TArg0? _arg0;
        private TArg1? _arg1;
        private TArg2? _arg2;

        public static LogMessage Construct(Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage._messageGetter = messageGetter;
            logMessage._arg0 = arg0;
            logMessage._arg1 = arg1;
            logMessage._arg2 = arg2;
            logMessage.LogLevel = logLevel;

            return logMessage;
        }

        protected override string CreateMessage()
            => _messageGetter!(_arg0!, _arg1!, _arg2!);

        protected override void FreeCore()
        {
            if (_messageGetter == null)
            {
                return;
            }

            _messageGetter = null;
            _arg0 = default;
            _arg1 = default;
            _arg2 = default;
            s_pool.Free(this);
        }
    }

    private sealed class LazyLogMessage<TArg0, TArg1, TArg2, TArg3> : LogMessage
    {
        private static readonly ObjectPool<LazyLogMessage<TArg0, TArg1, TArg2, TArg3>> s_pool = SharedPools.Default<LazyLogMessage<TArg0, TArg1, TArg2, TArg3>>();

        private Func<TArg0, TArg1, TArg2, TArg3, string>? _messageGetter;
        private TArg0? _arg0;
        private TArg1? _arg1;
        private TArg2? _arg2;
        private TArg3? _arg3;

        public static LogMessage Construct(Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, LogLevel logLevel)
        {
            var logMessage = s_pool.Allocate();
            logMessage._messageGetter = messageGetter;
            logMessage._arg0 = arg0;
            logMessage._arg1 = arg1;
            logMessage._arg2 = arg2;
            logMessage._arg3 = arg3;
            logMessage.LogLevel = logLevel;

            return logMessage;
        }

        protected override string CreateMessage()
            => _messageGetter!(_arg0!, _arg1!, _arg2!, _arg3!);

        protected override void FreeCore()
        {
            if (_messageGetter == null)
            {
                return;
            }

            _messageGetter = null;
            _arg0 = default;
            _arg1 = default;
            _arg2 = default;
            _arg3 = default;
            s_pool.Free(this);
        }
    }
}
