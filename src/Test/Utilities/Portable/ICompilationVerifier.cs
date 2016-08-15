// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
