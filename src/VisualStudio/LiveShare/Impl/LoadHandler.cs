// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using CustomMethods = Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol.CustomMethods;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class LoadHandler : ILspRequestHandler, ILspRequestHandler<object, object>
    {
        public Task<object> HandleAsync(object request, RequestContext requestContext, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, CustomMethods.LoadName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynLoadHandler : LoadHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, CustomMethods.LoadName)]
    internal class CSharpLoadHandler : LoadHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, CustomMethods.LoadName)]
    internal class VisualBasicLoadHandler : LoadHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, CustomMethods.LoadName)]
    internal class TypeScriptLoadHandler : LoadHandler
    {
    }
}
