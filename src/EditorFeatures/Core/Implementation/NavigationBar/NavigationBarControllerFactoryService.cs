﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    [Export(typeof(INavigationBarControllerFactoryService))]
    internal class NavigationBarControllerFactoryService : INavigationBarControllerFactoryService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IUIThreadOperationExecutor _uIThreadOperationExecutor;
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigationBarControllerFactoryService(
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor uIThreadOperationExecutor,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _uIThreadOperationExecutor = uIThreadOperationExecutor;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigationBar);
        }

        public IDisposable CreateController(INavigationBarPresenter presenter, ITextBuffer textBuffer)
        {
            return new NavigationBarController(
                _threadingContext,
                presenter,
                textBuffer,
                _uIThreadOperationExecutor,
                _asyncListener);
        }
    }
}
