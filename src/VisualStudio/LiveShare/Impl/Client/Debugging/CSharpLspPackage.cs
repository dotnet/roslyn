// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Debugging
{
    [Guid(StringConstants.CSharpLspPackageGuidString)]
    [ProvideLanguageService(StringConstants.CSharpLspLanguageServiceGuidString, StringConstants.CSharpLspLanguageName, 101,
        RequestStockColors = true, ShowDropDownOptions = true, ShowCompletion = true, EnableAdvancedMembersOption = true, ShowSmartIndent = true, DefaultToInsertSpaces = true)]
    [ProvideService(typeof(CSharpLspLanguageService))]
    internal class CSharpLspPackage : AbstractPackage<CSharpLspPackage, CSharpLspLanguageService>
    {
        protected override VisualStudioWorkspaceImpl CreateWorkspace() => ComponentModel.GetService<VisualStudioWorkspaceImpl>();

        protected override string RoslynLanguageName => StringConstants.CSharpLspLanguageName;

        protected override IEnumerable<IVsEditorFactory> CreateEditorFactories()
        {
            return new IVsEditorFactory[] { };
        }

        protected override CSharpLspLanguageService CreateLanguageService() => new CSharpLspLanguageService(this);

        protected override void RegisterMiscellaneousFilesWorkspaceInformation(MiscellaneousFilesWorkspace miscellaneousFilesWorkspace)
        {
        }
    }
}
