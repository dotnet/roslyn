// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class AbstractRoslynProject : AbstractProject, ICompilerOptionsHostObject
    {
        internal VsENCRebuildableProjectImpl EditAndContinueImplOpt;

        public AbstractRoslynProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            string language,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
            : base(projectTracker, reportExternalErrorCreatorOpt, projectSystemName, hierarchy, language, serviceProvider, visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt)
        {
            if (visualStudioWorkspaceOpt != null)
            {
                this.EditAndContinueImplOpt = new VsENCRebuildableProjectImpl(this);
            }
        }

        public override void Disconnect()
        {
            // project is going away
            this.EditAndContinueImplOpt = null;

            base.Disconnect();
        }

        /// <summary>
        /// Returns the parsed command line arguments (parsed by <see cref="ParseCommandLineArguments"/>) that were set by the project
        /// system's call to <see cref="ICompilerOptionsHostObject.SetCompilerOptions(string, out bool)"/>.
        /// </summary>
        protected CommandLineArguments GetParsedCommandLineArguments()
        {
            if (_lastParsedCommandLineArguments == null)
            {
                // We don't have any yet, so let's parse nothing
                _lastParsedCompilerOptions = "";
                _lastParsedCommandLineArguments = ParseCommandLineArguments(SpecializedCollections.EmptyEnumerable<string>());
            }

            return _lastParsedCommandLineArguments;
        }

        private string _lastParsedCompilerOptions;
        private CommandLineArguments _lastParsedCommandLineArguments;

        protected abstract CommandLineArguments ParseCommandLineArguments(IEnumerable<string> arguments);

        int ICompilerOptionsHostObject.SetCompilerOptions(string compilerOptions, out bool supported)
        {
            if (!string.Equals(_lastParsedCompilerOptions, compilerOptions))
            {
                var splitArguments = CommandLineParser.SplitCommandLineIntoArguments(compilerOptions, removeHashComments: false);

                _lastParsedCommandLineArguments = ParseCommandLineArguments(splitArguments);
                _lastParsedCompilerOptions = compilerOptions;

                UpdateOptions();
            }

            supported = true;

            return VSConstants.S_OK;
        }
    }
}
