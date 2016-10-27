// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
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
        private readonly bool _isUpdate1;

        private static readonly Func<string, string, string> s_messageGetter = (t, m) => string.Format("{0} : {1}", t, m);

        [ImportingConstructor]
        public VisualStudioWaitIndicator(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var shell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            object property;
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out property);

            _isUpdate1 = Equals(property, "14.0.24720.0 D14REL");
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
                catch (AggregateException e)
                {
                    var operationCanceledException = e.InnerExceptions[0] as OperationCanceledException;
                    if (operationCanceledException != null)
                    {
                        return WaitIndicatorResult.Canceled;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private VisualStudioWaitContext StartWait(
            string title, string message, bool allowCancel, bool showProgress)
        {
            // Update1 has a bug where trying to update hte progress bar will cause a hang.
            // Check if we're on update1 and turn off 'showProgress' in that case.
            if (_isUpdate1)
            {
                showProgress = false;
            }

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
