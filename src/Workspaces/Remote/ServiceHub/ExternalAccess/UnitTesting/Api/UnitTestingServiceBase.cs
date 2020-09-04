// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal abstract class UnitTestingServiceBase : ServiceBase
    {
        protected UnitTestingServiceBase(
            IServiceProvider serviceProvider,
            Stream stream,
            IEnumerable<JsonConverter>? jsonConverters = null) : base(serviceProvider, stream, jsonConverters)
        {
        }

        protected new void StartService()
            => base.StartService();

        protected Task<Solution> GetSolutionAsync(JObject solutionInfo, CancellationToken cancellationToken)
            => GetSolutionImplAsync(solutionInfo, cancellationToken);

        protected new Task<T> RunServiceAsync<T>(Func<Task<T>> callAsync, CancellationToken cancellationToken)
            => base.RunServiceAsync(callAsync, cancellationToken);

        protected new Task RunServiceAsync(Func<Task> callAsync, CancellationToken cancellationToken)
            => base.RunServiceAsync(callAsync, cancellationToken);

        protected Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => EndPoint.InvokeAsync<T>(targetName, arguments, cancellationToken);

        protected Task InvokeAsync(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => EndPoint.InvokeAsync(targetName, arguments, cancellationToken);

        public UnitTestingIncrementalAnalyzerProvider? TryRegisterAnalyzerProvider(string analyzerName, IUnitTestingIncrementalAnalyzerProviderImplementation provider)
            => UnitTestingIncrementalAnalyzerProvider.TryRegister(GetWorkspace(), analyzerName, provider);
    }
}
