// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// A MEF-exported factory which produces <see cref="IDocumentOptionsProvider"/>s for <see cref="Workspace"/>s.
    /// </summary>
    internal interface IDocumentOptionsProviderFactory
    {
        IDocumentOptionsProvider? TryCreate(Workspace workspace);
    }
}
