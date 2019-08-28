// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    [Export(typeof(INavigationBarControllerFactoryService))]
    internal class NavigationBarControllerFactoryService : INavigationBarControllerFactoryService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IWaitIndicator _waitIndicator;
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigationBarControllerFactoryService(
            IThreadingContext threadingContext,
            IWaitIndicator waitIndicator,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _waitIndicator = waitIndicator;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigationBar);
        }

        public INavigationBarController CreateController(INavigationBarPresenter presenter, ITextBuffer textBuffer)
        {
            return new NavigationBarController(
                _threadingContext,
                presenter,
                textBuffer,
                _waitIndicator,
                _asyncListener);
        }
    }
}
