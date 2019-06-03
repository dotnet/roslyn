﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.LoadName)]
    internal class LoadHandler : ILspRequestHandler, ILspRequestHandler<object, object>
    {
        public Task<object> HandleAsync(object request, RequestContext requestContext, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }
    }
}
