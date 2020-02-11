﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal abstract class PythiaServiceBase : ServiceBase
    {
        protected PythiaServiceBase(IServiceProvider serviceProvider, Stream stream, IEnumerable<JsonConverter>? jsonConverters = null)
            : base(serviceProvider, stream, jsonConverters)
        {
        }

        protected new void StartService()
            => base.StartService();

        public Task<Solution> GetSolutionAsync(JObject solutionInfo, CancellationToken cancellationToken)
        {
            var reader = solutionInfo.CreateReader();
            var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Converters = new[] { AggregateJsonConverter.Instance } });
            var pinnedSolutionInfo = serializer.Deserialize<PinnedSolutionInfo>(reader);

            return CreateSolutionService(pinnedSolutionInfo).GetSolutionAsync(pinnedSolutionInfo, cancellationToken);
        }

        protected new Task<T> RunServiceAsync<T>(Func<Task<T>> callAsync, CancellationToken cancellationToken, [CallerMemberName]string? callerName = null)
            => base.RunServiceAsync<T>(callAsync, cancellationToken, callerName);

        protected new Task RunServiceAsync(Func<Task> callAsync, CancellationToken cancellationToken, [CallerMemberName]string? callerName = null)
            => base.RunServiceAsync(callAsync, cancellationToken, callerName);
    }
}
