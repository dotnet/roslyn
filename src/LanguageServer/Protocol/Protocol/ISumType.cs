// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Abstracts over the idea of a "sum type". Sum types are types that can contain one value of various types.
    /// This abstraction is guaranteed to be typesafe, meaning you cannot access the underlying value without knowing
    /// its specific type.
    /// </summary>
    internal interface ISumType
    {
        /// <summary>
        /// Gets the value stored in the SumType. This can be matched against using the "is" operator.
        /// </summary>
        object? Value { get; }
    }
}
