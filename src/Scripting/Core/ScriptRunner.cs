// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// A delegate that will run a script when invoked.
    /// </summary>
    /// <param name="globals">An object instance whose members can be accessed by the script as global variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">The type of <paramref name="globals"/> doesn't match the corresponding <see cref="Script.GlobalsType"/>.</exception>
    public delegate Task<T> ScriptRunner<T>(object globals = null, CancellationToken cancellationToken = default(CancellationToken));
}
