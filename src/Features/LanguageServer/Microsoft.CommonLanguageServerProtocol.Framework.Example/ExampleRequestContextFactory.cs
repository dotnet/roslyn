// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;ple;

internal class ExampleRequestContextFactory : IRequestContextFactory<ExampleRequestContext>
{
    private readonly ILspServices _lspServices;

    public ExampleRequestContextFactory(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public Task<ExampleRequestContext?> CreateRequestContextAsync(IQueueItem<ExampleRequestContext> queueItem, CancellationToken cancellationToken)
    {
        var logger = _lspServices.GetRequiredService<ILspLogger>();

        var requestContext = new ExampleRequestContext(_lspServices, logger);

        return Task.FromResult<ExampleRequestContext?>(requestContext);
    }
}
