// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

internal abstract partial class AbstractLegacyProject : IProjectSiteEx
{
    private readonly Stack<ProjectSystemProject.BatchScope> _batchScopes = new();

    public void StartBatch()
        => _batchScopes.Push(ProjectSystemProject.CreateBatchScope());

    public void EndBatch()
    {
        Contract.ThrowIfFalse(_batchScopes.Count > 0);
        var scope = _batchScopes.Pop();
        scope.Dispose();
    }

    public void AddFileEx([MarshalAs(UnmanagedType.LPWStr)] string filePath, [MarshalAs(UnmanagedType.LPWStr)] string linkMetadata)
    {
        // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
        //var sourceCodeKind = extension.Equals(".csx", StringComparison.OrdinalIgnoreCase)
        //    ? SourceCodeKind.Script
        //    : SourceCodeKind.Regular;
        AddFile(filePath, linkMetadata, SourceCodeKind.Regular);
    }

    public void SetProperty([MarshalAs(UnmanagedType.LPWStr)] string property, [MarshalAs(UnmanagedType.LPWStr)] string value)
    {
        // TODO: Handle the properties we care about.
    }
}
