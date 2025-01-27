// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal interface ILspServiceLoggerFactory
    {
        Task<AbstractLspLogger> CreateLoggerAsync(string serverTypeName, JsonRpc jsonRpc, CancellationToken cancellationToken);
    }
}
