// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    [ExportLanguageService(typeof(IProjectExistsUIContextProviderLanguageService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpProjectExistsUIContextProviderLanguageService : IProjectExistsUIContextProviderLanguageService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpProjectExistsUIContextProviderLanguageService()
        {
        }

        public UIContext GetUIContext()
            => UIContext.FromUIContextGuid(Guids.CSharpProjectExistsInWorkspaceUIContext);
    }
}
