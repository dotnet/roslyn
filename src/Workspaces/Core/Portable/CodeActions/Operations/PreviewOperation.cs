// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Represents a preview operation for generating a custom user preview for the operation.
/// </summary>
public abstract class PreviewOperation : CodeActionOperation
{
    /// <summary>
    /// Gets a custom preview control for the operation.
    /// If preview is null and <see cref="CodeActionOperation.Title"/> is non-null, then <see cref="CodeActionOperation.Title"/> is used to generate the preview.
    /// </summary>
    public abstract Task<object?> GetPreviewAsync(CancellationToken cancellationToken);
}
