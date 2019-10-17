// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Remote;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingStreamExtensions
    {
        public static JsonRpc UnitTesting_CreateStreamJsonRpc(
            this Stream stream,
            object target,
            TraceSource logger,
            IEnumerable<AggregateJsonConverter> jsonConverters = null)
            => stream.CreateStreamJsonRpc(target, logger, jsonConverters);
    }
}
