// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("initialized", LanguageServerConstants.DefaultLanguageName)]
internal sealed class InitializedHandler<TRequest, TRequestContext> : INotificationHandler<TRequest, TRequestContext>
{
    private bool HasBeenInitialized = false;

    public bool MutatesSolutionState => true;

    public Task HandleNotificationAsync(TRequest request, TRequestContext requestContext, CancellationToken cancellationToken)
    {
        if (HasBeenInitialized)
        {
            throw new InvalidOperationException("initialized was called twice");
        }

        HasBeenInitialized = true;

        return Task.CompletedTask;
    }
}
