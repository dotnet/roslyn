// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Services;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportIncrementalAnalyzerProvider(Name, new[] { WorkspaceKind.Host }), Shared]
    internal class DesignerAttributeIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        public const string Name = nameof(DesignerAttributeIncrementalAnalyzerProvider);

        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectItemDesignerTypeUpdateService _projectItemDesignerTypeUpdateService;
        private readonly IForegroundNotificationService _notificationService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesignerAttributeIncrementalAnalyzerProvider(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IProjectItemDesignerTypeUpdateService projectItemDesignerTypeUpdateService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _projectItemDesignerTypeUpdateService = projectItemDesignerTypeUpdateService;
            _notificationService = notificationService;
            _listenerProvider = listenerProvider;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(CodeAnalysis.Workspace workspace)
        {
            return new DesignerAttributeIncrementalAnalyzer(
                _threadingContext, _serviceProvider, _projectItemDesignerTypeUpdateService, _notificationService, _listenerProvider);
        }
    }
}
