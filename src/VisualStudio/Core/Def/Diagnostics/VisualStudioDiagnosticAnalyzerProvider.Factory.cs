// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using System.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;

internal partial class VisualStudioDiagnosticAnalyzerProvider
{
    [Export(typeof(IVisualStudioDiagnosticAnalyzerProviderFactory)), Shared]
    internal sealed class Factory : IVisualStudioDiagnosticAnalyzerProviderFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;

        private VisualStudioDiagnosticAnalyzerProvider? _lazyProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Factory(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
        }

        public async Task<VisualStudioDiagnosticAnalyzerProvider> GetOrCreateProviderAsync(CancellationToken cancellationToken)
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

            return _lazyProvider = new VisualStudioDiagnosticAnalyzerProvider(extensionManager, typeIExtensionContent);
        }
    }
}
