// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(IProjectShimFactory))]
    internal partial class ProjectShimFactory : IProjectShimFactory
    {
        private readonly VisualStudioProjectTracker _projectTracker;
        private readonly SVsServiceProvider _serviceProvider;
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
        public ProjectShimFactory(
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

        IProjectShim IProjectShimFactory.CreateProjectShim(string languageName, string projectName)
        {
            var vsSolution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));
            IVsHierarchy vsHierarchy;
            ErrorHandler.ThrowOnFailure(vsSolution.GetProjectOfUniqueName(projectName, out vsHierarchy));
            if (vsHierarchy != null)
            {
                Func<ProjectId, IVsReportExternalErrors> getExternalErrorReporter = id => GetExternalErrorReporter(id, languageName);
                return new ProjectShim(_projectTracker, getExternalErrorReporter, projectName,
                    vsHierarchy, languageName, _serviceProvider, _visualStudioWorkspace, _hostDiagnosticUpdateSource);
            }

            return null;
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
