// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    // NOTE: Microsoft.VisualStudio.LanguageServices.TypeScript.TypeScriptProject derives from AbstractProject.
#pragma warning disable CS0618 // IVisualStudioHostProject is obsolete
    internal abstract partial class AbstractProject : ForegroundThreadAffinitizedObject, IVisualStudioHostProject
#pragma warning restore CS0618 // IVisualStudioHostProject is obsolete
    {
        internal const string ProjectGuidPropertyName = "ProjectGuid";

        internal static object RuleSetErrorId = new object();

        private readonly DiagnosticDescriptor _errorReadingRulesetRule = new DiagnosticDescriptor(
            id: IDEDiagnosticIds.ErrorReadingRulesetId,
            title: ServicesVSResources.ErrorReadingRuleset,
            messageFormat: ServicesVSResources.Error_reading_ruleset_file_0_1,
            category: FeaturesResources.Roslyn_HostError,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


        public AbstractProject(
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            string projectFilePath,
            IVsHierarchy hierarchy,
            string language,
            Guid projectGuid,
            IServiceProvider serviceProviderNotUsed, // not used, but left for compat with TypeScript
            IThreadingContext threadingContext,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt = null)
            : base(threadingContext)
        {
            Hierarchy = hierarchy;
            Guid = projectGuid;

            var displayName = hierarchy != null && hierarchy.TryGetName(out var name) ? name : projectSystemName;
            this.DisplayName = displayName;

            ProjectSystemName = projectSystemName;
            HostDiagnosticUpdateSource = hostDiagnosticUpdateSourceOpt;

            // Set the default value for last design time build result to be true, until the project system lets us know that it failed.
            LastDesignTimeBuildSucceeded = true;

            if (ProjectFilePath != null)
            {
                Version = VersionStamp.Create(File.GetLastWriteTimeUtc(ProjectFilePath));
            }
            else
            {
                Version = VersionStamp.Create();
            }

            if (reportExternalErrorCreatorOpt != null)
            {
                ExternalErrorReporter = reportExternalErrorCreatorOpt(Id);
            }
        }

        /// <summary>
        /// Indicates whether this project is a website type.
        /// </summary>
        public bool IsWebSite { get; protected set; }

        /// <summary>
        /// A full path to the project obj output binary, or null if the project doesn't have an obj output binary.
        /// </summary>
        internal string ObjOutputPath { get; private set; }

        /// <summary>
        /// A full path to the project bin output binary, or null if the project doesn't have an bin output binary.
        /// </summary>
        internal string BinOutputPath { get; private set; }

        public IReferenceCountedDisposable<IRuleSetFile> RuleSetFile { get; private set; }

        protected IVsReportExternalErrors ExternalErrorReporter { get; }

        internal HostDiagnosticUpdateSource HostDiagnosticUpdateSource { get; }

        public ProjectId Id { get; set; }

        public string Language { get; }

        /// <summary>
        /// The <see cref="IVsHierarchy"/> for this project.  NOTE: May be null in Deferred Project Load cases.
        /// </summary>
        public IVsHierarchy Hierarchy { get; }

        /// <summary>
        /// Guid of the project
        /// 
        /// it is not readonly since it can be changed while loading project
        /// </summary>
        public Guid Guid { get; protected set; }

        public Workspace Workspace { get; }

        public VersionStamp Version { get; }

        public IProjectCodeModel ProjectCodeModel { get; protected set; }
        
        /// <summary>
        /// The containing directory of the project. Null if none exists (consider Venus.)
        /// </summary>
        protected string ContainingDirectoryPathOpt
        {
            get
            {
                var projectFilePath = this.ProjectFilePath;
                if (projectFilePath != null)
                {
                    return Path.GetDirectoryName(projectFilePath);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The full path of the project file. Null if none exists (consider Venus.)
        /// Note that the project file path might change with project file rename.
        /// If you need the folder of the project, just use <see cref="ContainingDirectoryPathOpt" /> which doesn't change for a project.
        /// </summary>
        public string ProjectFilePath { get; private set; }

        /// <summary>
        /// The public display name of the project. This name is not unique and may be shared
        /// between multiple projects, especially in cases like Venus where the intellisense
        /// projects will match the name of their logical parent project.
        /// </summary>
        public string DisplayName { get; private set; }

        internal string AssemblyName { get; private set; }

        /// <summary>
        /// The name of the project according to the project system. In "regular" projects this is
        /// equivalent to <see cref="DisplayName"/>, but in Venus cases these will differ. The
        /// ProjectSystemName is the 2_Default.aspx project name, whereas the regular display name
        /// matches the display name of the project the user actually sees in the solution explorer.
        /// These can be assumed to be unique within the Visual Studio workspace.
        /// </summary>
        public string ProjectSystemName { get; }

        /// <summary>
        /// Flag indicating if the latest design time build has succeeded for current project state.
        /// </summary>
        /// <remarks>Default value is true.</remarks>
        protected bool LastDesignTimeBuildSucceeded { get; private set; }
    }
}
