// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options;

internal interface IOptionPersisterProvider
{
    /// <summary>
    /// Gets the <see cref="IOptionPersister"/>. If the persister does not already exist, it is created.
    /// </summary>
    /// <remarks>
    /// This method is safe for concurrent use from any thread. No guarantees are made regarding the use of the UI
    /// thread.
    /// </remarks>
    /// <returns>The option persister.</returns>
    IOptionPersister GetOrCreatePersister();
}
