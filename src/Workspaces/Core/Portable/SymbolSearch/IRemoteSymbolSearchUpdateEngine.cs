// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface IRemoteSymbolSearchUpdateService
    {
        internal interface ICallback
        {
            ValueTask LogExceptionAsync(RemoteServiceCallbackId callbackId, string exception, string text, CancellationToken cancellationToken);
            ValueTask LogInfoAsync(RemoteServiceCallbackId callbackId, string text, CancellationToken cancellationToken);
        }

        ValueTask UpdateContinuouslyAsync(RemoteServiceCallbackId callbackId, string sourceName, string localSettingsDirectory, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string name, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken);
    }
}
