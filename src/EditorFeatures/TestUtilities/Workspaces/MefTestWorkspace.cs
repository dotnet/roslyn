// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [Export(typeof(Workspace))]
    internal class MefTestWorkspace : Workspace
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MefTestWorkspace()
            : base(Microsoft.CodeAnalysis.Host.Mef.MefHostServices.DefaultHost, "MefTest")
        {
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
            => true;
    }
}
