// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    /// <summary>
    /// This service provides diagnostic analyzers from the analyzer assets specified in the manifest files of installed VSIX extensions.
    /// These analyzers are used across this workspace session.
    /// </summary>
    [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
    internal class VisualStudioWorkspaceDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
    {
        private const string AnalyzerContentTypeName = "Microsoft.VisualStudio.Analyzer";

        private readonly ImmutableArray<HostDiagnosticAnalyzerPackage> _hostDiagnosticAnalyzerInfo;

        [ImportingConstructor]
        public VisualStudioWorkspaceDiagnosticAnalyzerProviderService(VisualStudioWorkspaceImpl workspace)
        {
            // Get the analyzer assets for installed VSIX extensions through the VSIX extension manager.
            var extensionManager = workspace.GetVsService<SVsExtensionManager, IVsExtensionManager>();

            var builder = ImmutableArray.CreateBuilder<HostDiagnosticAnalyzerPackage>();
            foreach (var extension in extensionManager.GetEnabledExtensions(AnalyzerContentTypeName))
            {
                var name = extension.Header.LocalizedName;
                var assemblies = extension.Content.Where(ShouldInclude).Select(c => Path.Combine(extension.InstallPath, c.RelativePath));

                builder.Add(new HostDiagnosticAnalyzerPackage(name, assemblies.ToImmutableArray()));
            }

            _hostDiagnosticAnalyzerInfo = builder.ToImmutable();
        }

        public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            return _hostDiagnosticAnalyzerInfo;
        }

        private bool ShouldInclude(IExtensionContent content)
        {
            return string.Equals(content.ContentTypeName, AnalyzerContentTypeName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
