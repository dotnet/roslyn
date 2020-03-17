// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface IFieldSymbolInternal : ISymbolInternal
    {
        /// <summary>
        /// Returns true if this field was declared as "volatile". 
        /// </summary>
        bool IsVolatile { get; }
    }
}
