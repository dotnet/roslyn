// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Reflection;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface ISourceAssemblySymbolInternal : IAssemblySymbolInternal
    {
        AssemblyFlags AssemblyFlags { get; }

        /// <summary>
        /// The contents of the AssemblySignatureKeyAttribute
        /// </summary>
        string? SignatureKey { get; }

        AssemblyHashAlgorithm HashAlgorithm { get; }

        bool InternalsAreVisible { get; }
    }
}
