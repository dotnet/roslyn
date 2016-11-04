// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Service that allows you to query the SymbolSearch database and which keeps 
    /// the database up to date.  
    /// </summary>
    internal interface ISymbolSearchUpdateEngine
    {
        Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory);

        Task<ImmutableArray<PackageWithTypeInfo>> FindPackagesWithTypeAsync(
            PackageSource source, string name, int arity);
        Task<ImmutableArray<PackageWithAssemblyInfo>> FindPackagesWithAssemblyAsync(
            PackageSource source, string assemblyName);
        Task<ImmutableArray<ReferenceAssemblyWithTypeInfo>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity);
    }
}