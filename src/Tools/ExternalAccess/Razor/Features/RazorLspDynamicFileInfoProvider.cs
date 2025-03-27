﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal abstract class RazorLspDynamicFileInfoProvider : AbstractRazorLspService
{
    public abstract Task<RazorDynamicFileInfo?> GetDynamicFileInfoAsync(Workspace workspace, ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken);
    public abstract Task RemoveDynamicFileInfoAsync(Workspace workspace, ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken);

    public event EventHandler<Uri>? Updated;

    public void Update(Uri razorUri)
    {
        Updated?.Invoke(this, razorUri);
    }
}
