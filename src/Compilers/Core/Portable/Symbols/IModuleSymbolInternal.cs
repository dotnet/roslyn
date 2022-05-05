// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface IModuleSymbolInternal : ISymbolInternal
    {
        /// <summary>
        /// Gets the first unsupported CompilerFeatureRequired string, if one exists. Null if there are none.
        /// </summary>
        string? GetUnsupportedCompilerFeature();
        new IAssemblySymbolInternal ContainingAssembly { get; }
    }
}
