// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IPooledConnectionReclamation
    {
        /// <summary>
        /// Returns <see cref="JsonRpcConnection"/> instance to the pool it was allocated from.
        /// </summary>
        void Return(JsonRpcConnection connection);
    }
}
