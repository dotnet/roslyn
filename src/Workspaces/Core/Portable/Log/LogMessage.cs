// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// log message that can generate string lazily
    /// </summary>
    internal abstract class LogMessage
    {
        public static LogMessage Create(string message)
        {
            return StaticLogMessage.Construct(message);
        }

        public static LogMessage Create(Func<string> messageGetter)
        {
            return LazyLogMessage.Construct(messageGetter);
        }

        public static LogMessage Create<TArg>(Func<TArg, string> messageGetter, TArg arg)
        {
            return LazyLogMessage<TArg>.Construct(messageGetter, arg);
        }

        public static LogMessage Create<TArg0, TArg1>(Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1)
        {
            return LazyLogMessage<TArg0, TArg1>.Construct(messageGetter, arg0, arg1);
        }

        public static LogMessage Create<TArg0, TArg1, TArg2>(Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            return LazyLogMessage<TArg0, TArg1, TArg2>.Construct(messageGetter, arg0, arg1, arg2);
        }

        public static LogMessage Create<TArg0, TArg1, TArg2, TArg3>(Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return LazyLogMessage<TArg0, TArg1, TArg2, TArg3>.Construct(messageGetter, arg0, arg1, arg2, arg3);
        }

        // message will be either initially set or lazily set by caller
        private string message;

        protected abstract string CreateMessage();

        /// <summary>
        /// Logger will call this to return LogMessage to its pool
        /// </summary>
        public abstract void Free();

        public string GetMessage()
        {
            if (this.message == null)
            {
                this.message = CreateMessage();
            }

            return this.message;
        }

        private sealed class StaticLogMessage : LogMessage
        {
            private static readonly ObjectPool<StaticLogMessage> Pool = SharedPools.Default<StaticLogMessage>();

            public static LogMessage Construct(string message)
            {
                var logMessage = Pool.Allocate();
                logMessage.message = message;

                return logMessage;
            }

            protected override string CreateMessage()
            {
                return this.message;
            }

            public override void Free()
            {
                if (this.message == null)
                {
                    return;
                }

                this.message = null;
                Pool.Free(this);
            }
        }

        private sealed class LazyLogMessage : LogMessage
        {
            private static readonly ObjectPool<LazyLogMessage> Pool = SharedPools.Default<LazyLogMessage>();

            private Func<string> messageGetter;

            public static LogMessage Construct(Func<string> messageGetter)
            {
                var logMessage = Pool.Allocate();
                logMessage.messageGetter = messageGetter;

                return logMessage;
            }

            protected override string CreateMessage()
            {
                return this.messageGetter();
            }

            public override void Free()
            {
                if (this.messageGetter == null)
                {
                    return;
                }

                this.messageGetter = null;
                Pool.Free(this);
            }
        }

        private sealed class LazyLogMessage<TArg0> : LogMessage
        {
            private static readonly ObjectPool<LazyLogMessage<TArg0>> Pool = SharedPools.Default<LazyLogMessage<TArg0>>();

            private Func<TArg0, string> messageGetter;
            private TArg0 arg;

            public static LogMessage Construct(Func<TArg0, string> messageGetter, TArg0 arg)
            {
                var logMessage = Pool.Allocate();
                logMessage.messageGetter = messageGetter;
                logMessage.arg = arg;

                return logMessage;
            }

            protected override string CreateMessage()
            {
                return this.messageGetter(this.arg);
            }

            public override void Free()
            {
                if (this.messageGetter == null)
                {
                    return;
                }

                this.messageGetter = null;
                this.arg = default(TArg0);
                Pool.Free(this);
            }
        }

        private sealed class LazyLogMessage<TArg0, TArg1> : LogMessage
        {
            private static readonly ObjectPool<LazyLogMessage<TArg0, TArg1>> Pool = SharedPools.Default<LazyLogMessage<TArg0, TArg1>>();

            private Func<TArg0, TArg1, string> messageGetter;
            private TArg0 arg0;
            private TArg1 arg1;

            internal static LogMessage Construct(Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1)
            {
                var logMessage = Pool.Allocate();
                logMessage.messageGetter = messageGetter;
                logMessage.arg0 = arg0;
                logMessage.arg1 = arg1;

                return logMessage;
            }

            protected override string CreateMessage()
            {
                return this.messageGetter(arg0, arg1);
            }

            public override void Free()
            {
                if (this.messageGetter == null)
                {
                    return;
                }

                this.messageGetter = null;
                this.arg0 = default(TArg0);
                this.arg1 = default(TArg1);
                Pool.Free(this);
            }
        }

        private sealed class LazyLogMessage<TArg0, TArg1, TArg2> : LogMessage
        {
            private static readonly ObjectPool<LazyLogMessage<TArg0, TArg1, TArg2>> Pool = SharedPools.Default<LazyLogMessage<TArg0, TArg1, TArg2>>();

            private Func<TArg0, TArg1, TArg2, string> messageGetter;
            private TArg0 arg0;
            private TArg1 arg1;
            private TArg2 arg2;

            public static LogMessage Construct(Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2)
            {
                var logMessage = Pool.Allocate();
                logMessage.messageGetter = messageGetter;
                logMessage.arg0 = arg0;
                logMessage.arg1 = arg1;
                logMessage.arg2 = arg2;

                return logMessage;
            }

            protected override string CreateMessage()
            {
                return this.messageGetter(arg0, arg1, arg2);
            }

            public override void Free()
            {
                if (this.messageGetter == null)
                {
                    return;
                }

                this.messageGetter = null;
                this.arg0 = default(TArg0);
                this.arg1 = default(TArg1);
                this.arg2 = default(TArg2);
                Pool.Free(this);
            }
        }

        private sealed class LazyLogMessage<TArg0, TArg1, TArg2, TArg3> : LogMessage
        {
            private static readonly ObjectPool<LazyLogMessage<TArg0, TArg1, TArg2, TArg3>> Pool = SharedPools.Default<LazyLogMessage<TArg0, TArg1, TArg2, TArg3>>();

            private Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter;
            private TArg0 arg0;
            private TArg1 arg1;
            private TArg2 arg2;
            private TArg3 arg3;

            public static LogMessage Construct(Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
            {
                var logMessage = Pool.Allocate();
                logMessage.messageGetter = messageGetter;
                logMessage.arg0 = arg0;
                logMessage.arg1 = arg1;
                logMessage.arg2 = arg2;
                logMessage.arg3 = arg3;

                return logMessage;
            }

            protected override string CreateMessage()
            {
                return this.messageGetter(arg0, arg1, arg2, arg3);
            }

            public override void Free()
            {
                if (this.messageGetter == null)
                {
                    return;
                }

                this.messageGetter = null;
                this.arg0 = default(TArg0);
                this.arg1 = default(TArg1);
                this.arg2 = default(TArg2);
                this.arg3 = default(TArg3);
                Pool.Free(this);
            }
        }
    }
}
