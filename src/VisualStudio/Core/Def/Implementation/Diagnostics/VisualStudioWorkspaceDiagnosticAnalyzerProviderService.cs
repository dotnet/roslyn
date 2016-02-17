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
            // Microsoft.VisualStudio.ExtensionManager is non-versioned, so we need to dynamically load it, depending on the version of VS we are running on
            // this will allow us to build once and deploy on different versions of VS SxS.
            var vsDteVersion = Version.Parse(workspace.GetVsDte().Version.Split(' ')[0]); // DTE.Version is in the format of D[D[.D[D]]][ (?+)], so we need to split out the version part and check for uninitialized Major/Minor below
            var assembly = Assembly.Load($"Microsoft.VisualStudio.ExtensionManager, Version={(vsDteVersion.Major == -1 ? 0 : vsDteVersion.Major)}.{(vsDteVersion.Minor == -1 ? 0 : vsDteVersion.Minor)}.0.0, PublicKeyToken=b03f5f7f11d50a3a");

            // Get the analyzer assets for installed VSIX extensions through the VSIX extension manager.
            var extensionManager = workspace.GetVsService(assembly.GetType("Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager"));

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
        internal static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostAnalyzerPackagesWithName(dynamic extensionManager)
        {
            var builder = ImmutableArray.CreateBuilder<HostDiagnosticAnalyzerPackage>();
            foreach (var extension in extensionManager.GetEnabledExtensions(AnalyzerContentTypeName))
            {
                var name = extension.Header.LocalizedName;

                var assemblies = ImmutableArray.CreateBuilder<string>();
                foreach (var content in extension.Content)
                {
                    if (!ShouldInclude(content))
                    {
                        continue;
                    }

                    var assembly = (string)extension.GetContentLocation(content);
                    if (assembly == null)
                    {
                        continue;
                    }

                    assemblies.Add(assembly);
                }

                builder.Add(new HostDiagnosticAnalyzerPackage(name, assemblies.ToImmutable()));
            }

            return builder.ToImmutable();
        }

        // internal for testing purpose
        internal static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostAnalyzerPackages(dynamic extensionManager)
        {
            var references = ImmutableArray.CreateBuilder<string>();
            foreach (var reference in extensionManager.GetEnabledExtensionContentLocations(AnalyzerContentTypeName))
            {
                if (string.IsNullOrEmpty((string)reference))
                {
                    continue;
                }

                references.Add((string)reference);
            }

            return ImmutableArray.Create(new HostDiagnosticAnalyzerPackage(name: null, assemblies: references.ToImmutable()));
        }

        private static bool ShouldInclude(dynamic content)
        {
            return string.Equals((string)content.ContentTypeName, AnalyzerContentTypeName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
