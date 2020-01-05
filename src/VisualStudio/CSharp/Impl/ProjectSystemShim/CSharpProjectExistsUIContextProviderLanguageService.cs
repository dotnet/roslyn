// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        public UIContext GetUIContext()
        {
            return UIContext.FromUIContextGuid(Guids.CSharpProjectExistsInWorkspaceUIContext);
        }
    }
}
