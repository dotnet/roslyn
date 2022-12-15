﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[Export(typeof(IHostWorkspaceProvider)), PartNotDiscoverable, Shared]
internal class MockHostWorkspaceProvider : IHostWorkspaceProvider
{
    public Workspace Workspace { get; set; } = null!;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MockHostWorkspaceProvider()
    {
    }
}
