﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportIncrementalAnalyzerProvider(Name, new[] { WorkspaceKind.Host }), Shared]
    internal class DesignerAttributeIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        public const string Name = nameof(DesignerAttributeIncrementalAnalyzerProvider);

        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _notificationService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesignerAttributeIncrementalAnalyzerProvider(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _notificationService = notificationService;
            _listenerProvider = listenerProvider;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(CodeAnalysis.Workspace workspace)
        {
            return new DesignerAttributeIncrementalAnalyzer(
                _threadingContext, _serviceProvider, _notificationService, _listenerProvider);
        }
    }
}
