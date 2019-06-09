// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    [Export(typeof(IWaitIndicator))]
    internal sealed class VisualStudioWaitIndicator : IWaitIndicator
    {
        private readonly SVsServiceProvider _serviceProvider;

        private static readonly Func<string, string, string> s_messageGetter = (t, m) => string.Format("{0} : {1}", t, m);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWaitIndicator(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WaitIndicatorResult Wait(
            string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action)
        {
            using (Logger.LogBlock(FunctionId.Misc_VisualStudioWaitIndicator_Wait, s_messageGetter, title, message, CancellationToken.None))
            using (var waitContext = StartWait(title, message, allowCancel, showProgress))
            {
                try
                {
                    action(waitContext);

                    return WaitIndicatorResult.Completed;
                }
                catch (OperationCanceledException)
                {
                    return WaitIndicatorResult.Canceled;
                }
                catch (AggregateException aggregate) when (aggregate.InnerExceptions.All(e => e is OperationCanceledException))
                {
                    return WaitIndicatorResult.Canceled;
                }
            }
        }

        private VisualStudioWaitContext StartWait(
            string title, string message, bool allowCancel, bool showProgress)
        {
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            Contract.ThrowIfNull(workspace);

            var notificationService = workspace.Services.GetService<IGlobalOperationNotificationService>();
            Contract.ThrowIfNull(notificationService);

            var dialogFactory = (IVsThreadedWaitDialogFactory)_serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));
            Contract.ThrowIfNull(dialogFactory);

            return new VisualStudioWaitContext(
                notificationService, dialogFactory, title, message, allowCancel, showProgress);
        }

        IWaitContext IWaitIndicator.StartWait(
            string title, string message, bool allowCancel, bool showProgress)
        {
            return StartWait(title, message, allowCancel, showProgress);
        }
    }
}
