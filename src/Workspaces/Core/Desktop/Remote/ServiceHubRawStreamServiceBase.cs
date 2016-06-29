// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal abstract class ServiceHubRawStreamServiceBase : ServiceHubServiceBase
    {
        public ServiceHubRawStreamServiceBase(Stream stream, IServiceProvider serviceProvider) : base(stream, serviceProvider)
        {
            // REVIEW: ServiceHub API is a bit wierd. workflow is not through traditional control flow but by dispatching message from stream
            Task.Run(WorkerWithExceptionHandlerAsync);
        }

        private async Task WorkerWithExceptionHandlerAsync()
        {
            try
            {
                await WorkerAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // REVIEW: should this exception sent back to client rather than swallow it? at least non-fatal watson?
                //         think about a way to use Telemetry helper for it
                LogError(ex.ToString());
            }
        }

        // do actual work here
        protected abstract Task WorkerAsync();
    }
}
