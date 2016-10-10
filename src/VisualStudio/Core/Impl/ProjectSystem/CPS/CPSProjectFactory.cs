// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(IWorkspaceProjectContextFactory))]
    internal partial class CPSProjectFactory : IWorkspaceProjectContextFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspace;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

        private readonly Dictionary<string, IVsReportExternalErrors> _externalErrorReporterMap;
        private readonly ImmutableDictionary<string, string> _projectLangaugeToErrorCodePrefixMap =
            ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, new[]
            {
                new KeyValuePair<string, string> (LanguageNames.CSharp, "CS"),
                new KeyValuePair<string, string> (LanguageNames.VisualBasic, "BC"),
            });

        [ImportingConstructor]
        public CPSProjectFactory(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _serviceProvider = serviceProvider;
            _visualStudioWorkspace = visualStudioWorkspace;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _externalErrorReporterMap = new Dictionary<string, IVsReportExternalErrors>(StringComparer.OrdinalIgnoreCase);
        }

        // internal for testing purposes only.
        internal static CPSProject CreateCPSProject(VisualStudioProjectTracker projectTracker, IServiceProvider serviceProvider, IVsHierarchy hierarchy, string projectDisplayName, string projectFilePath, Guid projectGuid, string language, ICommandLineParserService commandLineParserService, string binOutputPath)
        {
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
            // NOTE: It is acceptable for hierarchy to be null in Deferred Project Load scenarios.
            var vsHierarchy = hierarchy as IVsHierarchy;

            Func<ProjectId, IVsReportExternalErrors> getExternalErrorReporter = id => GetExternalErrorReporter(id, languageName);
            return new CPSProject(_visualStudioWorkspace.ProjectTracker, getExternalErrorReporter, projectDisplayName, projectFilePath,
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
            // NOTE: It is acceptable for hierarchy to be null in Deferred Project Load scenarios.
            var vsHierarchy = hierarchy as IVsHierarchy;

            Func<ProjectId, IVsReportExternalErrors> getExternalErrorReporter = id => errorReporter;
            return new CPSProject(_visualStudioWorkspace.ProjectTracker, getExternalErrorReporter, projectDisplayName, projectFilePath,
                vsHierarchy, languageName, projectGuid, binOutputPath, _serviceProvider, _visualStudioWorkspace, _hostDiagnosticUpdateSource,
                commandLineParserServiceOpt: _visualStudioWorkspace.Services.GetLanguageServices(languageName)?.GetService<ICommandLineParserService>());
        }

        private IVsReportExternalErrors GetExternalErrorReporter(ProjectId projectId, string languageName)
        {
            lock (_externalErrorReporterMap)
            {
                IVsReportExternalErrors errorReporter;
                if (!_externalErrorReporterMap.TryGetValue(languageName, out errorReporter))
                {
                    string errorCodePrefix;
                    if (!_projectLangaugeToErrorCodePrefixMap.TryGetValue(languageName, out errorCodePrefix))
                    {
                        throw new NotSupportedException(nameof(languageName));
                    }

                    errorReporter = new ProjectExternalErrorReporter(projectId, errorCodePrefix, _serviceProvider);
                    _externalErrorReporterMap.Add(languageName, errorReporter);
                }

                return errorReporter;
            }
        }
    }
}
