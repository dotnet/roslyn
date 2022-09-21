// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Xunit.Abstractions;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[ExportWorkspaceService(typeof(IWorkspaceTestLogger), ServiceLayer.Host), Shared, PartNotDiscoverable]
internal class WorkpacePartialSolutionsTestHook : IWorkpacePartialSolutionsTestHook
{
    public bool IsPartialSolutionDisabled { get; set; } = true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkpacePartialSolutionsTestHook()
    {
    }
}
