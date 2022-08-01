// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal class RoslynRequestExecutionQueue : RequestExecutionQueue<RequestContext>, ILspService
{
    private IRequestContextFactory<RequestContext>? _requestContextFactory;

    public RoslynRequestExecutionQueue(string serverKind, ILspLogger logger) : base(serverKind, logger)
    {
    }

    protected override IRequestContextFactory<RequestContext> GetRequestContextFactory(ILspServices lspServices)
    {
        if (_requestContextFactory is null)
        {
            _requestContextFactory = lspServices.GetRequiredService<IRequestContextFactory<RequestContext>>();
        }

        return _requestContextFactory;
    }
}
