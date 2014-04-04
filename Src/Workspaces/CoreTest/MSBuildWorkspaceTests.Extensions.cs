// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public static class MSBuildWorkspaceTestExtensions
    {
        internal static EventWaiter VerifyWorkspaceChangedEvent(this MSBuildWorkspace workspace, Action<WorkspaceChangeEventArgs> action)
        {
            var wew = new EventWaiter();
            workspace.WorkspaceChanged += wew.Wrap<WorkspaceChangeEventArgs>((sender, args) => action(args));
            return wew;
        }

        internal static EventWaiter VerifyWorkspaceFailedEvent(this MSBuildWorkspace workspace, Action<WorkspaceDiagnosticEventArgs> action)
        {
            var wew = new EventWaiter();
            workspace.WorkspaceFailed += wew.Wrap<WorkspaceDiagnosticEventArgs>((sender, args) => action(args));
            return wew;
        }
    }
}
