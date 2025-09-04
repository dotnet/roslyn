// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Notification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.UnitTesting;

[Export(typeof(IGlobalOperationNotificationService)), Shared]
internal sealed partial class VisualStudioGlobalOperationNotificationService : AbstractGlobalOperationNotificationService, IDisposable
{
    private readonly SolutionEventMonitor _solutionEventMonitor;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioGlobalOperationNotificationService(
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider) : base(listenerProvider, threadingContext.DisposalToken)
    {
        _solutionEventMonitor = new SolutionEventMonitor(this);

        // we will pause whatever ambient work loads we have that are tied to IGlobalOperationNotificationService
        // such as solution crawler, preemptive remote host synchronization and etc. any background work users
        // didn't explicitly asked for.
        //
        // this should give all resources to BulkFileOperation. we do same for things like build, debugging, wait
        // dialog and etc. BulkFileOperation is used for things like git branch switching and etc.

        // BulkFileOperation can't have nested events. there will be ever only 1 events (Begin/End)
        // so we only need simple tracking.
        var gate = new object();
        IDisposable? localRegistration = null;

        BulkFileOperation.Begin += (s, a) => StartBulkFileOperationNotification();
        BulkFileOperation.End += (s, a) => StopBulkFileOperationNotification();

        return;

        void StartBulkFileOperationNotification()
        {
            lock (gate)
            {
                // this shouldn't happen, but we are using external component
                // so guarding us from them
                if (localRegistration != null)
                {
                    FatalError.ReportAndCatch(new InvalidOperationException("BulkFileOperation already exist"), ErrorSeverity.General);
                    return;
                }

                localRegistration = Start("BulkFileOperation");
            }
        }

        void StopBulkFileOperationNotification()
        {
            lock (gate)
            {
                // localRegistration may be null if BulkFileOperation was already in the middle of running.  So we
                // explicitly do not assert that is is non-null here.
                localRegistration?.Dispose();
                localRegistration = null;
            }
        }
    }

    public void Dispose()
    {
        _solutionEventMonitor.Dispose();
    }
}
