// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    /// <summary>
    /// This service provides diagnostic analyzers from the analyzer assets specified in the manifest files of installed VSIX extensions.
    /// These analyzers are used across this workspace session.
    /// </summary>
    [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
    internal partial class VisualStudioWorkspaceDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
    {
        private const string AnalyzerContentTypeName = "Microsoft.VisualStudio.Analyzer";

        private readonly ImmutableArray<HostDiagnosticAnalyzerPackage> _hostDiagnosticAnalyzerInfo;

        /// <summary>
        /// Loader for VSIX-based analyzers.
        /// </summary>
        private static readonly AnalyzerAssemblyLoader s_analyzerAssemblyLoader = new AnalyzerAssemblyLoader();

        [ImportingConstructor]
        public VisualStudioWorkspaceDiagnosticAnalyzerProviderService(VisualStudioWorkspaceImpl workspace)
        {
            // Get the analyzer assets for installed VSIX extensions through the VSIX extension manager.
            var extensionManager = workspace.GetVsService<SVsExtensionManager, IVsExtensionManager>();

            _hostDiagnosticAnalyzerInfo = GetHostAnalyzerPackagesWithName(extensionManager);
        }

        public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            return _hostDiagnosticAnalyzerInfo;
        }

        public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader()
        {
            return s_analyzerAssemblyLoader;
        }

        // internal for testing purposes.
        internal static IAnalyzerAssemblyLoader GetLoader()
        {
            return s_analyzerAssemblyLoader;
        }

        // internal for testing purpose
        internal static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostAnalyzerPackagesWithName(IVsExtensionManager extensionManager)
        {
            var builder = ImmutableArray.CreateBuilder<HostDiagnosticAnalyzerPackage>();
            foreach (var extension in extensionManager.GetEnabledExtensions(AnalyzerContentTypeName))
            {
                var name = extension.Header.LocalizedName;
                var assemblies = extension.Content.Where(ShouldInclude)
                                                  .Select(c => extension.GetContentLocation(c))
                                                  .WhereNotNull();

                builder.Add(new HostDiagnosticAnalyzerPackage(name, assemblies.ToImmutableArray()));
            }

            return builder.ToImmutable();
        }

        // internal for testing purpose
        internal static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostAnalyzerPackages(IVsExtensionManager extensionManager)
        {
            var references = ImmutableArray.CreateBuilder<string>();
            foreach (var reference in extensionManager.GetEnabledExtensionContentLocations(AnalyzerContentTypeName))
            {
                if (string.IsNullOrEmpty(reference))
                {
                    continue;
                }

                references.Add(reference);
            }

            return ImmutableArray.Create(new HostDiagnosticAnalyzerPackage(name: null, assemblies: references.ToImmutable()));
        }

        private static bool ShouldInclude(IExtensionContent content)
        {
            return string.Equals(content.ContentTypeName, AnalyzerContentTypeName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
