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

        private readonly object _extensionManager;
        private readonly Type _typeIExtensionContent;

        // internal for testing
        internal VisualStudioDiagnosticAnalyzerProvider(object extensionManager, Type typeIExtensionContent)
        {
            Contract.ThrowIfNull(extensionManager);
            Contract.ThrowIfNull(typeIExtensionContent);

            _extensionManager = extensionManager;
            _typeIExtensionContent = typeIExtensionContent;
        }

        // internal for testing
        internal ImmutableArray<AnalyzerReference> GetAnalyzerReferencesInExtensions()
        {
            try
            {
                // dynamic is weird. it can't see internal type with public interface even if callee is
                // implementation of the public interface in internal type. so we can't use dynamic here
                var _ = PooledHashSet<string>.GetInstance(out var analyzePaths);

                // var enabledExtensions = extensionManager.GetEnabledExtensions(AnalyzerContentTypeName);
                var extensionManagerType = _extensionManager.GetType();
                var extensionManager_GetEnabledExtensionsMethod = extensionManagerType.GetRuntimeMethod("GetEnabledExtensions", new Type[] { typeof(string) });
                var enabledExtensions = (IEnumerable<object>)extensionManager_GetEnabledExtensionsMethod.Invoke(_extensionManager, new object[] { AnalyzerContentTypeName });

                foreach (var extension in enabledExtensions)
                {
                    // var name = extension.Header.LocalizedName;
                    var extensionType = extension.GetType();
                    var extensionType_HeaderProperty = extensionType.GetRuntimeProperty("Header");
                    var extension_Header = extensionType_HeaderProperty.GetValue(extension);
                    var extension_HeaderType = extension_Header.GetType();
                    var extension_HeaderType_LocalizedNameProperty = extension_HeaderType.GetRuntimeProperty("LocalizedName");
                    var name = extension_HeaderType_LocalizedNameProperty.GetValue(extension_Header) as string;

                    // var extension_Content = extension.Content;
                    var extensionType_ContentProperty = extensionType.GetRuntimeProperty("Content");
                    var extension_Content = (IEnumerable<object>)extensionType_ContentProperty.GetValue(extension);

                    foreach (var content in extension_Content)
                    {
                        if (!ShouldInclude(content))
                        {
                            continue;
                        }

                        var extensionType_GetContentMethod = extensionType.GetRuntimeMethod("GetContentLocation", new Type[] { _typeIExtensionContent });
                        if (extensionType_GetContentMethod?.Invoke(extension, new object[] { content }) is not string assemblyPath ||
                            string.IsNullOrEmpty(assemblyPath))
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
