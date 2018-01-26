// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportIncrementalAnalyzerProvider(Name, new[] { WorkspaceKind.Host }), Shared]
    internal class DesignerAttributeIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        public const string Name = nameof(DesignerAttributeIncrementalAnalyzerProvider);

        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _notificationService;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _asyncListeners;

        [ImportingConstructor]
        public DesignerAttributeIncrementalAnalyzerProvider(
            SVsServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _serviceProvider = serviceProvider;
            _notificationService = notificationService;
            _asyncListeners = asyncListeners;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(CodeAnalysis.Workspace workspace)
        {
            return new DesignerAttributeIncrementalAnalyzer(_serviceProvider, _notificationService, _asyncListeners);
        }
    }
}
