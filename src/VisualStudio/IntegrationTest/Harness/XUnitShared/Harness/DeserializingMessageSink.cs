// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using Xunit.Runner.Common;
    using Xunit.Sdk;
    using Xunit.Threading;

    public sealed class DeserializingMessageSink : LongLivedMarshalByRefObject
    {
        private readonly IMessageSink _executionMessageSink;

        public DeserializingMessageSink(IMessageSink executionMessageSink)
        {
            _executionMessageSink = executionMessageSink;
        }

        public bool OnMessage(string json)
        {
            return _executionMessageSink.OnMessage(MessageSinkMessageDeserializer.Deserialize(json, diagnosticMessageSink: null)!);
        }
    }
}
