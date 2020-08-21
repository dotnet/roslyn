// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : ServiceBase
    {
        internal sealed class ServiceFactory : IServiceHubServiceFactory
        {
            public Task<object> CreateAsync(
                Stream stream,
                IServiceProvider hostProvidedServices,
                ServiceActivationOptions serviceActivationOptions,
                IServiceBroker serviceBroker,
                AuthorizationServiceClient authorizationServiceClient)
            {
                // Dispose the AuthorizationServiceClient since we won't be using it
                authorizationServiceClient.Dispose();

                return Task.FromResult<object>(new CodeAnalysisService(stream, hostProvidedServices));
            }
        }

        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;

        public CodeAnalysisService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            _analyzerInfoCache = new DiagnosticAnalyzerInfoCache();

            StartService();
        }
    }
}
