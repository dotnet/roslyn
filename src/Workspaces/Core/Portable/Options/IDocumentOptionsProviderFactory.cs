// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// A MEF-exported factory which produces <see cref="IDocumentOptionsProvider"/>s for <see cref="Workspace"/>s.
    /// </summary>
    interface IDocumentOptionsProviderFactory
    {
        IDocumentOptionsProvider? TryCreate(Workspace workspace);
    }
}
