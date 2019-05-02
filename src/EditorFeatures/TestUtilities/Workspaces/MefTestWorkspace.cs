// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
#pragma warning disable RS0032 // Test exports should not be discoverable
    [Export(typeof(Workspace))]
#pragma warning restore RS0032 // Test exports should not be discoverable
    internal class MefTestWorkspace : Workspace
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
