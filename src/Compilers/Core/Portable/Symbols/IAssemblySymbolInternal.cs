// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface IAssemblySymbolInternal : ISymbolInternal
    {
        Version? AssemblyVersionPattern { get; }

        /// <summary>
        /// Gets the name of this assembly.
        /// </summary>
        AssemblyIdentity Identity { get; }
    }
}
