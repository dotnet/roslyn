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

            // get rootfolder and shellfolder location
            string rootFolder;
            string shellFolder;
            if (TryGetRootAndShellFolder(extensionManager, out shellFolder, out rootFolder))
            {
                _hostDiagnosticAnalyzerInfo = GetHostAnalyzerPackagesWithName(extensionManager, rootFolder, shellFolder);
                return;
            }

            // if we can't get rootFolder/shellFolder location, use old behavior.
            _hostDiagnosticAnalyzerInfo = GetHostAnalyzerPackages(extensionManager);
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
        // we have to use reflection in here because Microsoft.VisualStudio.ExtensionManager is non-versioned, and reflection allows us to build once and deploy to multiple different versions of VS SxS.
        internal static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostAnalyzerPackagesWithName(object extensionManager, string rootFolder, string shellFolder)
        {
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

                var assemblies = new List<string>();

                // var extension_Content = extension.Content;
                var extensionType_ContentProperty = extensionType.GetRuntimeProperty("Content");
                var extension_Content = extensionType_ContentProperty.GetValue(extension) as IEnumerable<object>;

                foreach (var content in extension_Content)
                {
                    if (ShouldInclude(content))
                    {
                        // var extension_InstallPath = extension.InstallPath;
                        var extensionType_InstallPathProperty = extensionType.GetRuntimeProperty("InstallPath");
                        var extension_InstallPath = extensionType_InstallPathProperty.GetValue(extension) as string;

                        // var content_RelativePath = content.RelativePath;
                        var contentType = content.GetType();
                        var contentType_RelativePathProperty = contentType.GetRuntimeProperty("RelativePath");
                        var content_RelativePath = contentType_RelativePathProperty.GetValue(content) as string;

                        var assembly = GetContentLocation(shellFolder, rootFolder, extension_InstallPath, content_RelativePath);

                        if (assembly != null)
                        {
                            assemblies.Add(assembly);
                        }
                    }
                }

                builder.Add(new HostDiagnosticAnalyzerPackage(name, assemblies.ToImmutableArray()));
            }

            return builder.ToImmutable();
        }

        // internal for testing purpose
        // we have to use reflection in here because Microsoft.VisualStudio.ExtensionManager is non-versioned, and reflection allows us to build once and deploy to multiple different versions of VS SxS.
        internal static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostAnalyzerPackages(object extensionManager)
        {
            var references = ImmutableArray.CreateBuilder<string>();

            // var enabledExtensionContentLocations = extension.GetEnabledExtensionContentLocations(AnalyzerContentTypeName);
            var extensionManagerType = extensionManager.GetType();
            var extensionManager_GetEnabledExtensionContentLocationsMethod = extensionManagerType.GetRuntimeMethod("GetEnabledExtensionContentLocations", new Type[] { typeof(string) });
            var enabledExtensionContentLocations = extensionManager_GetEnabledExtensionContentLocationsMethod.Invoke(extensionManager, new object[] { AnalyzerContentTypeName }) as IEnumerable<string>;

            foreach (var reference in enabledExtensionContentLocations)
            {
                if (string.IsNullOrEmpty(reference))
                {
                    continue;
                }

                references.Add(reference);
            }

            return ImmutableArray.Create(new HostDiagnosticAnalyzerPackage(name: null, assemblies: references.ToImmutable()));
        }

        private static bool TryGetRootAndShellFolder(object extensionManager, out string shellFolder, out string rootFolder)
        {
            // use reflection to get this information. currently there is no other way to get this information
            shellFolder = GetProperty(extensionManager, "ShellFolder");
            rootFolder = GetProperty(extensionManager, "RootFolder");

            return !string.IsNullOrEmpty(rootFolder) && !string.IsNullOrEmpty(shellFolder);
        }

        private static string GetContentLocation(string shellFolder, string rootFolder, string installPath, string relativePath)
        {
            // extension manager should expose an API that doesn't require this.
            const string ShellFolderToken = "$ShellFolder$";
            const string RootFolderToken = "$RootFolder$";

            if (relativePath.StartsWith(ShellFolderToken))
            {
                return relativePath.Replace(ShellFolderToken, shellFolder);
            }
            else if (relativePath.StartsWith(RootFolderToken))
            {
                return relativePath.Replace(RootFolderToken, rootFolder);
            }

            string contentLocation = null;
            try
            {
                contentLocation = Path.Combine(installPath, relativePath);
            }
            //Path.Combine will throw an ArgumentException if either of the two path arguments contain illegal characters.
            //We'll just catch this exception here and ignore the paths with illegal characters.
            catch (ArgumentException)
            {
            }

            return contentLocation;
        }

        private static string GetProperty(object extensionManager, string propertyName)
        {
            try
            {
                return (string)extensionManager.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(extensionManager);
            }
            catch
            {
                return null;
            }
        }

        // we have to use reflection in here because Microsoft.VisualStudio.ExtensionManager is non-versioned, and reflection allows us to build once and deploy to multiple different versions of VS SxS.
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
