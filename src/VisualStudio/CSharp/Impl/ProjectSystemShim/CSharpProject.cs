// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    /// <summary>
    /// The representation of a project to both the project factory and workspace API.
    /// </summary>
    /// <remarks>
    /// Due to the number of interfaces this object must implement, all interface implementations
    /// are in a separate files. Methods that are shared across multiple interfaces (which are
    /// effectively methods that just QI from one interface to another), are implemented here.
    /// </remarks>
    internal partial class CSharpProject : AbstractEncProject
    {
        private static readonly CSharpCompilationOptions s_defaultCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        private static readonly CSharpParseOptions s_defaultParseOptions = new CSharpParseOptions();

        protected CSharpProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            IServiceProvider serviceProvider,
            MiscellaneousFilesWorkspace miscellaneousFilesWorkspaceOpt,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
            : base(projectTracker,
                   reportExternalErrorCreatorOpt,
                   projectSystemName,
                   hierarchy,
                   LanguageNames.CSharp,
                   serviceProvider,
                   miscellaneousFilesWorkspaceOpt,
                   visualStudioWorkspaceOpt,
                   hostDiagnosticUpdateSourceOpt)
        {
            InitializeOptions();

            projectTracker.AddProject(this);
        }

        protected virtual void InitializeOptions()
        {
            this.SetOptions(this.CreateCompilationOptions(), this.CreateParseOptions());
        }

        protected virtual CSharpCompilationOptions CreateCompilationOptions()
        {
            return s_defaultCompilationOptions;
        }

        protected virtual CSharpParseOptions CreateParseOptions()
        {
            return s_defaultParseOptions;
        }

        protected override void UpdateAnalyzerRules()
        {
            base.UpdateAnalyzerRules();

            this.SetOptions(this.CreateCompilationOptions(), this.CreateParseOptions());
        }
    }
}
