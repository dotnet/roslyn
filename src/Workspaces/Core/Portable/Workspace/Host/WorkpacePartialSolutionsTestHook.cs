// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests;

[ExportWorkspaceService(typeof(IWorkpacePartialSolutionsTestHook), ServiceLayer.Host), Shared]
internal class WorkpacePartialSolutionsTestHook : IWorkpacePartialSolutionsTestHook
{
    public bool IsPartialSolutionDisabled { get; set; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkpacePartialSolutionsTestHook()
    {
    }
}
