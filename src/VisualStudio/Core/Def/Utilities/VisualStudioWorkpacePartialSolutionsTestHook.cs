// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices;

/// <summary>
/// Allows integration tests to disable partial solutions.
/// </summary>
[ExportWorkspaceService(typeof(IWorkpacePartialSolutionsTestHook), ServiceLayer.Host), Shared]
internal class VisualStudioWorkpacePartialSolutionsTestHook : IWorkpacePartialSolutionsTestHook
{
    public bool IsPartialSolutionDisabled { get; set; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioWorkpacePartialSolutionsTestHook()
    {
    }
}
