// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(VisualStudioErrorTaskList))]
    internal partial class VisualStudioErrorTaskList : AbstractVisualStudioErrorTaskList
    {
        [ImportingConstructor]
        public VisualStudioErrorTaskList(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            IForegroundNotificationService notificationService,
            IDiagnosticService diagnosticService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
            base(serviceProvider, workspace, notificationService, diagnosticService, asyncListeners)
        {
        }
    }
}
