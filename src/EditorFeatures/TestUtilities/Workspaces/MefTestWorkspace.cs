﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [Export(typeof(Workspace))]
    internal class MefTestWorkspace : Workspace
    {
        [ImportingConstructor]
        public MefTestWorkspace()
            : base(Microsoft.CodeAnalysis.Host.Mef.MefHostServices.DefaultHost, "MefTest")
        {
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            return true;
        }
    }
}
