﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public static class WorkspaceTestUtilities
    {
        public static Workspace CreateWorkspaceWithPartialSemantics(Type[]? additionalParts = null)
            => new WorkspaceWithPartialSemantics(FeaturesTestCompositions.Features.AddParts(additionalParts).GetHostServices());

        private class WorkspaceWithPartialSemantics : Workspace
        {
            public WorkspaceWithPartialSemantics(HostServices hostServices) : base(hostServices, workspaceKind: nameof(WorkspaceWithPartialSemantics))
            {
            }

            protected internal override bool PartialSemanticsEnabled => true;
        }
    }
}
