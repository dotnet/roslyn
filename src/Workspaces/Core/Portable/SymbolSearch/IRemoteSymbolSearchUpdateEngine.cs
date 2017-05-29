// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface IRemoteSymbolSearchUpdateEngine
    {
        Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken);

        Task<SerializablePackageWithTypeResult[]> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken);
        Task<SerializablePackageWithAssemblyResult[]> FindPackagesWithAssemblyAsync(string source, string name, CancellationToken cancellationToken);
        Task<SerializableReferenceAssemblyWithTypeResult[]> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken);
    }
}