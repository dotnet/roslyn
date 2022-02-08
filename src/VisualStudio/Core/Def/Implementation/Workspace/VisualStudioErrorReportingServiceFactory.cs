// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IErrorReportingService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioErrorReportingServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly SVsServiceProvider _serviceProvider;

        private IErrorReportingService? _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioErrorReportingServiceFactory(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _listenerProvider = listenerProvider;
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_singleton == null)
            {
                _singleton = new VisualStudioErrorReportingService(
                    _threadingContext,
                    _listenerProvider,
                    workspaceServices.GetRequiredService<IInfoBarService>(),
                    _serviceProvider);
            }

            return _singleton;
        }
    }
}
