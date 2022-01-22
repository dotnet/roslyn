// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
