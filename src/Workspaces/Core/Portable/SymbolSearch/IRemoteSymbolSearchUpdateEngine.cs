// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface IRemoteSymbolSearchUpdateEngine
    {
        Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory);

        Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity);
        Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string name);
        Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity);
    }
}