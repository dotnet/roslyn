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

            _hostDiagnosticAnalyzerInfo = GetHostAnalyzerPackagesWithName(extensionManager, assembly.GetType("Microsoft.VisualStudio.ExtensionManager.IExtensionContent"));
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
        internal static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostAnalyzerPackagesWithName(object extensionManager, Type parameterType)
        {
            // dynamic is wierd. it can't see internal type with public interface even if callee is
            // implementation of the public interface in internal type. so we can't use dynamic here

            var builder = ImmutableArray.CreateBuilder<HostDiagnosticAnalyzerPackage>();

            // var enabledExtensions = extensionManager.GetEnabledExtensions(AnalyzerContentTypeName);
            var extensionManagerType = extensionManager.GetType();
            var extensionManager_GetEnabledExtensionsMethod = extensionManagerType.GetRuntimeMethod("GetEnabledExtensions", new Type[] { typeof(string) });
            var enabledExtensions = extensionManager_GetEnabledExtensionsMethod.Invoke(extensionManager, new object[] { AnalyzerContentTypeName }) as IEnumerable<object>;

            foreach (var extension in enabledExtensions)
            {
                // var name = extension.Header.LocalizedName;
                var extensionType = extension.GetType();
                var extensionType_HeaderProperty = extensionType.GetRuntimeProperty("Header");
                var extension_Header = extensionType_HeaderProperty.GetValue(extension);
                var extension_HeaderType = extension_Header.GetType();
                var extension_HeaderType_LocalizedNameProperty = extension_HeaderType.GetRuntimeProperty("LocalizedName");
                var name = extension_HeaderType_LocalizedNameProperty.GetValue(extension_Header) as string;

                var assemblies = ImmutableArray.CreateBuilder<string>();

                // var extension_Content = extension.Content;
                var extensionType_ContentProperty = extensionType.GetRuntimeProperty("Content");
                var extension_Content = extensionType_ContentProperty.GetValue(extension) as IEnumerable<object>;

                foreach (var content in extension_Content)
                {
                    if (!ShouldInclude(content))
                    {
                        continue;
                    }

                    var extensionType_GetContentMethod = extensionType.GetRuntimeMethod("GetContentLocation", new Type[] { parameterType });
                    var assembly = extensionType_GetContentMethod?.Invoke(extension, new object[] { content }) as string;
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

        private static bool ShouldInclude(object content)
        {
            // var content_ContentTypeName = content.ContentTypeName;
            var contentType = content.GetType();
            var contentType_ContentTypeNameProperty = contentType.GetRuntimeProperty("ContentTypeName");
            var content_ContentTypeName = contentType_ContentTypeNameProperty.GetValue(content) as string;

            return string.Equals(content_ContentTypeName, AnalyzerContentTypeName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
