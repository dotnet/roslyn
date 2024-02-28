// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

/// <summary>
/// Language service that allows a language to participate in the editor's inline rename feature.
/// </summary>
internal abstract class VSTypeScriptEditorInlineRenameServiceImplementation
{
    public abstract Task<VSTypeScriptInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken);
}
