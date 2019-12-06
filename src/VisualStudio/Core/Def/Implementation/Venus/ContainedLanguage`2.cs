// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    internal partial class ContainedLanguage<TPackage, TLanguageService> : ContainedLanguage
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        public ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            AbstractProject project,
            IVsHierarchy hierarchy,
            uint itemid,
            TLanguageService languageService,
            SourceCodeKind sourceCodeKind,
            IFormattingRule vbHelperFormattingRule,
            Workspace workspace)
            : base(bufferCoordinator,
                   componentModel,
                   project.VisualStudioProject,
                   hierarchy,
                   itemid,
                   project.ProjectTracker,
                   project.Id,
                   languageService.LanguageServiceId,
                   vbHelperFormattingRule: null)
        {
            Contract.ThrowIfTrue(vbHelperFormattingRule != null);
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        public ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            AbstractProject project,
            IVsHierarchy hierarchy,
            uint itemid,
            TLanguageService languageService,
            SourceCodeKind sourceCodeKind,
            IFormattingRule vbHelperFormattingRule)
            : base(bufferCoordinator,
                   componentModel,
                   project.VisualStudioProject,
                   hierarchy,
                   itemid,
                   projectTrackerOpt: null,
                   project.VisualStudioProject.Id,
                   languageService.LanguageServiceId,
                   vbHelperFormattingRule: null)
        {
            Contract.ThrowIfTrue(vbHelperFormattingRule != null);
        }
    }
}
