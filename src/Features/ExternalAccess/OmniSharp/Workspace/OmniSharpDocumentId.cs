// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Workspace;

internal static class OmniSharpDocumentId
{
    public static DocumentId CreateFromSerialized(ProjectId projectId, Guid id, bool isSourceGenerated, string? debugName)
            => DocumentId.CreateFromSerialized(projectId, id, isSourceGenerated, debugName);
}
