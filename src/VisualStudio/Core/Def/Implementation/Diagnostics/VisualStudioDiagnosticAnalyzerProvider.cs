// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.ExtensionManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    /// <summary>
    /// This service provides diagnostic analyzers from the analyzer assets specified in the manifest files of installed VSIX extensions.
    /// These analyzers are used across this workspace session.
    /// </summary>
    internal partial class VisualStudioDiagnosticAnalyzerProvider
    {
        private const string AnalyzerContentTypeName = "Microsoft.VisualStudio.Analyzer";

        /// <summary>
        /// Loader for VSIX-based analyzers.
        /// </summary>
        public static readonly IAnalyzerAssemblyLoader AnalyzerAssemblyLoader = new Loader();

        private readonly IVsExtensionManager _extensionManager;

        internal VisualStudioDiagnosticAnalyzerProvider(IVsExtensionManager extensionManager)
        {
            Contract.ThrowIfNull(extensionManager);

            _extensionManager = extensionManager;
        }

        // internal for testing
        internal ImmutableArray<AnalyzerReference> GetAnalyzerReferencesInExtensions()
        {
            try
            {
                // dynamic is weird. it can't see internal type with public interface even if callee is
                // implementation of the public interface in internal type. so we can't use dynamic here
                var _ = PooledHashSet<string>.GetInstance(out var analyzePaths);

                var enabledExtensions = _extensionManager.GetEnabledExtensions(AnalyzerContentTypeName);

                foreach (var extension in enabledExtensions)
                {
                    var name = extension.Header.LocalizedName;
                    foreach (var content in extension.Content)
                    {
                        if (!ShouldInclude(content))
                        {
                            continue;
                        }

                        var assemblyPath = extension.GetContentLocation(content);
                        if (string.IsNullOrEmpty(assemblyPath))
                        {
                            continue;
                        }

                        analyzePaths.Add(assemblyPath);
                    }
                }

                // make sure enabled extensions are alive in memory
                // so that we can debug it through if mandatory analyzers are missing
                GC.KeepAlive(enabledExtensions);

                return analyzePaths.SelectAsArray(path => (AnalyzerReference)new AnalyzerFileReference(path, AnalyzerAssemblyLoader));
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                // this can be called from any thread, and extension manager could be disposed in the middle of us using it since
                // now all these are free-threaded and there is no central coordinator, or API or state is immutable that prevent states from
                // changing in the middle of others using it.
                //
                // fortunately, this only happens on disposing at shutdown, so we just catch the exception and silently swallow it. 
                // we are about to shutdown anyway.
                return ImmutableArray<AnalyzerReference>.Empty;
            }
        }

        private static bool ShouldInclude(IExtensionContent content)
        {
            return string.Equals(content.ContentTypeName, AnalyzerContentTypeName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
