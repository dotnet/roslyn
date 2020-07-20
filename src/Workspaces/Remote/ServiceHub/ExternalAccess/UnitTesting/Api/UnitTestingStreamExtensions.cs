// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal static class UnitTestingStreamExtensions
    {
        public static JsonRpc UnitTesting_CreateStreamJsonRpc(
            this Stream stream,
            object? target,
            TraceSource logger,
            IEnumerable<AggregateJsonConverter>? jsonConverters = null)
        {
            jsonConverters ??= SpecializedCollections.EmptyEnumerable<AggregateJsonConverter>();

            var jsonFormatter = new JsonMessageFormatter();
            jsonFormatter.JsonSerializer.Converters.AddRange(jsonConverters.Concat(AggregateJsonConverter.Instance));

            return new JsonRpc(new HeaderDelimitedMessageHandler(stream, jsonFormatter), target)
            {
                CancelLocallyInvokedMethodsWhenConnectionIsClosed = true,
                TraceSource = logger
            };
        }
    }
}
