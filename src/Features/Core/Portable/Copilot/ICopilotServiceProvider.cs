// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotServiceProvider : IWorkspaceService
{
    public Task<ImmutableArray<string>?> SendOneOffRequestAsync(ImmutableArray<string> promptParts, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ICopilotServiceProvider), ServiceLayer.Default), Shared]
internal sealed class DefaultCopilotServiceProvider : ICopilotServiceProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultCopilotServiceProvider()
    {
    }

    public Task<ImmutableArray<string>?> SendOneOffRequestAsync(ImmutableArray<string> promptParts, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
