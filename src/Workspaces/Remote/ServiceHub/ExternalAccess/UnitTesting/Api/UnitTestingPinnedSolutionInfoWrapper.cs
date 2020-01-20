﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingPinnedSolutionInfoWrapper
    {
        /// <summary>
        /// Because PinnedSolutionInfo is internal, and it is directly passed into the callee in
        /// <see cref="KeepAliveSession.TryInvokeAsync{T}(string, Solution, System.Collections.Generic.IReadOnlyList{object}, System.Threading.CancellationToken)"/>
        /// the type of <param name="underlyingObject"/> has to be object. Its runtime type is <see cref="JObject"/>.
        /// </summary>
        public UnitTestingPinnedSolutionInfoWrapper(object underlyingObject)
        {
            var reader = ((JObject)underlyingObject).CreateReader();
            var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Converters = new[] { AggregateJsonConverter.Instance } });
            UnderlyingObject = serializer.Deserialize<PinnedSolutionInfo>(reader);
        }

        internal PinnedSolutionInfo UnderlyingObject { get; }
    }
}
