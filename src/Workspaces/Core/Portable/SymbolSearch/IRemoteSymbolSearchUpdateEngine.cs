// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface IRemoteSymbolSearchUpdateEngine
    {
        Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory);

        Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken);
        Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string name, CancellationToken cancellationToken);
        Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken);
    }
}
