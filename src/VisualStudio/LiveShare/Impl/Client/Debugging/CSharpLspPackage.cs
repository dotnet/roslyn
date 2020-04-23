// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Debugging
{
    [Guid(StringConstants.CSharpLspPackageGuidString)]
    internal class CSharpLspPackage : AbstractPackage<CSharpLspPackage, CSharpLspLanguageService>
    {
        protected override VisualStudioWorkspaceImpl CreateWorkspace() => ComponentModel.GetService<VisualStudioWorkspaceImpl>();

        protected override string RoslynLanguageName => StringConstants.CSharpLspLanguageName;

        protected override IEnumerable<IVsEditorFactory> CreateEditorFactories()
            => SpecializedCollections.EmptyEnumerable<IVsEditorFactory>();

        protected override CSharpLspLanguageService CreateLanguageService() => new CSharpLspLanguageService(this);

        protected override void RegisterMiscellaneousFilesWorkspaceInformation(MiscellaneousFilesWorkspace miscellaneousFilesWorkspace)
        {
        }
    }
}
