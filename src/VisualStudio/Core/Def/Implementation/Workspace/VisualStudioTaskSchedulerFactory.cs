// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Host), Shared]
    internal class VisualStudioTaskSchedulerFactory : EditorTaskSchedulerFactory
    {
        [ImportingConstructor]
        public VisualStudioTaskSchedulerFactory([ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(asyncListeners)
        {
        }

        public override IWorkspaceTaskScheduler CreateEventingTaskQueue()
        {
            // In Visual Studio, we raise these events on the UI thread. At this point we should know
            // exactly which thread that is.
            Contract.ThrowIfTrue(ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.Kind == ForegroundThreadDataKind.Unknown);
            return new WorkspaceTaskQueue(this, ForegroundThreadAffinitizedObject.ForegroundTaskScheduler);
        }
    }
}
