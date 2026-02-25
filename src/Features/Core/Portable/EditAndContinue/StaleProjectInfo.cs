// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Stale project details.
/// </summary>
internal readonly struct StaleProjectInfo(Guid mvid, string staleDocumentPath)
{
    /// <summary>
    /// Module ID of the built output binary.
    /// </summary>
    public Guid Mvid { get; } = mvid;

    /// <summary>
    /// Path of one of the stale documents that caused the project staleness.
    /// 
    /// Use path instead of <see cref="DocumentId"/> so that a diagnostic describing the reason of the staleness can be reported 
    /// even if the document is removed from the project.
    /// </summary>
    public string StaleDocumentPath { get; } = staleDocumentPath;
}
