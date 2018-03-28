// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(IWorkspaceProjectContextFactory))]
    internal partial class CPSProjectFactory : ForegroundThreadAffinitizedObject, IWorkspaceProjectContextFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspace;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

        private readonly ImmutableDictionary<string, string> _projectLangaugeToErrorCodePrefixMap =
            ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, new[]
            {
                new KeyValuePair<string, string> (LanguageNames.CSharp, "CS"),
                new KeyValuePair<string, string> (LanguageNames.VisualBasic, "BC"),
                new KeyValuePair<string, string> (LanguageNames.FSharp, "FS"),
            });

        [ImportingConstructor]
        public CPSProjectFactory(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource) :
            base(assertIsForeground: false)
        {
            _serviceProvider = serviceProvider;
            _visualStudioWorkspace = visualStudioWorkspace;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        // internal for testing purposes only.
        internal static CPSProject CreateCPSProject(VisualStudioProjectTracker projectTracker, IServiceProvider serviceProvider, IVsHierarchy hierarchy, string projectDisplayName, string projectFilePath, Guid projectGuid, string language, ICommandLineParserService commandLineParserService, string binOutputPath)
        {
            // this only runs under unit test
            return new CPSProject(projectTracker, reportExternalErrorCreatorOpt: null, hierarchy: hierarchy, language: language,
                serviceProvider: serviceProvider, visualStudioWorkspaceOpt: null, hostDiagnosticUpdateSourceOpt: null,
                projectDisplayName: projectDisplayName, projectFilePath: projectFilePath, projectGuid: projectGuid,
                binOutputPath: binOutputPath, commandLineParserServiceOpt: commandLineParserService);
        }

        IWorkspaceProjectContext IWorkspaceProjectContextFactory.CreateProjectContext(
            string languageName,
            string projectDisplayName,
            string projectFilePath,
            Guid projectGuid,
            object hierarchy,
            string binOutputPath)
        {
            AssertIsForeground();

            EnsurePackageLoaded(languageName);

            // NOTE: It is acceptable for hierarchy to be null in Deferred Project Load scenarios.
            var vsHierarchy = hierarchy as IVsHierarchy;

            IVsReportExternalErrors getExternalErrorReporter(ProjectId id) => GetExternalErrorReporter(id, languageName);
            return new CPSProject(_visualStudioWorkspace.GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider), getExternalErrorReporter, projectDisplayName, projectFilePath,
                vsHierarchy, languageName, projectGuid, binOutputPath, _serviceProvider, _visualStudioWorkspace, _hostDiagnosticUpdateSource,
                commandLineParserServiceOpt: _visualStudioWorkspace.Services.GetLanguageServices(languageName)?.GetService<ICommandLineParserService>());
        }

        // TODO: this is a workaround. Factory has to be refactored so that all callers supply their own error reporters
        IWorkspaceProjectContext IWorkspaceProjectContextFactory.CreateProjectContext(
            string languageName,
            string projectDisplayName,
            string projectFilePath,
            Guid projectGuid,
            object hierarchy,
            string binOutputPath,
            ProjectExternalErrorReporter errorReporter)
        {
            AssertIsForeground();

            EnsurePackageLoaded(languageName);

            // NOTE: It is acceptable for hierarchy to be null in Deferred Project Load scenarios.
            var vsHierarchy = hierarchy as IVsHierarchy;

            IVsReportExternalErrors getExternalErrorReporter(ProjectId id) => errorReporter;
            return new CPSProject(_visualStudioWorkspace.GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider), getExternalErrorReporter, projectDisplayName, projectFilePath,
                vsHierarchy, languageName, projectGuid, binOutputPath, _serviceProvider, _visualStudioWorkspace, _hostDiagnosticUpdateSource,
                commandLineParserServiceOpt: _visualStudioWorkspace.Services.GetLanguageServices(languageName)?.GetService<ICommandLineParserService>());
        }

        private IVsReportExternalErrors GetExternalErrorReporter(ProjectId projectId, string languageName)
        {
            if (!_projectLangaugeToErrorCodePrefixMap.TryGetValue(languageName, out var errorCodePrefix))
            {
                throw new NotSupportedException(nameof(languageName));
            }

            return new ProjectExternalErrorReporter(projectId, errorCodePrefix, _serviceProvider);
        }

        private void EnsurePackageLoaded(string language)
        {
            // we need to make sure we load required packages which initialize VS related services 
            // such as OB, FAR, Error list, Msic workspace, solution crawler before actually
            // set roslyn CPS up.
            var shell = (IVsShell)_serviceProvider.GetService(typeof(SVsShell));
            if (shell == null)
            {
                // no shell. can happen in unit test
                return;
            }

            IVsPackage unused;
            switch (language)
            {
                case LanguageNames.CSharp:
                    shell.LoadPackage(Guids.CSharpPackageId, out unused);
                    break;
                case LanguageNames.VisualBasic:
                    shell.LoadPackage(Guids.VisualBasicPackageId, out unused);
                    break;
                default:
                    // by default, load roslyn package for things like typescript and etc
                    shell.LoadPackage(Guids.RoslynPackageId, out unused);
                    break;
            }
        }
    }
}
