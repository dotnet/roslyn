// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
