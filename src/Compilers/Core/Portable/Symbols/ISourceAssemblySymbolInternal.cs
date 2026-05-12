// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface ISourceAssemblySymbolInternal : IAssemblySymbolInternal
    {
        AssemblyFlags AssemblyFlags { get; }

        /// <summary>
        /// The public key portion of the <see cref="System.Reflection.AssemblySignatureKeyAttribute"/>.
        /// This is used in strong name key migration. When non-null, the assembly uses 
        /// the key migration mechanism and requires counter-signature verification during signing.
        /// </summary>
        string? SignatureKey { get; }

        AssemblyHashAlgorithm HashAlgorithm { get; }

        bool InternalsAreVisible { get; }
    }
}
