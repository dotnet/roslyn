// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(IProjectContextFactory))]
    internal partial class CPSProjectFactory : IProjectContextFactory
    {
        private readonly VisualStudioProjectTracker _projectTracker;
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
            VisualStudioProjectTracker projectTracker,
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _projectTracker = projectTracker;
            _serviceProvider = serviceProvider;
            _visualStudioWorkspace = visualStudioWorkspace;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _externalErrorReporterMap = new Dictionary<string, IVsReportExternalErrors>(StringComparer.OrdinalIgnoreCase);
        }

        // internal for testing purposes only.
        internal static CPSProject CreateCPSProject(VisualStudioProjectTracker projectTracker, IServiceProvider serviceProvider, IVsHierarchy hierarchy, string projectDisplayName, string projectFilePath, string language, Guid projectGuid, string projectTypeGuid, CommandLineArguments commandLineArguments)
        {
            return new CPSProject(commandLineArguments, projectTracker, reportExternalErrorCreatorOpt: null, hierarchy: hierarchy, language: language,
                serviceProvider: serviceProvider, visualStudioWorkspaceOpt: null, hostDiagnosticUpdateSourceOpt: null, projectDisplayName: projectDisplayName,
                projectFilePath: projectFilePath, projectGuid: projectGuid, projectTypeGuid: projectTypeGuid);
        }

        IProjectContext IProjectContextFactory.CreateProjectContext(
            string languageName,
            string projectDisplayName,
            string projectFilePath,
            Guid projectGuid,
            string projectTypeGuid,
            IVsHierarchy hierarchy,
            CommandLineArguments commandLineArguments)
        {
            Contract.ThrowIfNull(hierarchy);

            Func<ProjectId, IVsReportExternalErrors> getExternalErrorReporter = id => GetExternalErrorReporter(id, languageName);
            return new CPSProject(commandLineArguments, _projectTracker, getExternalErrorReporter, projectDisplayName, projectFilePath,
                projectGuid, projectTypeGuid, hierarchy, languageName, _serviceProvider, _visualStudioWorkspace, _hostDiagnosticUpdateSource);
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
                        Debug.Fail($"Unknown language '{languageName}'");
                        _projectLangaugeToErrorCodePrefixMap.Add(languageName, languageName);
                    }

                    errorReporter = new ProjectExternalErrorReporter(projectId, errorCodePrefix, _serviceProvider);
                    _externalErrorReporterMap.Add(languageName, errorReporter);
                }

                return errorReporter;
            }
        }
    }
}
