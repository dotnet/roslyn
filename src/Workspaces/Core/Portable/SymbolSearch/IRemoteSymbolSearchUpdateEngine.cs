// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface IRemoteSymbolSearchUpdateEngine
    {
        Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory);

        Task<PackageWithTypeResult[]> FindPackagesWithTypeAsync(string source, string name, int arity);
        Task<PackageWithAssemblyResult[]> FindPackagesWithAssemblyAsync(string source, string name);
        Task<ReferenceAssemblyWithTypeResult[]> FindReferenceAssembliesWithTypeAsync(string name, int arity);
    }
}