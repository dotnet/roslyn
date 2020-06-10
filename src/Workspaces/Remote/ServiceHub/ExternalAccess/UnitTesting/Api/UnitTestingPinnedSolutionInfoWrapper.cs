// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Remote;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal readonly struct UnitTestingPinnedSolutionInfoWrapper
    {
        /// <summary>
        /// Because PinnedSolutionInfo is internal, and it is directly passed into the callee in
        /// <see cref="RemoteServiceConnection.RunRemoteAsync{T}(string, Solution, System.Collections.Generic.IReadOnlyList{object}, System.Threading.CancellationToken)"/>
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
