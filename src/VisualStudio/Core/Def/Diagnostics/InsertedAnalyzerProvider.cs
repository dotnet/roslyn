// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;

/// <summary>
/// Discovers analyzers inserted into Visual Studio which are candidates for redirection.
/// Analyzers loaded from SDK are redirected to these VS-inserted analyzers
/// by <see cref="RedirectingAnalyzerAssemblyResolver"/>.
/// </summary>
internal sealed class InsertedAnalyzerProvider
{
    [Export(typeof(IInsertedAnalyzerProviderFactory)), Shared]
    internal sealed class Factory : IInsertedAnalyzerProviderFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;

        private InsertedAnalyzerProvider? _lazyProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Factory(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
        }

        public async Task<InsertedAnalyzerProvider> GetOrCreateProviderAsync(CancellationToken cancellationToken)
        {
            // the following code requires UI thread:
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (_lazyProvider != null)
            {
                return _lazyProvider;
            }

            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));

            // Microsoft.VisualStudio.ExtensionManager is non-versioned, so we need to dynamically load it, depending on the version of VS we are running on
            // this will allow us to build once and deploy on different versions of VS SxS.
            var vsDteVersion = Version.Parse(dte.Version.Split(' ')[0]); // DTE.Version is in the format of D[D[.D[D]]][ (?+)], so we need to split out the version part and check for uninitialized Major/Minor below

            var assembly = Assembly.Load($"Microsoft.VisualStudio.ExtensionManager, Version={(vsDteVersion.Major == -1 ? 0 : vsDteVersion.Major)}.{(vsDteVersion.Minor == -1 ? 0 : vsDteVersion.Minor)}.0.0, PublicKeyToken=b03f5f7f11d50a3a");
            var typeIExtensionContent = assembly.GetType("Microsoft.VisualStudio.ExtensionManager.IExtensionContent");
            var type = assembly.GetType("Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager");
            var extensionManager = _serviceProvider.GetService(type);

            return _lazyProvider = new InsertedAnalyzerProvider(extensionManager, typeIExtensionContent);
        }
    }

    public const string InsertedAnalyzerMappingContentTypeName = "Microsoft.VisualStudio.InsertedAnalyzer";

    private readonly object _extensionManager;
    private readonly Type _typeIExtensionContent;
    private readonly Lazy<ImmutableArray<string>> _lazyInsertedAnalyzerMappingFilePaths;

    // internal for testing
    internal InsertedAnalyzerProvider(object extensionManager, Type typeIExtensionContent)
    {
        Contract.ThrowIfNull(extensionManager);
        Contract.ThrowIfNull(typeIExtensionContent);

        _extensionManager = extensionManager;
        _typeIExtensionContent = typeIExtensionContent;
        _lazyInsertedAnalyzerMappingFilePaths = new(GetInsertedAnalyzerMappingFilePathsImpl);
    }

    public ImmutableArray<string> GetInsertedAnalyzerMappingFilePaths()
        => _lazyInsertedAnalyzerMappingFilePaths.Value;

    private ImmutableArray<string> GetInsertedAnalyzerMappingFilePathsImpl()
    {
        try
        {
            var _ = ArrayBuilder<string>.GetInstance(out var mappingFilePaths);

            // dynamic is weird. it can't see internal type with public interface even if callee is
            // implementation of the public interface in internal type. so we can't use dynamic here

            // IEnumerable<IInstalledExtension> enabledExtensions = extensionManager.GetEnabledExtensions(InsertedAnalyzerContentTypeName);
            var extensionManagerType = _extensionManager.GetType();
            var extensionManager_GetEnabledExtensionsMethod = extensionManagerType.GetRuntimeMethod("GetEnabledExtensions", new Type[] { typeof(string) });
            var enabledExtensions = (IEnumerable<object>)extensionManager_GetEnabledExtensionsMethod.Invoke(_extensionManager, new object[] { InsertedAnalyzerMappingContentTypeName });

            foreach (var extension in enabledExtensions)
            {
                // IEnumerable<IExtensionContent> extension_Content = extension.Content;
                var extensionType = extension.GetType();
                var extensionType_ContentProperty = extensionType.GetRuntimeProperty("Content");
                var extension_Content = (IEnumerable<object>)extensionType_ContentProperty.GetValue(extension);

                foreach (var content in extension_Content)
                {
                    if (!ShouldInclude(content))
                    {
                        continue;
                    }

                    // string mappingFilePath = extension.GetContentLocation(content);
                    var extensionType_GetContentMethod = extensionType.GetRuntimeMethod("GetContentLocation", new Type[] { _typeIExtensionContent });
                    if (extensionType_GetContentMethod?.Invoke(extension, new object[] { content }) is not string mappingFilePath ||
                        string.IsNullOrEmpty(mappingFilePath))
                    {
                        continue;
                    }

                    mappingFilePaths.Add(mappingFilePath);
                }
            }

            // make sure enabled extensions are alive in memory
            // so that we can debug it through if mandatory analyzers are missing
            GC.KeepAlive(enabledExtensions);

            // Sort for deterministic result.
            mappingFilePaths.Sort();

            return mappingFilePaths.ToImmutableAndFree();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            // this can be called from any thread, and extension manager could be disposed in the middle of us using it since
            // now all these are free-threaded and there is no central coordinator, or API or state is immutable that prevent states from
            // changing in the middle of others using it.
            //
            // fortunately, this only happens on disposing at shutdown, so we just catch the exception and silently swallow it. 
            // we are about to shutdown anyway.
            return [];
        }
    }

    private static bool ShouldInclude(object content)
    {
        // string content_ContentTypeName = content.ContentTypeName;
        var contentType = content.GetType();
        var contentType_ContentTypeNameProperty = contentType.GetRuntimeProperty("ContentTypeName");
        var content_ContentTypeName = contentType_ContentTypeNameProperty.GetValue(content) as string;

        return content_ContentTypeName == InsertedAnalyzerMappingContentTypeName;
    }
}

internal interface IInsertedAnalyzerProviderFactory
{
    Task<InsertedAnalyzerProvider> GetOrCreateProviderAsync(CancellationToken cancellationToken);
}
