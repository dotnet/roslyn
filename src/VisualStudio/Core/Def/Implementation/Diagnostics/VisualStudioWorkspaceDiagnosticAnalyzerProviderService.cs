// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    /// <summary>
    /// This service provides diagnostic analyzers from the analyzer assets specified in the manifest files of installed VSIX extensions.
    /// These analyzers are used across this workspace session.
    /// </summary>
    [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
    internal partial class VisualStudioWorkspaceDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
    {
        public const string MicrosoftCodeAnalysisCSharp = "Microsoft.CodeAnalysis.CSharp.dll";
        public const string MicrosoftCodeAnalysisVisualBasic = "Microsoft.CodeAnalysis.VisualBasic.dll";

        private const string AnalyzerContentTypeName = "Microsoft.VisualStudio.Analyzer";

        private readonly Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>> _hostDiagnosticAnalyzerInfo;

        /// <summary>
        /// Loader for VSIX-based analyzers.
        /// </summary>
        private static readonly AnalyzerAssemblyLoader s_analyzerAssemblyLoader = new AnalyzerAssemblyLoader();

        [ImportingConstructor]
        public VisualStudioWorkspaceDiagnosticAnalyzerProviderService(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            var dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(EnvDTE.DTE));

            // Microsoft.VisualStudio.ExtensionManager is non-versioned, so we need to dynamically load it, depending on the version of VS we are running on
            // this will allow us to build once and deploy on different versions of VS SxS.
            var vsDteVersion = Version.Parse(dte.Version.Split(' ')[0]); // DTE.Version is in the format of D[D[.D[D]]][ (?+)], so we need to split out the version part and check for uninitialized Major/Minor below
            var assembly = Assembly.Load($"Microsoft.VisualStudio.ExtensionManager, Version={(vsDteVersion.Major == -1 ? 0 : vsDteVersion.Major)}.{(vsDteVersion.Minor == -1 ? 0 : vsDteVersion.Minor)}.0.0, PublicKeyToken=b03f5f7f11d50a3a");
            if (assembly == null)
            {
                // extension manager can't be null. if it is null, then VS is seriously broken.
                // fail fast right away
                FailFast.OnFatalException(new Exception("extension manager can't be null"));
            }

            // Get the analyzer assets for installed VSIX extensions through the VSIX extension manager.
            var extensionManager = serviceProvider.GetService(assembly.GetType("Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager"));
            Assumes.Present(extensionManager);

            _hostDiagnosticAnalyzerInfo = new Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>>(
                () => GetHostAnalyzerPackagesWithName(extensionManager, assembly.GetType("Microsoft.VisualStudio.ExtensionManager.IExtensionContent")), isThreadSafe: true);
        }

        public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            return _hostDiagnosticAnalyzerInfo.Value;
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
            try
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
                        if (!(extensionType_GetContentMethod?.Invoke(extension, new object[] { content }) is string assembly))
                        {
                            continue;
                        }

                        assemblies.Add(assembly);
                    }

                    builder.Add(new HostDiagnosticAnalyzerPackage(name, assemblies.ToImmutable()));
                }

                var packages = builder.ToImmutable();

                EnsureMandatoryAnalyzers(packages);

                // make sure enabled extensions are alive in memory
                // so that we can debug it through if mandatory analyzers are missing
                GC.KeepAlive(enabledExtensions);

                return packages;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // this can be called from any thread, and extension manager could be disposed in the middle of us using it since
                // now all these are free-threaded and there is no central coordinator, or API or state is immutable that prevent states from
                // changing in the middle of others using it.
                //
                // fortunately, this only happens on disposing at shutdown, so we just catch the exception and silently swallow it. 
                // we are about to shutdown anyway.
                return ImmutableArray<HostDiagnosticAnalyzerPackage>.Empty;
            }
        }

        private static void EnsureMandatoryAnalyzers(ImmutableArray<HostDiagnosticAnalyzerPackage> packages)
        {
            foreach (var package in packages)
            {
                if (package.Assemblies.Any(a => a?.EndsWith(MicrosoftCodeAnalysisCSharp, StringComparison.OrdinalIgnoreCase) == true) &&
                    package.Assemblies.Any(a => a?.EndsWith(MicrosoftCodeAnalysisVisualBasic, StringComparison.OrdinalIgnoreCase) == true))
                {
                    return;
                }
            }

            FailFast.OnFatalException(new Exception("Mandatory analyzers are missing"));
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
