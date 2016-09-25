// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchUpdateEngine : IDisposable
    {
        Task UpdateContinuouslyAsync(
            string sourceName, string localSettingsDirectory);
        Task StopUpdatesAsync();

        Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity);
        Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity);
    }
}