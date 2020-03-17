﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // Used by Razor: https://github.com/aspnet/AspNetCore-Tooling/blob/master/src/Razor/src/Microsoft.CodeAnalysis.Remote.Razor/RazorServiceBase.cs
    internal abstract class ServiceHubServiceBase : ServiceBase
    {
        private PinnedSolutionInfo? _solutionInfo;

        protected ServiceHubServiceBase(IServiceProvider serviceProvider, Stream stream, IEnumerable<JsonConverter>? jsonConverters = null)
            : base(serviceProvider, stream, jsonConverters)
        {
        }

        /// <summary>
        /// Invoked remotely - <see cref="WellKnownServiceHubServices.ServiceHubServiceBase_Initialize"/>
        /// </summary>
        [Obsolete]
        public virtual void Initialize(PinnedSolutionInfo info)
        {
            _solutionInfo = info;
        }

        [Obsolete("Use GetSolutionAsync(JObject, CancellationToken) instead")]
        protected Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        {
            // must be initialized
            Contract.ThrowIfNull(_solutionInfo);

            return GetSolutionAsync(_solutionInfo, cancellationToken);
        }

        public Task<Solution> GetSolutionAsync(JObject solutionInfo, CancellationToken cancellationToken)
        {
            var reader = solutionInfo.CreateReader();
            var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Converters = new[] { AggregateJsonConverter.Instance } });
            var pinnedSolutionInfo = serializer.Deserialize<PinnedSolutionInfo>(reader);

            return CreateSolutionService(pinnedSolutionInfo).GetSolutionAsync(pinnedSolutionInfo, cancellationToken);
        }
    }
}
