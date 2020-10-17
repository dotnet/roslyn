﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal interface ICompilationVerifier
    {
        ImmutableArray<ModuleMetadata> GetAllModuleMetadata();
        IModuleSymbol GetModuleSymbolForEmittedImage(ImmutableArray<byte> peImage, MetadataImportOptions importOptions);
        IModuleSymbol GetModuleSymbolForEmittedImage();
        ImmutableArray<byte> EmittedAssemblyData { get; }
    }
}
