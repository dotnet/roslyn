// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class RoslynRequestExecutionQueue : RequestExecutionQueue<RequestContext>
    {
        public RoslynRequestExecutionQueue(ILspLogger logger, IHandlerProvider handlerProvider)
            : base(logger, handlerProvider)
        {
        }

        public override Task HandleNonMutatingRequestErrorAsync(Task nonMutatingRequestTask)
        {
            return nonMutatingRequestTask.ReportNonFatalErrorAsync();
        }
    }
}
