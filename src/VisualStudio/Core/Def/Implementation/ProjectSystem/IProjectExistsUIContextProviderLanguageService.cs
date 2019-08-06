// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Implemented by an language that wants a <see cref="UIContext"/> to be activated when there is a project of a given language in the workspace.
    /// </summary>
    internal interface IProjectExistsUIContextProviderLanguageService : ILanguageService
    {
        UIContext GetUIContext();
    }
}
