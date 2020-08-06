// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        private readonly struct QueueItem
        {
            public Func<RequestContext, Task> Callback { get; }
            public bool MutatesSolutionState { get; }
            public string ClientName { get; }
            public ClientCapabilities ClientCapabilities { get; }

            public QueueItem(bool mutatesSolutionState, ClientCapabilities clientCapabilities, string clientName, Func<RequestContext, Task> callback)
            {
                MutatesSolutionState = mutatesSolutionState;
                ClientCapabilities = clientCapabilities;
                ClientName = clientName;
                Callback = callback;
            }
        }
    }
}
