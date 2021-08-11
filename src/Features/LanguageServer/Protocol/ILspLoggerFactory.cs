﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    internal interface ILspLoggerFactory
    {
        Task<ILspLogger> CreateLoggerAsync(string serverTypeName, string? clientName, JsonRpc jsonRpc, CancellationToken cancellationToken);
    }
}
